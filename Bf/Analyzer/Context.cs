namespace Bf.Analyzer
{
   enum EnterBlock : byte
   {
      Always,
      IfNonZero,
   }

   enum ExitBlock : byte
   {
      Always,
      IfZero,
      Never,
   }

   enum PointerMove : byte
   {
      Fixed = 0,
      Forward = 1,
      Backward = 2,
      Variable = Forward | Backward,
   }

   class Context
   {
      public EnterBlock Start { get; set; }
      public ExitBlock End { get; set; }

      public PointerMove Move { get; set; }

      public bool PerformsIO { get; set; }

      public Context(bool isNonZero)
      {
         Start = isNonZero ? EnterBlock.Always : EnterBlock.IfNonZero;
         End = ExitBlock.IfZero;
         Move = PointerMove.Fixed;
         PerformsIO = false;
      }

      public void Close(int offset, Context outer)
      {
         Move |= offset switch {
            > 0 => PointerMove.Forward,
            < 0 => PointerMove.Backward,
            _ => PointerMove.Fixed,
         };
         // outer.Move = (outer.Move, Move) switch {
         //    (Fixed, var move) => move,
         //    (Forward, Fixed or Forward) => Forward,
         //    (Backward, Fixed or Backward) => Backward,
         //    (_, _) => Variable,
         // };
         outer.Move |= Move;
         outer.PerformsIO |= PerformsIO;
      }
   }
}
