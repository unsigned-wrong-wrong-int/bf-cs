namespace Bf.Analyzer
{
   enum Repetition : byte
   {
      Ordinary,
      Once,
      Infinite,
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
      public bool IsConditional { get; set; }
      public Repetition Repetition { get; set; }

      public PointerMove Move { get; set; }

      public bool PerformsIO { get; set; }

      public Context(bool isNonZero)
      {
         IsConditional = !isNonZero;
         Repetition = Repetition.Ordinary;
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
