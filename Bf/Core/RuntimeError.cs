namespace Bf.Core
{
   enum RuntimeError
   {
      PointerOutOfBounds,
      InfiniteLoop,
   }

   static class RuntimeErrorExtension
   {
      public static string GetMessage(this RuntimeError error) => error switch
         {
            RuntimeError.PointerOutOfBounds => "Pointer out of array bounds",
            RuntimeError.InfiniteLoop => "Infinite loop with no read/write",
            _ => "Unknown error",
         };
   }
}
