using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace LukeBot.UI
{
    public class Interface
    {
        IHost mHost = null;

        public static void ThreadMain()
        {
            Interface iface = new Interface();
            iface.Run();
        }

        public void Run()
        {
            mHost = CreateHostBuilder().Build();
            mHost.Run();
        }

        public void Stop()
        {
            Task stop = mHost.StopAsync();
            stop.Wait();
        }

        public IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    string IP = Common.Utils.GetConfigServerIP();
                    if (IP != Common.Constants.DEFAULT_SERVER_IP)
                    {
                        string[] URLs = new string[]
                        {
                            // TODO readd below with certificates
                            //"https://" + IP + ":443/",
                            "http://" + IP + ":80/",
                        };
                        webBuilder.UseUrls(URLs);
                    }
                    webBuilder.UseStartup<Startup>();
                });
    }
}
