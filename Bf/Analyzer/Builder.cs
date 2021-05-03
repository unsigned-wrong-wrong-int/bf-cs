using System.Collections.Generic;
using System.Reflection.Emit;

namespace Bf.Analyzer
{
   class Builder
   {
      readonly DynamicMethod method;
      readonly ILGenerator il;

      readonly RuntimeInfo runtime;

      readonly Label pointerError;
      readonly Label loopError;

      readonly Stack<Label> labels;

      public Builder(RuntimeInfo runtime)
      {
         method = new DynamicMethod("",
            returnType: null,
            parameterTypes: new[] { runtime.Type });
         il = method.GetILGenerator();
         pointerError = il.DefineLabel();
         loopError = il.DefineLabel();
         labels = new();
         _ = il.DeclareLocal(typeof(byte* /* pointer */));
         _ = il.DeclareLocal(typeof(byte /* loaded */));
         _ = il.DeclareLocal(typeof(byte* /* lowerBound */));
         _ = il.DeclareLocal(typeof(byte* /* upperBound */));
         _ = il.DeclareLocal(typeof(byte[] /* cells */), pinned: true);
      }

      public void Begin()
      {
         /*
            cells = runtime.Start();
            pointer = &cells;
            lowerBound = pointer;
            upperBound = pointer + cells.Length;
         */
         il.Emit(OpCodes.Ldarg_0);
         il.Emit(OpCodes.Call, runtime.Start);
         il.Emit(OpCodes.Dup);
         il.Emit(OpCodes.Stloc_S, 4);
         il.Emit(OpCodes.Ldc_I4_0);
         il.Emit(OpCodes.Ldelema, typeof(byte));
         il.Emit(OpCodes.Conv_U);
         il.Emit(OpCodes.Dup);
         il.Emit(OpCodes.Stloc_0);
         il.Emit(OpCodes.Dup);
         il.Emit(OpCodes.Stloc_2);
         il.Emit(OpCodes.Ldloc_S, 4);
         il.Emit(OpCodes.Ldlen);
         il.Emit(OpCodes.Add);
         il.Emit(OpCodes.Stloc_3);
      }

      public void End()
      {
         /*
            runtime.End();
            return;
         pointerError:
            runtime.Abort("...");
            return;
         loopError:
            runtime.Abort("...");
            return;
         */
         il.Emit(OpCodes.Ldarg_0);
         il.Emit(OpCodes.Call, runtime.End);
         il.Emit(OpCodes.Ret);
         il.MarkLabel(pointerError);
         il.Emit(OpCodes.Ldarg_0);
         il.Emit(OpCodes.Ldstr, "Pointer out of array bounds");
         il.Emit(OpCodes.Call, runtime.Abort);
         il.Emit(OpCodes.Ret);
         il.MarkLabel(loopError);
         il.Emit(OpCodes.Ldarg_0);
         il.Emit(OpCodes.Ldstr, "Infinite loop with no read/write");
         il.Emit(OpCodes.Call, runtime.Abort);
         il.Emit(OpCodes.Ret);
      }

      void AtOffset(int offset)
      {
         // $ptr = pointer + offset;
         il.Emit(OpCodes.Ldloc_0);
         if (offset != 0)
         {
            il.Emit(OpCodes.Ldc_I4, offset);
            il.Emit(OpCodes.Add);
         }
      }

      public void AddConst(int offset, byte value, bool overwrite)
      {
         AtOffset(offset);
         if (overwrite)
         {
            // $val = value;
            il.Emit(OpCodes.Ldc_I4_S, value);
         }
         else
         {
            // $val = *$ptr + value;
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldind_U1);
            il.Emit(OpCodes.Ldc_I4_S, value);
            il.Emit(OpCodes.Add);
         }
         // *$ptr = $val;
         il.Emit(OpCodes.Stind_I1);
      }

      public void Add(int offset, byte multiplier)
      {
         AtOffset(offset);
         // $val = *$ptr;
         il.Emit(OpCodes.Dup);
         il.Emit(OpCodes.Ldind_U1);
         // $val += loaded * multiplier;
         il.Emit(OpCodes.Ldloc_1);
         if (multiplier != 1)
         {
            il.Emit(OpCodes.Ldc_I4_S, multiplier);
            il.Emit(OpCodes.Mul);
         }
         il.Emit(OpCodes.Add);
         // *$ptr = $val;
         il.Emit(OpCodes.Stind_I1);
      }

      public void Load(int offset, byte shiftRight)
      {
         AtOffset(offset);
         // $val = *$ptr;
         il.Emit(OpCodes.Ldind_U1);
         if (shiftRight != 0)
         {
            // loaded = $val;
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Stloc_1);
            // if (($val & mask) != 0) goto loopError;
            il.Emit(OpCodes.Ldc_I4_S, (byte)((1 << shiftRight) - 1));
            il.Emit(OpCodes.And);
            il.Emit(OpCodes.Brtrue, loopError);
            // $val = loaded >> shiftRight;
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Ldc_I4_S, shiftRight);
            il.Emit(OpCodes.Shr_Un);
         }
         // loaded = $val;
         il.Emit(OpCodes.Stloc_1);
      }

      public void WriteConst(byte value)
      {
         // runtime.Write(value);
         il.Emit(OpCodes.Ldarg_0);
         il.Emit(OpCodes.Ldc_I4_S, value);
         il.Emit(OpCodes.Call, runtime.Write);
      }

      public void Write(int offset)
      {
         AtOffset(offset);
         // runtime.Write(*$ptr);
         il.Emit(OpCodes.Ldind_U1);
         il.Emit(OpCodes.Call, runtime.Write);
      }

      public void Read(int offset)
      {
         AtOffset(offset);
         // *$ptr = runtime.Read();
         il.Emit(OpCodes.Ldarg_0);
         il.Emit(OpCodes.Call, runtime.Read);
         il.Emit(OpCodes.Stind_I1);
      }

      public void Move(int offset)
      {
         if (offset == 0)
         {
            return;
         }
         // pointer += offset;
         il.Emit(OpCodes.Ldloc_0);
         il.Emit(OpCodes.Ldc_I4, offset);
         il.Emit(OpCodes.Add);
         il.Emit(OpCodes.Stloc_0);
      }

      public void CheckLowerBound(int offset)
      {
         if (offset >= 0)
         {
            return;
         }
         // if (pointer + offset < lowerBound) goto pointerError;
         il.Emit(OpCodes.Ldloc_0);
         il.Emit(OpCodes.Ldc_I4, offset);
         il.Emit(OpCodes.Add);
         il.Emit(OpCodes.Ldloc_2);
         il.Emit(OpCodes.Blt, pointerError);
      }

      public void CheckUpperBound(int offset)
      {
         if (offset <= 0)
         {
            return;
         }
         // if (pointer + offset >= upperBound) goto pointerError;
         il.Emit(OpCodes.Ldloc_0);
         il.Emit(OpCodes.Ldc_I4, offset);
         il.Emit(OpCodes.Add);
         il.Emit(OpCodes.Ldloc_3);
         il.Emit(OpCodes.Bge, pointerError);
      }

      public void InfiniteLoop(bool isConditional)
      {
         if (!isConditional)
         {
            // goto loopError;
            il.Emit(OpCodes.Br, loopError);
            return;
         }
         // if (*pointer != 0) goto loopError;
         il.Emit(OpCodes.Ldloc_0);
         il.Emit(OpCodes.Ldind_U1);
         il.Emit(OpCodes.Brtrue, loopError);
      }

      public void BeginIf()
      {
         // if (*pointer == 0) goto ifZero;
         il.Emit(OpCodes.Ldloc_0);
         il.Emit(OpCodes.Ldind_U1);
         var label = il.DefineLabel();
         il.Emit(OpCodes.Brfalse, label);
         labels.Push(label);
      }

      public void EndIf()
      {
         // ifZero:
         il.MarkLabel(labels.Pop());
      }

      public void BeginLoop()
      {
         // ifNonZero:
         var label = il.DefineLabel();
         il.MarkLabel(label);
         labels.Push(label);
      }

      public void EndLoop(bool isConditional)
      {
         if (!isConditional)
         {
            // goto ifNonZero;
            il.Emit(OpCodes.Br, labels.Pop());
            return;
         }
         // if (*pointer != 0) goto ifNonZero;
         il.Emit(OpCodes.Ldloc_0);
         il.Emit(OpCodes.Ldind_U1);
         il.Emit(OpCodes.Brtrue, labels.Pop());
      }
   }
}
