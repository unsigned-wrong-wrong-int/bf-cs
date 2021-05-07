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

      int offset;
      public PointerMove Move { get; set; }

      public bool PerformsIO { get; set; }

      public Context(bool isNonZero)
      {
         IsConditional = !isNonZero;
         Repetition = Repetition.Ordinary;
         offset = 0;
         Move = PointerMove.Fixed;
         PerformsIO = false;
      }

      public void Continue(int offset) => this.offset += offset;

      public void End(int offset, Context outer)
      {
         Move |= (this.offset += offset) switch
         {
            > 0 => PointerMove.Forward,
            < 0 => PointerMove.Backward,
            _ => PointerMove.Fixed,
         };
         outer.Move |= Move;
         outer.PerformsIO |= PerformsIO;
      }
   }
}
