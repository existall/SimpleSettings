using System;
using System.Reflection;
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
	}

	public interface IHidingChild : IRoot
	{
		new string Value { get; set; }
		int Extra { get; set; }
	}
}
