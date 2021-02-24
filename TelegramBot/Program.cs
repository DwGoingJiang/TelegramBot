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
                    var d = p.GetService<Data>();
                    //d.AddLinkDatasAsync(new string[] { "测试", "关键字", "TEST" }, LinkType.Private, "dwgoing").Wait();
                    var res = d.GetLinkDatasAsync("关键字").Result;
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
