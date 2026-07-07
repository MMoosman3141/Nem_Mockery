using System.Reflection;
using System.Reflection.Emit;

namespace Nem_Mockery.Interception;

/// <summary>
/// Emits the replacement method MonoMod detours a mocked method to. The generated
/// handler has the exact signature of the source method plus a leading "orig"
/// trampoline delegate (MonoMod's hook convention). Its body boxes every argument
/// into an <see langword="object"/> array, calls
/// <see cref="MockDispatcher"/>, writes by-ref updates back, and returns the
/// dispatcher's (unboxed) result.
/// </summary>
internal static class HandlerFactory {
  /// <summary>
  /// Builds the handler for <paramref name="method"/>.
  /// </summary>
  /// <param name="method">The method or constructor being mocked.</param>
  /// <param name="mockId">The registry id baked into the handler as a constant.</param>
  /// <returns>The handler to pass to MonoMod as the hook target.</returns>
  internal static DynamicMethod Create(MethodBase method, int mockId) {
    Type returnType = (method is MethodInfo info) ? info.ReturnType : typeof(void);
    Type[] sourceSignature = BuildSourceSignature(method);
    Type origDelegateType = DelegateTypeFactory.GetDelegateType(returnType, sourceSignature);

    Type[] handlerParameters = new Type[sourceSignature.Length + 1];
    handlerParameters[0] = origDelegateType;
    Array.Copy(sourceSignature, 0, handlerParameters, 1, sourceSignature.Length);

    DynamicMethod handler = new(
      $"Nem_Mockery_Handler_{mockId}",
      returnType,
      handlerParameters,
      typeof(MockDispatcher).Module,
      skipVisibility: true);

    ILGenerator il = handler.GetILGenerator();
    LocalBuilder argumentArray = il.DeclareLocal(typeof(object[]));
    LocalBuilder dispatchResult = il.DeclareLocal(typeof(object));

    // object[] arguments = new object[slotCount];
    EmitLoadInt(il, sourceSignature.Length);
    il.Emit(OpCodes.Newarr, typeof(object));
    il.Emit(OpCodes.Stloc, argumentArray);

    // arguments[slot] = (object)argSlot; -- dereferencing and boxing as needed.
    for (int slot = 0; slot < sourceSignature.Length; slot++) {
      il.Emit(OpCodes.Ldloc, argumentArray);
      EmitLoadInt(il, slot);
      EmitLoadArgument(il, slot + 1);
      Type slotType = sourceSignature[slot];
      if (slotType.IsByRef) {
        Type elementType = slotType.GetElementType()!;
        if (elementType.IsValueType) {
          il.Emit(OpCodes.Ldobj, elementType);
          il.Emit(OpCodes.Box, elementType);
        } else {
          il.Emit(OpCodes.Ldind_Ref);
        }
      } else if (slotType.IsValueType) {
        il.Emit(OpCodes.Box, slotType);
      }
      il.Emit(OpCodes.Stelem_Ref);
    }

    // object result = MockDispatcher.Dispatch(mockId, orig, arguments);
    EmitLoadInt(il, mockId);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldloc, argumentArray);
    il.Emit(OpCodes.Call, MockDispatcher.DispatchMethod);
    il.Emit(OpCodes.Stloc, dispatchResult);

    // Write possibly-updated array elements back through every by-ref slot (covers
    // ref/out parameters and the by-ref 'this' of struct instance methods).
    for (int slot = 0; slot < sourceSignature.Length; slot++) {
      Type slotType = sourceSignature[slot];
      if (!slotType.IsByRef) {
        continue;
      }
      Type elementType = slotType.GetElementType()!;
      EmitLoadArgument(il, slot + 1);
      il.Emit(OpCodes.Ldloc, argumentArray);
      EmitLoadInt(il, slot);
      il.Emit(OpCodes.Ldelem_Ref);
      if (elementType.IsValueType) {
        il.Emit(OpCodes.Unbox_Any, elementType);
        il.Emit(OpCodes.Stobj, elementType);
      } else {
        il.Emit(OpCodes.Castclass, elementType);
        il.Emit(OpCodes.Stind_Ref);
      }
    }

    if (returnType != typeof(void)) {
      il.Emit(OpCodes.Ldloc, dispatchResult);
      if (returnType.IsValueType) {
        il.Emit(OpCodes.Unbox_Any, returnType);
      } else {
        il.Emit(OpCodes.Castclass, returnType);
      }
    }
    il.Emit(OpCodes.Ret);

    return handler;
  }

  /// <summary>
  /// Computes the full signature of the source method as MonoMod sees it: an explicit
  /// leading receiver slot for instance methods (by-ref for structs), then the
  /// declared parameters.
  /// </summary>
  private static Type[] BuildSourceSignature(MethodBase method) {
    ParameterInfo[] parameters = method.GetParameters();
    bool hasThis = !method.IsStatic;
    Type[] signature = new Type[parameters.Length + (hasThis ? 1 : 0)];
    int offset = 0;
    if (hasThis) {
      Type declaringType = method.DeclaringType!;
      signature[0] = declaringType.IsValueType ? declaringType.MakeByRefType() : declaringType;
      offset = 1;
    }
    for (int i = 0; i < parameters.Length; i++) {
      signature[i + offset] = parameters[i].ParameterType;
    }
    return signature;
  }

  private static void EmitLoadInt(ILGenerator il, int value) {
    if (value is >= -1 and <= 8) {
      il.Emit(value switch {
        -1 => OpCodes.Ldc_I4_M1,
        0 => OpCodes.Ldc_I4_0,
        1 => OpCodes.Ldc_I4_1,
        2 => OpCodes.Ldc_I4_2,
        3 => OpCodes.Ldc_I4_3,
        4 => OpCodes.Ldc_I4_4,
        5 => OpCodes.Ldc_I4_5,
        6 => OpCodes.Ldc_I4_6,
        7 => OpCodes.Ldc_I4_7,
        _ => OpCodes.Ldc_I4_8,
      });
    } else if (value is >= sbyte.MinValue and <= sbyte.MaxValue) {
      il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
    } else {
      il.Emit(OpCodes.Ldc_I4, value);
    }
  }

  private static void EmitLoadArgument(ILGenerator il, int index) {
    switch (index) {
      case 0:
        il.Emit(OpCodes.Ldarg_0);
        break;
      case 1:
        il.Emit(OpCodes.Ldarg_1);
        break;
      case 2:
        il.Emit(OpCodes.Ldarg_2);
        break;
      case 3:
        il.Emit(OpCodes.Ldarg_3);
        break;
      default:
        il.Emit(OpCodes.Ldarg_S, (byte)index);
        break;
    }
  }
}
