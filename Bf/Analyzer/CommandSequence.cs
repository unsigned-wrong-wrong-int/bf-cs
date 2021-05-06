using System.Collections.Generic;

namespace Bf.Analyzer
{
   readonly struct CommandSequence
   {
       readonly Dictionary<int, Cell> cells;
       readonly Queue<(int, Command)> commands;

      public CommandSequence(Dictionary<int, Cell> cells,
         Queue<(int, Command)> commands)
      {
         this.cells = cells;
         this.commands = commands;
      }

      public Enumerator GetEnumerator() => new(this);

      public class Enumerator
      {
         bool initial;
         (int offset, Command command) current;
         Dictionary<int, Cell>.Enumerator cells;
         Queue<(int, Command)>.Enumerator commands;

         public Enumerator(CommandSequence sequence)
         {
            initial = true;
            cells = sequence.cells.GetEnumerator();
            commands = sequence.commands.GetEnumerator();
         }

         public bool MoveNext()
         {
            if (initial)
            {
               if (cells.MoveNext())
               {
                  Cell cell;
                  (current.offset, cell) = cells.Current;
                  current.command = cell.Initializer;
                  return true;
               }
               initial = false;
            }
            if (commands.MoveNext())
            {
               current = commands.Current;
               return true;
            }
            return false;
         }

         public (int offset, Command command) Current => current;
      }
   }
}
