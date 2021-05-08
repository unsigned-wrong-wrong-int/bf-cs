namespace Bf.Analyzer
{
   class Node
   {
      // if Overwrite:  { *p = Value; }
      // else        :  { *p += Value; }

      public Node? Previous { get; set; }

      public byte Value { get; set; }

      public bool Overwrite { get; set; }

      NodeTag? tag;

      public Node(Node? prev, bool overwrite)
      {
         Previous = prev;
         Value = 0;
         Overwrite = overwrite;
         tag = null;
      }

      public NodeTag GetTag()
      {
         if (tag is null)
         {
            tag = new();
         }
         return tag;
      }

      public bool IsConst => Overwrite && tag is null;

      public bool IsDependent => tag is not null;

      void RemoveTag()
      {
         if (tag is not null)
         {
            tag.Removed = true;
            tag = null;
         }
      }

      public void Clear(bool overwrite)
      {
         Value = 0;
         Overwrite = overwrite;
         RemoveTag();
      }

      public Node Prepend(Node prev)
      {
         Previous = prev.Previous;
         if (Overwrite)
         {
            prev.RemoveTag();
         }
         else
         {
            Value += prev.Value;
            Overwrite = prev.Overwrite;
         }
         return this;
      }
   }

   class NodeTag
   {
      public bool Removed { get; set; }
   }
}
