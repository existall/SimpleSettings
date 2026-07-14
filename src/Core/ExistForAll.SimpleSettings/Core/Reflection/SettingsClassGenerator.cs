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

		// Serializes ALL generation on this instance. System.Reflection.Emit is not thread-safe: DefineType and
		// the rest of the emit sequence mutate module-scoped state (the type-name table, the metadata/token
		// allocator) on the shared _moduleBuilder with no internal synchronization. So two threads generating
		// the SAME interface would both DefineType its name (the second throws "Duplicate type name" -> the
		// scan aborts), and two threads generating DIFFERENT interfaces would corrupt the shared module. One
		// gate whose scope matches _moduleBuilder's scope covers both. The warm cache-hit path stays lock-free
		// (the TryGetValue before the lock). This closes the T7 race that Q4's ConcurrentDictionary left open:
		// it made the cache thread-safe, but not the check-then-DefineType.
		private readonly object _generationGate = new();

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
			// Warm path: already generated. Lock-free — the cache is a ConcurrentDictionary, and the value it
			// holds is a fully-baked Type published (inside the lock) only after CreateTypeInfo() completed, so
			// there is no torn/partial read.
			if (_generatedTypes.TryGetValue(interfaceType, out var existingType))
				return existingType;

			lock (_generationGate)
			{
				// Another thread may have generated it while we waited for the lock.
				if (_generatedTypes.TryGetValue(interfaceType, out existingType))
					return existingType;

				try
				{
					var result = DefineImplementationType(interfaceType);
					_generatedTypes[interfaceType] = result;
					return result;
				}
				catch (Exception e)
				{
					// A failure before DefineType (e.g. property extraction) is retryable. A failure after it
					// leaves a half-defined TypeBuilder under this name, so a retry would hit "Duplicate type
					// name" — pre-existing behavior, only reachable for a malformed interface that would fail
					// deterministically anyway.
					throw new TypeGenerationException(interfaceType, e);
				}
			}
		}

		private Type DefineImplementationType(Type interfaceType)
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

			return typeBuilder.CreateTypeInfo().AsType();
		}
	}
}
