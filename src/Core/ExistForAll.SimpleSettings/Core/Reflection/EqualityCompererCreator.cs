using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ExistForAll.SimpleSettings.Core.Reflection
{
	internal class EqualityCompererCreator : IEqualityCompererCreator
	{
		public void CreateEqualsMethod(TypeBuilder typeBuilder, List<FieldInfo> fields)
		{
			if (typeBuilder == null) throw new ArgumentNullException(nameof(typeBuilder));
			if (fields == null) throw new ArgumentNullException(nameof(fields));

			var method = typeBuilder.DefineMethod("Equals", MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.HideBySig, typeof(bool),
				[typeof(object)]);

			var methodGenerator = method.GetILGenerator();
			var other = methodGenerator.DeclareLocal(typeBuilder.DeclaringType!);
			var next = methodGenerator.DefineLabel();
			methodGenerator.Emit(OpCodes.Ldarg_1);
			methodGenerator.Emit(OpCodes.Isinst, typeBuilder.DeclaringType!);
			methodGenerator.Emit(OpCodes.Stloc, other);
			methodGenerator.Emit(OpCodes.Ldloc, other);
			methodGenerator.Emit(OpCodes.Brtrue_S, next);
			methodGenerator.Emit(OpCodes.Ldc_I4_0);
			methodGenerator.Emit(OpCodes.Ret);
			methodGenerator.MarkLabel(next);
			foreach (var field in fields)
			{
				var comparerType = typeof(EqualityComparer<>).MakeGenericType(field.FieldType).GetTypeInfo();
				next = methodGenerator.DefineLabel();
				methodGenerator.EmitCall(OpCodes.Call, comparerType.GetMethod("get_Default")!, null);
				methodGenerator.Emit(OpCodes.Ldarg_0);
				methodGenerator.Emit(OpCodes.Ldfld, field);
				methodGenerator.Emit(OpCodes.Ldloc, other);
				methodGenerator.EmitCall(OpCodes.Callvirt, comparerType.GetMethod("Equals", [field.FieldType, field.FieldType
				])!, null);
				methodGenerator.Emit(OpCodes.Brtrue_S, next);
				methodGenerator.Emit(OpCodes.Ldc_I4);
				methodGenerator.Emit(OpCodes.Ret);
				methodGenerator.MarkLabel(next);
			}

			methodGenerator.Emit(OpCodes.Ldc_I4_1);
			methodGenerator.Emit(OpCodes.Ret);
		}

		public void CreateGetHashCodeMethod(TypeBuilder typeBuilder, List<FieldInfo> fields)
		{
			if (typeBuilder == null) throw new ArgumentNullException(nameof(typeBuilder));
			if (fields == null) throw new ArgumentNullException(nameof(fields));

			var method = typeBuilder.DefineMethod("GetHashCode", MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.HideBySig, typeof(int), null);

			var methodGenerator = method.GetILGenerator();
			methodGenerator.Emit(OpCodes.Ldc_I4_0);
			foreach (var field in fields)
			{
				var comparerType = typeof(EqualityComparer<>).MakeGenericType(field.FieldType).GetTypeInfo();
				methodGenerator.EmitCall(OpCodes.Call, comparerType.GetMethod("get_Default")!, null);
				methodGenerator.Emit(OpCodes.Ldarg_0);
				methodGenerator.Emit(OpCodes.Ldfld, field);
				methodGenerator.EmitCall(OpCodes.Callvirt, comparerType.GetMethod("GetHashCode", [field.FieldType])!, null);
				methodGenerator.Emit(OpCodes.Xor);
			}

			methodGenerator.Emit(OpCodes.Ret);
		}
	}
}