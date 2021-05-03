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
      public byte Value { get; set; }
      public Node? Node { get; set; }
      public List<Target>? Targets { get; private set; }

      Command(CommandType type, byte value, Node? node, List<Target>? targets)
      {
         Type = type;
         Value = value;
         Node = node;
         Targets = targets;
      }

      public void AddTarget(Node node, int offset, byte multiplier)
      {
         if (Targets is null)
         {
            Targets = new();
         }
         Targets.Add(new(node.GetTag(), offset, multiplier));
      }

      public static Command InfiniteLoop() =>
         new(CommandType.InfiniteLoop, 0, null, null);

      public static Command Load(Node node, byte shiftRight) =>
         new(CommandType.Load, shiftRight, node, null);

      public static Command Write(Node node) =>
         new(CommandType.Write, 0, node, null);

      public static Command Write(byte value) =>
         new(CommandType.Write, value, null, null);

      public static Command Read(Node node) =>
         new(CommandType.Read, 0, node, null);
   }
}
