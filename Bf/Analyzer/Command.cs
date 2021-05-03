using System.Collections.Generic;

namespace Bf.Analyzer
{
   enum CommandType
   {
      InfiniteLoop,
      Load,
      Write,
      Read,
   }

   readonly struct Target
   {
      public NodeTag Tag { get; }
      public int Offset { get; }
      public byte Multiplier { get; }

      public Target(NodeTag tag, int offset, byte multiplier)
      {
         Tag = tag;
         Offset = offset;
         Multiplier = multiplier;
      }
   }

   class Command
   {
      public CommandType Type { get; }
      public byte Value { get; }
      public byte ShiftRight { get; }
      public bool IsConditional { get; }
      public Node? Node { get; }
      public List<Target>? Targets { get; private set; }

      Command(CommandType type, bool isConditional)
      {
         Type = type;
         IsConditional = isConditional;
      }

      Command(CommandType type, byte shiftRight, Node node)
      {
         Type = type;
         ShiftRight = shiftRight;
         Node = node;
      }

      Command(CommandType type, byte value)
      {
         Type = type;
         Value = value;
      }

      Command(CommandType type, Node node)
      {
         Type = type;
         Node = node;
      }

      public void AddTarget(Node node, int offset, byte multiplier)
      {
         if (Targets is null)
         {
            Targets = new();
         }
         Targets.Add(new(node.GetTag(), offset, multiplier));
      }

      public static Command InfiniteLoop(bool isConditional) =>
         new(CommandType.InfiniteLoop, isConditional);

      public static Command Load(Node node, byte shiftRight) =>
         new(CommandType.Load, shiftRight, node);

      public static Command Write(byte value) => new(CommandType.Write, value);

      public static Command Write(Node node) => new(CommandType.Write, node);

      public static Command Read(Node node) => new(CommandType.Read, node);
   }
}
