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
         Builder builder = new(entryPoint);
         builder.Begin();
         var current = pointer;
         do {
            current.Emit(builder);
            current = current.Next;
         } while (current is not null);
         builder.End();
         return entryPoint.Compile();
      }
   }
}
