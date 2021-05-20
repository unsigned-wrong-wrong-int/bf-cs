namespace Bf.Analyzer
{
   static class BuilderExtension
   {
      static void Emit(this Builder builder, int offset, Node node)
      {
         builder.AddConst(offset, node.Value, node.Overwrite);
      }

      static void Emit(this Builder builder, int offset, Target target)
      {
         if (target.Valid)
         {
            builder.Add(checked(offset + target.Offset), target.Multiplier);
         }
      }

      static void Emit(this Builder builder, int offset, Command command)
      {
         switch (command.Type)
         {
            case CommandType.InfiniteLoop:
               builder.InfiniteLoop(command.IsConditional);
               return;
            case CommandType.Load:
               builder.Load(offset, command.ShiftRight);
               if (command.Targets is null)
               {
                  break;
               }
               foreach (var target in command.Targets)
               {
                  builder.Emit(offset, target);
               }
               break;
            case CommandType.Write:
               if (command.Node is null)
               {
                  builder.WriteConst(command.Value);
                  return;
               }
               builder.Write(offset);
               break;
            case CommandType.Read:
               builder.Write(offset);
               break;
         }
         // command.Node should be non-null here
         builder.Emit(offset, command.Node!);
      }

      static void BeginBlock(this Builder builder, Pointer pointer)
      {
         if (pointer.IsStartOfLoop)
         {
            if (pointer.Context.IsConditional)
            {
               builder.BeginIf();
            }
            if (pointer.Context.Repetition != Repetition.Once)
            {
               switch (pointer.Context.Move)
               {
                  case PointerMove.Fixed:
                     builder.CheckLowerBound(pointer.MinOffset);
                     builder.CheckUpperBound(pointer.MaxOffset);
                     builder.BeginLoop();
                     return;
                  case PointerMove.Forward:
                     builder.CheckLowerBound(pointer.MinOffset);
                     builder.BeginLoop();
                     builder.CheckUpperBound(pointer.MaxOffset);
                     return;
                  case PointerMove.Backward:
                     builder.CheckUpperBound(pointer.MaxOffset);
                     builder.BeginLoop();
                     builder.CheckLowerBound(pointer.MinOffset);
                     return;
               }
               builder.BeginLoop();
            }
         }
         builder.CheckLowerBound(pointer.MinOffset);
         builder.CheckUpperBound(pointer.MaxOffset);
      }

      static void EndBlock(this Builder builder, Pointer pointer)
      {
         builder.Move(pointer.LastOffset);
         if (!pointer.IsEndOfLoop)
         {
            return;
         }
         if (pointer.Context.Repetition != Repetition.Once)
         {
            builder.EndLoop(pointer.Context.Repetition == Repetition.Ordinary);
         }
         if (pointer.Context.IsConditional)
         {
            builder.EndIf();
         }
      }

      static void EmitBlock(this Builder builder, Pointer pointer)
      {
         builder.BeginBlock(pointer);
         foreach (var (offset, command) in pointer.GetCommands())
         {
            builder.Emit(offset, command);
         }
         builder.EndBlock(pointer);
      }

      public static void Emit(this Builder builder, Pointer pointer)
      {
         builder.Begin();
         do {
            builder.EmitBlock(pointer);
            pointer = pointer.Next!;
         } while (pointer is not null);
         builder.End();
      }
   }
}
