using System;
using Bf.Core;

namespace Bf.Analyzer
{
   class Compiler
   {
      readonly Pointer pointer;

      public Compiler(Pointer pointer)
      {
         this.pointer = pointer;
      }

      public Action Compile<R>(R runtime) where R : IRuntime
      {
         var entryPoint = runtime.CreateEntryPoint();
         new Builder(entryPoint).Emit(pointer);
         return entryPoint.Compile();
      }
   }
}
