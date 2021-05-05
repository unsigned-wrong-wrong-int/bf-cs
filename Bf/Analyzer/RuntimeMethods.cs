using System;
using System.Linq;
using System.Reflection;

namespace Bf.Analyzer
{
   class RuntimeMethods
   {
      public MethodInfo Start { get; }
      public MethodInfo End { get; }
      public MethodInfo Read { get; }
      public MethodInfo Write { get; }
      public MethodInfo Abort { get; }

      public RuntimeMethods(Type type)
      {
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
