using System.Reflection;
using System.Reflection.Emit;

namespace ExistForAll.SimpleSettings.Core.Reflection
{
	internal interface IPropertyCreator
	{
		void CreateAnonymousProperties(TypeBuilder typeBuilder, PropertyInfo[] properties, out List<FieldInfo> fields);
	}
}