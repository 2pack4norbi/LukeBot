using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using LukeBot.Logging;
using LukeBot.Config;
using System.IO;
using Microsoft.AspNetCore.Server.Kestrel.Https;


namespace LukeBot.Endpoint
{
    public class Endpoint
    {
        private static Endpoint mEndpoint = null;
        private static Thread mEndpointThread = null;
        private IWebHost mHost = null;

        private static void ThreadMain()
        {
            mEndpoint = new Endpoint();
            mEndpoint.Run();
        }

        public static void StartThread()
        {
            mEndpointThread = new Thread(ThreadMain);
            mEndpointThread.Start();
        }

        public static void StopThread()
        {
            if (mEndpoint != null)
                mEndpoint.Stop();

            if (mEndpointThread != null)
                mEndpointThread.Join();
        }

        public void Run()
        {
            mHost = CreateHostBuilder().Build();
            mHost.Run();
        }

        public async void Stop()
        {
            if (mHost != null)
                await mHost.StopAsync();
        }

        public IWebHostBuilder CreateHostBuilder()
        {
            IWebHostBuilder builder = WebHost.CreateDefaultBuilder();

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(new LBLoggingProvider());
            });

            string domain;
            string[] URLs;

            if (!Conf.TryGet<string>(Common.Constants.PROP_STORE_HTTPS_DOMAIN_PROP, out domain))
            {
                domain = "localhost";
            }

            if (domain.Contains("localhost"))
            {
                // manually set only localhost
                // we do this path just in case someone prefers to use different-than-default port 5000
                URLs = new string[]
                {
                    "https://" + domain + "/",
                };
            }
            else
            {
                // add defined address + localhost:5000
                URLs = new string[]
                {
                    "https://" + domain + "/",
                    "https://localhost:5000/"
                };
            }

            Logger.Log().Info("Endpoint using host addresses:");
            foreach (string addr in URLs)
            {
                Logger.Log().Info("  - https://" + addr + "/");
            }

            builder.UseUrls(URLs);
            builder.UseStartup<Startup>();
            builder.UseContentRoot(Directory.GetCurrentDirectory() + "/Data/ContentRoot");

            return builder;
        }
    }
}
