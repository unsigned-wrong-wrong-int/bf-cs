using System;
using System.Reflection.Emit;

namespace Bf.Core
{
   class EntryPoint
   {
      readonly DynamicMethod method;
      readonly object instance;

      public ILGenerator Generator { get; }
      public RuntimeMethods Runtime { get; }

      public EntryPoint(Type type, object instance)
      {
         method = new("",
            returnType: null,
            parameterTypes: new[] { type },
            owner: type);
         this.instance = instance;
         Generator = method.GetILGenerator();
         Runtime = new(type);
      }

      public Action Compile() => method.CreateDelegate<Action>(instance);
   }

   static class RuntimeExtension
   {
      public static EntryPoint CreateEntryPoint<R>(this R runtime)
         where R : IRuntime => new(typeof(R), runtime);
   }
}
