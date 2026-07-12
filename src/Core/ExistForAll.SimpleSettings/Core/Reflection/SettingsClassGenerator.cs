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

		internal SettingsClassGenerator(ITypePropertiesExtractor typePropertiesExtractor,
			IPropertyCreator propertyCreator)
		{
			_typePropertiesExtractor = typePropertiesExtractor;
			_propertyCreator = propertyCreator;
		}

		public SettingsClassGenerator()
			: this(new TypePropertiesExtractor(new ConcurrentDictionary<Type, PropertyInfo[]>()),
				new PropertyCreator())
		{
			var assemblyName = new AssemblyName(Guid.NewGuid().ToString());
			var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);

			_moduleBuilder = assemblyBuilder.DefineDynamicModule("ConfingModule");
		}

		public Type GenerateType(Type interfaceType)
		{
			try
			{
				var name = $"{interfaceType.GetNormalizeInterfaceName()}Impl";

				var existingType = _moduleBuilder.Assembly.GetType(name.Replace("+", "\\+"));

				if (existingType != null)
					return existingType;

				var properties = _typePropertiesExtractor.ExtractTypeProperties(interfaceType);

				var typeBuilder = _moduleBuilder.DefineType(name, TypeAttributes.Class | TypeAttributes.Public);

				typeBuilder.AddInterfaceImplementation(interfaceType);

				_propertyCreator.CreateAnonymousProperties(typeBuilder, properties.ToArray(), out _);
				
				var result = typeBuilder.CreateTypeInfo();

				return result.AsType();
			}
			catch (Exception e)
			{
				throw new TypeGenerationException(interfaceType, e);
			}
		}
	}
}