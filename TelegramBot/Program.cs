using System;
using DwFramework.Core;
using DwFramework.Core.Plugins;
using DwFramework.ORM;

namespace TelegramBot
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                var host = new ServiceHost(EnvironmentType.Develop, "Config.json");
                host.RegisterLog();
                host.RegisterORMService(configPath: "ORM");
                host.RegisterFromAssemblies();
                host.OnInitialized += p =>
                {
                    var client = new Client("1697435010:AAFV21zb1o0T7-PII3xay9ILtwOT71gk3gc");
                };
                host.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
