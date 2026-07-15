using System.Collections.Concurrent;
using System.Reflection;
using ExistForAll.SimpleSettings.Core.Reflection;

namespace ExistForAll.SimpleSettings.Conversion
{
	// Handles the List<T> family (List/IList/ICollection/IReadOnlyList/IReadOnlyCollection<T>) by reusing the
	// base array-build path and copying the result into a List<T>. Materialization uses a cached per-element
	// delegate (built once, invoked on every populate) so the warm path carries no MakeGenericType/Activator
	// reflection.
	internal class ListTypeConverter : CollectionTypeConverter
	{
		private static readonly MethodInfo CreateListMethod =
			typeof(ListTypeConverter).GetMethod(nameof(CreateList), BindingFlags.NonPublic | BindingFlags.Static)!;

		private static readonly ConcurrentDictionary<Type, Func<Array, object>> ListFactories = new();

		public ListTypeConverter(SettingsOptions settingsOptions, TypeConvertersCollections converters)
			: base(settingsOptions, converters)
		{
		}

		public override bool CanConvert(Type settingsType)
		{
			return settingsType.IsListLike();
		}

		protected override Type GetElementType(Type settingsType)
		{
			return settingsType.GetTypeInfo().GetGenericArguments()[0];
		}

		public override object Convert(object value, Type settingsType)
		{
			var elementType = GetElementType(settingsType);
			var builtArray = (Array)base.Convert(value, settingsType);
			var factory = ListFactories.GetOrAdd(elementType, BuildFactory);

			return factory(builtArray);
		}

		private static Func<Array, object> BuildFactory(Type elementType)
		{
			var closed = CreateListMethod.MakeGenericMethod(elementType);

			return (Func<Array, object>)closed.CreateDelegate(typeof(Func<Array, object>));
		}

		// The source is the freshly built T[] (ICollection<T>), so new List<T>(source) copies into a
		// right-sized buffer rather than aliasing the array.
		private static object CreateList<TElement>(Array source)
		{
			return new List<TElement>((TElement[])source);
		}
	}
}
