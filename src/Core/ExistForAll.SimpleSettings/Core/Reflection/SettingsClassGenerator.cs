using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ExistForAll.SimpleSettings.Core.Reflection
{
	internal class SettingsClassGenerator : ISettingsClassGenerator
	{
		private readonly ITypePropertiesExtractor _typePropertiesExtractor;
		private readonly IPropertyCreator _propertyCreator;
		private readonly ModuleBuilder _moduleBuilder = null!;

		// A settings interface generates exactly one impl type for the module's lifetime, so cache by the
		// interface Type instead of re-querying the module by mangled type name on every call.
		private readonly ConcurrentDictionary<Type, Type> _generatedTypes = new();

		internal SettingsClassGenerator(ITypePropertiesExtractor typePropertiesExtractor,
			IPropertyCreator propertyCreator)
		{
			_typePropertiesExtractor = typePropertiesExtractor;
			_propertyCreator = propertyCreator;
		}

		public SettingsClassGenerator()
			: this(new TypePropertiesExtractor(), new PropertyCreator())
		{
			var assemblyName = new AssemblyName(Guid.NewGuid().ToString());
			var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);

			_moduleBuilder = assemblyBuilder.DefineDynamicModule("ConfingModule");
		}

		public Type GenerateType(Type interfaceType)
		{
			if (_generatedTypes.TryGetValue(interfaceType, out var existingType))
				return existingType;

			try
			{
				// Namespace-qualified so two settings interfaces that share a simple name
				// (e.g. Foo.ISettings + Bar.ISettings) don't collide on the generated type name and
				// abort the scan. Deliberately NOT GetNormalizeInterfaceName() — that helper also backs
				// the default config section name (SettingsOptions.SectionNameFormatter), which must stay
				// simple-name-based; the generated impl name is an internal detail and can differ.
				var name = $"{(interfaceType.FullName ?? interfaceType.Name).Replace('.', '_').Replace('+', '_')}Impl";

				var properties = _typePropertiesExtractor.ExtractTypeProperties(interfaceType);

				var typeBuilder = _moduleBuilder.DefineType(name, TypeAttributes.Class | TypeAttributes.Public);

				typeBuilder.AddInterfaceImplementation(interfaceType);

				_propertyCreator.CreateAnonymousProperties(typeBuilder, properties.ToArray(), out _);

				var result = typeBuilder.CreateTypeInfo().AsType();

				_generatedTypes[interfaceType] = result;

				return result;
			}
			catch (Exception e)
			{
				throw new TypeGenerationException(interfaceType, e);
			}
		}
	}
}