namespace Bf.Analyzer
{
   enum CellState : byte
   {
      Any,
      Zero,
      NonZero,
   }

   class Context
   {
      public CellState Start { get; set; }
      public CellState End { get; set; }
      public bool PerformsIO { get; set; }

      public Context(bool isNonZero)
      {
         Start = isNonZero ? CellState.NonZero : CellState.Any;
         End = CellState.Any;
         PerformsIO = false;
      }
   }
}
