namespace Bf.Analyzer
{
   enum CommandType
   {
      Read,
      Write,
      WriteConst,
   }

   readonly struct Command
   {
      public CommandType Type { get; }
      public byte Value { get; }
      public Node? Node { get; }

      Command(CommandType type, byte value, Node? node)
      {
         Type = type;
         Value = value;
         Node = node;
      }

      public static Command Read(Node node) =>
         new(CommandType.Read, 0, node);

      public static Command Write(Node node) =>
         new(CommandType.Write, 0, node);

      public static Command Write(byte value) =>
         new(CommandType.WriteConst, value, null);
   }
}
