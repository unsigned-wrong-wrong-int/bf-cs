using System.Collections.Generic;

namespace Bf.Analyzer
{
   readonly struct Term
   {
      //   [-> +++ <]      { *(p + 1) = 3 * *p; *p = 0; }
      // ^     ^^^
      // |     Multiplier
      // Node (current cell)

      public byte Multiplier { get; }
      public Node Node { get; }

      public Term(byte multipiler, Node node)
      {
         Multiplier = multipiler;
         Node = node;
      }
   }

   class Node
   {
      // { *p += Value + Terms; }
      // if Overwrite is true then `=` is used instead of `+=`.
      // ShiftRight is used to optimize some special cases of multiplication:
      // [-->+<]
      // {
      //    assert((*p & 1) == 0, "infinite loop");
      //    *(p + 1) += 1 * (*p >> 2);
      //                        ^^^^
      //                        ShiftRight
      //    *p = 0;
      // }

      public Node? Previous { get; set; }

      public byte Value { get; set; }

      public bool Overwrite { get; set; }

      public byte ShiftRight { get; set; }

      public List<Term>? Terms { get; private set; }

      public Node(Node? prev, bool overwrite)
      {
         Previous = prev;
         Value = 0;
         Overwrite = overwrite;
         ShiftRight = 0;
         Terms = null;
      }

      public bool IsConst => !Overwrite && Terms is null;

      public void AddTerm(Term value)
      {
         if (Terms is null)
         {
            Terms = new();
         }
         Terms.Add(value);
      }

      public void SetZero()
      {
         Value = 0;
         Overwrite = true;
         Terms = null;
      }

      public void Prepend(Node prev)
      {
         Previous = prev.Previous;
         if (!Overwrite)
         {
            Value += prev.Value;
            Overwrite = prev.Overwrite;
            if (prev.Terms is {} prevTerms)
            {
               if (Terms is {} terms)
               {
                  prevTerms.AddRange(terms);
               }
               Terms = prevTerms;
            }
         }
      }
   }
}
