using System.Reflection;
using System.Reflection.Emit;

namespace ExistForAll.SimpleSettings.Core.Reflection
{
	internal interface IEqualityCompererCreator
	{
		void CreateEqualsMethod(TypeBuilder typeBuilder, List<FieldInfo> fields);
		void CreateGetHashCodeMethod(TypeBuilder typeBuilder, List<FieldInfo> fields);
	}
}