using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ExistForAll.SimpleSettings.Core.Reflection;

namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings
{
	public class SettingsClassGeneratorTests
	{
		[Test]
		public async Task GenerateType_WhereTypeCreated_ShouldDerivedFromInterface()
		{
			var interfaceType = typeof(ITestInterface);

			var sut = new SettingsClassGenerator();

			var result = sut.GenerateType(interfaceType);

			var typeInfo = result.GetTypeInfo();

			await Assert.That(typeInfo.IsClass).IsTrue();
			await Assert.That(interfaceType.GetTypeInfo().IsAssignableFrom(typeInfo)).IsTrue();
		}

		[Test]
		public async Task GenerateType_WhenGivenAnInterface_ShouldCreateType()
		{
			var generator = new SettingsClassGenerator();

			var type = typeof(IRoot);

			var result = generator.GenerateType(type);

			var instance = (IRoot)Activator.CreateInstance(result)!;

			var isAssignableFrom = type.IsInstanceOfType(instance);

			await Assert.That(isAssignableFrom).IsTrue();
		}

		[Test]
		public async Task GenerateType_WhenGivenAnInterfaceInheritance_ShouldCreateTypeFromDerived()
		{
			var generator = new SettingsClassGenerator();

			var type = typeof(IRootChild);

			var result = generator.GenerateType(type);

			var instance = (IRootChild)Activator.CreateInstance(result)!;

			var isAssignableFrom = type.IsInstanceOfType(instance);

			await Assert.That(result.GetProperty(nameof(IRootChild.Age))).IsNotNull();
			await Assert.That(result.GetProperty(nameof(IRootChild.Value))).IsNotNull();
			await Assert.That(isAssignableFrom).IsTrue();
		}

		[Test]
		public async Task GenerateType_WhenCalledTwiceForSameInterface_ReturnsCachedType()
		{
			var generator = new SettingsClassGenerator();

			var first = generator.GenerateType(typeof(ITestInterface));
			var second = generator.GenerateType(typeof(ITestInterface));

			await Assert.That(ReferenceEquals(first, second)).IsTrue();
		}

		[Test]
		public async Task GenerateType_ForTwoInterfacesSharingASimpleName_GeneratesDistinctTypes()
		{
			// DupA.IDuplicateName and DupB.IDuplicateName share the simple name "IDuplicateName".
			// The generated impl name must be namespace-qualified, otherwise both map to the same
			// module type name and the second DefineType throws (aborting the whole scan).
			var generator = new SettingsClassGenerator();

			var a = generator.GenerateType(typeof(DupA.IDuplicateName));
			var b = generator.GenerateType(typeof(DupB.IDuplicateName));

			await Assert.That(ReferenceEquals(a, b)).IsFalse();
			await Assert.That(typeof(DupA.IDuplicateName).GetTypeInfo().IsAssignableFrom(a.GetTypeInfo())).IsTrue();
			await Assert.That(typeof(DupB.IDuplicateName).GetTypeInfo().IsAssignableFrom(b.GetTypeInfo())).IsTrue();
		}

		[Test]
		public async Task GenerateType_WhenDerivedHidesABasePropertyName_DeduplicatesByName()
		{
			var generator = new SettingsClassGenerator();

			// IHidingChild re-declares Value (already on IRoot). Property extraction must dedup by name;
			// without it the generator would emit two "Value" members and TypeBuilder would throw.
			var result = generator.GenerateType(typeof(IHidingChild));

			await Assert.That(result.GetProperty(nameof(IHidingChild.Value))).IsNotNull();
			await Assert.That(result.GetProperty(nameof(IHidingChild.Extra))).IsNotNull();
			await Assert.That(typeof(IHidingChild).IsInstanceOfType(Activator.CreateInstance(result)!)).IsTrue();
		}

		[Test]
		public async Task GenerateType_ConcurrentSameInterface_ReturnsSingleSharedType()
		{
			// The reported T7 race: concurrent first-generation of the SAME interface used to let two threads
			// both DefineType the same name (the second throws -> the resolve/scan aborts). Post-fix, every
			// caller gets the one shared impl.
			var generator = new SettingsClassGenerator();
			var results = new ConcurrentBag<Type>();

			Parallel.For(0, 128, _ => results.Add(generator.GenerateType(typeof(IStressA))));

			await Assert.That(results.Count).IsEqualTo(128);
			await Assert.That(results.Distinct().Count()).IsEqualTo(1);
		}

		[Test]
		public async Task GenerateType_ConcurrentAcrossSameAndDistinctInterfaces_IsRaceFree()
		{
			// Guards the design decision (one lock over ALL generation): Reflection.Emit is not thread-safe, so
			// concurrent DefineType of DISTINCT interfaces also races the single shared ModuleBuilder — a
			// per-type Lazy would not catch that. Fixed threads + a Barrier align the DefineType calls for
			// maximum contention (Parallel.For can't guarantee N concurrent workers, which would deadlock a
			// Barrier(N)). Each thread hits one of 8 interfaces, so this exercises same-interface AND
			// distinct-interface contention at once.
			var generator = new SettingsClassGenerator();
			var interfaces = new[]
			{
				typeof(IStressA), typeof(IStressB), typeof(IStressC), typeof(IStressD),
				typeof(IStressE), typeof(IStressF), typeof(IStressG), typeof(IStressH),
			};

			const int threadCount = 32;
			var barrier = new Barrier(threadCount);
			var perInterface = new ConcurrentDictionary<Type, ConcurrentBag<Type>>();
			var failures = new ConcurrentBag<Exception>();
			var workers = new List<Thread>();

			for (var i = 0; i < threadCount; i++)
			{
				var iface = interfaces[i % interfaces.Length];
				var worker = new Thread(() =>
				{
					try
					{
						barrier.SignalAndWait();
						perInterface.GetOrAdd(iface, _ => new ConcurrentBag<Type>()).Add(generator.GenerateType(iface));
					}
					catch (Exception e)
					{
						failures.Add(e);
					}
				});
				workers.Add(worker);
				worker.Start();
			}

			foreach (var worker in workers)
				worker.Join();

			await Assert.That(failures.Count).IsEqualTo(0);
			await Assert.That(perInterface.Count).IsEqualTo(interfaces.Length);
			foreach (var iface in interfaces)
				await Assert.That(perInterface[iface].Distinct().Count()).IsEqualTo(1);
		}
	}

	public interface IHidingChild : IRoot
	{
		new string Value { get; set; }
		int Extra { get; set; }
	}

	// Distinct marker interfaces for the concurrency stress tests (GenerateType_Concurrent* above).
	public interface IStressA { int Value { get; set; } }
	public interface IStressB { int Value { get; set; } }
	public interface IStressC { int Value { get; set; } }
	public interface IStressD { int Value { get; set; } }
	public interface IStressE { int Value { get; set; } }
	public interface IStressF { int Value { get; set; } }
	public interface IStressG { int Value { get; set; } }
	public interface IStressH { int Value { get; set; } }
}

namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings.DupA
{
	// Same simple name as DupB.IDuplicateName; no suffix/attribute/base so neither is auto-discovered
	// by assembly scans — they only exercise the generator directly via GenerateType.
	public interface IDuplicateName
	{
		string Value { get; set; }
	}
}

namespace ExistForAll.SimpleSettings.UnitTests.SimpleSettings.DupB
{
	public interface IDuplicateName
	{
		string Value { get; set; }
	}
}
