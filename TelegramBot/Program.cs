using System;
using System.Threading;

namespace TelegramBot
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                Client.Init("1697435010:AAFV21zb1o0T7-PII3xay9ILtwOT71gk3gc");
                var autoReset = new AutoResetEvent(false);
                Console.CancelKeyPress += (_, _) => autoReset.Set();
                Console.WriteLine("Enter Ctrl + C To Stop");
                autoReset.WaitOne();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
