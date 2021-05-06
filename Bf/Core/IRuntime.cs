namespace Bf.Core
{
   interface IRuntime
   {
      byte[] Start();
      void End();

      byte Read();
      void Write(byte value);

      void Abort(string message);
   }
}
