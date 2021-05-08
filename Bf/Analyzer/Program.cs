using System;
using Bf.Core;

namespace Bf.Analyzer
{
   class Program
   {
      public Pointer Pointer { get; }

      public Program(Pointer pointer) => Pointer = pointer;

      public Action Compile<R>(R runtime) where R : IRuntime
      {
         var entryPoint = runtime.CreateEntryPoint();
         new Builder(entryPoint).Emit(Pointer);
         return entryPoint.Compile();
      }
   }
}
