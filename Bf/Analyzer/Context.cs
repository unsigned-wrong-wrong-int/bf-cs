namespace Bf.Analyzer
{
   enum CellState : byte
   {
      Any,
      Zero,
      NonZero,
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
      public CellState Start { get; set; }
      public CellState End { get; set; }

      public PointerMove Move { get; set; }

      public bool PerformsIO { get; set; }

      public Context(bool isNonZero)
      {
         Start = isNonZero ? CellState.NonZero : CellState.Any;
         End = CellState.Any;
         Move = PointerMove.Fixed;
         PerformsIO = false;
      }

      public void Close(int offset)
      {
         Move |= offset switch {
            > 0 => PointerMove.Forward,
            < 0 => PointerMove.Backward,
            _ => PointerMove.Fixed,
         };
      }

      public void Include(Context inner)
      {
         // (Move, inner.Move) switch {
         //    (Fixed, m) => m,
         //    (Forward, Fixed or Forward) => Forward,
         //    (Backward, Fixed or Backward) => Backward,
         //    (_, _) => Variable,
         // }
         Move |= inner.Move;
         PerformsIO |= inner.PerformsIO;
      }
   }
}
