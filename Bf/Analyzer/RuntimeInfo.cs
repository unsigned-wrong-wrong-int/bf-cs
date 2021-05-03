using System;
using System.Linq;
using System.Reflection;

namespace Bf.Analyzer
{
   readonly struct RuntimeInfo
   {
      public readonly Type Type;

      public readonly MethodInfo Start;
      public readonly MethodInfo End;
      public readonly MethodInfo Read;
      public readonly MethodInfo Write;
      public readonly MethodInfo Abort;

      public RuntimeInfo(Type type)
      {
         Type = type;
         var map = type.GetInterfaceMap(typeof(IRuntime));
         var methods = map.InterfaceMethods.Zip(map.TargetMethods)
            .ToDictionary(pair => pair.First.Name, pair => pair.Second);
         Start = methods[nameof(IRuntime.Start)];
         End = methods[nameof(IRuntime.End)];
         Read = methods[nameof(IRuntime.Read)];
         Write = methods[nameof(IRuntime.Write)];
         Abort = methods[nameof(IRuntime.Abort)];
      }
   }
}
