using ExistForAll.SimpleSettings.Binder;
using ExistForAll.SimpleSettings.Binders;
using ExistForAll.SimpleSettings.Conversion;
using Microsoft.Extensions.Configuration;

namespace ExistForAll.SimpleSettings.UnitTests.Conversion
{
	// S1: a bound value that fails conversion must never reach the exception message OR a chained inner
	// exception — either can embed a secret, which then lands in logs via Exception.ToString()/ILogger.
	// Each test binds a distinctive sentinel to a typed property that fails to convert and asserts the
	// sentinel appears NOWHERE in the thrown exception's full ToString() (which walks message + every inner
	// exception + stack), while the safe diagnostics (property name + target type) are still present.
	public class ExceptionRedactionTests
	{
		private const string Secret = "S3CR3T-sentinel-do-not-log";

		[Test]
		public async Task Convert_SecretToInt_DoesNotLeakValue()
		{
			// Convert.ChangeType throws a FormatException whose message embeds the raw input on modern .NET.
			var ex = CaptureConversionFailure<IIntSetting>(nameof(IIntSetting.Value), Secret);
			await AssertRedacted(ex, nameof(IIntSetting.Value), nameof(Int32));
		}

		[Test]
		public async Task Convert_SecretToEnum_DoesNotLeakValue()
		{
			// Enum.Parse throws an ArgumentException that embeds the requested value.
			var ex = CaptureConversionFailure<IEnumSetting>(nameof(IEnumSetting.Day), Secret);
			await AssertRedacted(ex, nameof(IEnumSetting.Day), nameof(DayOfWeek));
		}

		[Test]
		public async Task Convert_SecretToDateTime_DoesNotLeakValue()
		{
			// DateTime.ParseExact throws a FormatException that embeds the input string.
			var ex = CaptureConversionFailure<IDateTimeSetting>(nameof(IDateTimeSetting.When), Secret);
			await AssertRedacted(ex, nameof(IDateTimeSetting.When), nameof(DateTime));
		}

		[Test]
		public async Task Convert_SecretToUri_DoesNotLeakValue()
		{
			// UriFormatException's message is generic on modern .NET (no URI), so this pins the OTHER vector:
			// our own message used to interpolate the raw value — a credentialed URL would leak there.
			var ex = CaptureConversionFailure<IUriSetting>(nameof(IUriSetting.Endpoint), Secret);
			await AssertRedacted(ex, nameof(IUriSetting.Endpoint), nameof(Uri));
		}

		[Test]
		public async Task Convert_CustomConverterLeakingValue_IsStillRedacted()
		{
			// ISettingsTypeConverter is a public extension point. A custom converter that throws a value-bearing
			// message must not leak either — the wrapper drops the inner and keeps only its type name.
			var ex = CaptureConversionFailure<ILeakyConverterSetting>(nameof(ILeakyConverterSetting.Value), Secret);
			await AssertRedacted(ex, nameof(ILeakyConverterSetting.Value), nameof(String));
		}

		[Test]
		public async Task Convert_SecretInSequenceElement_DoesNotLeakValue()
		{
			// D-06/S1 on the ConfigurationBinder GetChildren() sequence path: an int[] element that fails to
			// convert is wrapped value-free — the secret in a LATER element never reaches the ToString() chain.
			var ex = CaptureSequenceConversionFailure<IIntArraySetting>(nameof(IIntArraySetting.Values), "1", Secret);
			await AssertRedacted(ex, nameof(IIntArraySetting.Values), nameof(Int32));
		}

		[Test]
		public async Task Convert_SecretInFirstSequenceElement_DoesNotLeakValue()
		{
			// S-6: element position must not matter — the throw site is the shared per-element convert, so a
			// secret in the FIRST element is redacted identically.
			var ex = CaptureSequenceConversionFailure<IIntArraySetting>(nameof(IIntArraySetting.Values), Secret, "2");
			await AssertRedacted(ex, nameof(IIntArraySetting.Values), nameof(Int32));
		}

		[Test]
		public async Task Convert_SecretInListSequenceElement_DoesNotLeakValue()
		{
			// S-6: the List<T> converter wraps into a List<T> only AFTER the element-convert throw site, so the
			// redaction contract holds identically for the List shape as for the array shape.
			var ex = CaptureSequenceConversionFailure<IIntListSetting>(nameof(IIntListSetting.Values), "1", Secret);
			await AssertRedacted(ex, nameof(IIntListSetting.Values), "List");
		}

		private static SettingsPropertyValueException CaptureSequenceConversionFailure<T>(string key, params string[] elements)
			where T : class
		{
			var data = new Dictionary<string, string?>();
			var section = SectionOf<T>();
			for (var i = 0; i < elements.Length; i++)
			{
				data[$"{section}:{key}:{i}"] = elements[i];
			}

			var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
			var builder = SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new ConfigurationBinder(configuration)));

			try
			{
				builder.GetSettings<T>();
			}
			catch (SettingsPropertyValueException e)
			{
				return e;
			}

			throw new Exception($"Expected a SettingsPropertyValueException for [{typeof(T).Name}], but none was thrown.");
		}

		private static SettingsPropertyValueException CaptureConversionFailure<T>(string key, string value)
			where T : class
		{
			var collection = new InMemoryCollection();
			collection.Add(SectionOf<T>(), key, value);
			var builder = SettingsBuilder.CreateBuilder(x => x.AddSectionBinder(new InMemoryBinder(collection)));

			try
			{
				builder.GetSettings<T>();
			}
			catch (SettingsPropertyValueException e)
			{
				return e;
			}

			throw new Exception($"Expected a SettingsPropertyValueException for [{typeof(T).Name}], but none was thrown.");
		}

		private static async Task AssertRedacted(SettingsPropertyValueException ex, string propertyName, string targetType)
		{
			var full = ex.ToString();
			await Assert.That(full.Contains(Secret)).IsFalse();
			await Assert.That(full.Contains(propertyName)).IsTrue();
			await Assert.That(full.Contains(targetType)).IsTrue();
		}

		// The default SectionNameFormatter strips the leading "I": IIntSetting -> "IntSetting".
		private static string SectionOf<T>() => typeof(T).Name.Substring(1);

		public interface IIntSetting
		{
			int Value { get; set; }
		}

		public interface IEnumSetting
		{
			DayOfWeek Day { get; set; }
		}

		public interface IDateTimeSetting
		{
			DateTime When { get; set; }
		}

		public interface IUriSetting
		{
			Uri Endpoint { get; set; }
		}

		public interface IIntArraySetting
		{
			int[] Values { get; set; }
		}

		public interface IIntListSetting
		{
			List<int> Values { get; set; }
		}

		public interface ILeakyConverterSetting
		{
			[SettingsProperty(ConverterType = typeof(LeakyConverter))]
			string Value { get; set; }
		}

		// A deliberately hostile custom converter: it stuffs the bound value into its exception message to
		// prove the wrapper still refuses to surface it.
		public class LeakyConverter : ISettingsTypeConverter
		{
			public bool CanConvert(Type settingsType) => true;

			public object Convert(object value, Type settingsType)
				=> throw new InvalidOperationException($"conversion blew up for value '{value}'");
		}
	}
}
