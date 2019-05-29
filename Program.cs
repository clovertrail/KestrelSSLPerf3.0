using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace aspnetcoredemo
{
    public class Program
    {
        private const string Port = "Port";
        private const string HttpsEnabled = "Https:Enabled";
        private const string HttpsLocalCertPath = "Https:LocalCertPath";
        private const string HttpsCertPasswd = "Https:CertPasswd";
        private const string LogLevel = "Logging:LogLevel:Default";
        private const int DefaultPort = 8080;

        public static void Main(string[] args)
        {
            ThreadPool.GetMinThreads(out var workerThreads, out var iocpThreads);
            ThreadPool.SetMinThreads(Math.Min(16, workerThreads * 4), Math.Min(16, iocpThreads * 4));

            var logger = NLog.Web.NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();
            try
            {
                logger.Debug("init main");
                CreateWebHostBuilder(args).Build().Run();
            }
            catch (Exception exception)
            {
                //NLog: catch setup errors
                logger.Error(exception, "Stopped program because of exception");
                throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                NLog.LogManager.Shutdown();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseSetting(WebHostDefaults.PreventHostingStartupKey, "true")
                .ConfigureLogging(LoggingConfig)
                .UseNLog()
                .UseKestrel(KestrelConfig)
                .UseStartup<Startup>();

        public static readonly Action<WebHostBuilderContext, KestrelServerOptions> KestrelConfig =
            (context, options) =>
            {
                var config = context.Configuration;
                if (!int.TryParse(config[Port], out var port))
                {
                    port = DefaultPort;
                }
                if (bool.TryParse(config[HttpsEnabled], out bool isHttps) && isHttps)
                {
                    var localCertPath = config[HttpsLocalCertPath];
                    var passwd = config[HttpsCertPasswd];
                    X509Certificate2 cert = null;
                    if (!string.IsNullOrEmpty(localCertPath))
                    {
                        // Use a local cert
                        cert = new X509Certificate2(localCertPath, passwd);
                    }
                    if (cert == null)
                    {
                        throw new InvalidOperationException("Cannot find valid pfx file");
                    }
                    options.ListenAnyIP(port, listenOptions =>
                    {
                        listenOptions.UseHttps(cert);
                    });
                }
                else
                {
                    options.ListenAnyIP(port);
                }
            };

        public static readonly Action<WebHostBuilderContext, ILoggingBuilder> LoggingConfig =
            (context, builder) =>
            {
                builder.ClearProviders();
                if (Enum.TryParse(typeof(LogLevel), context.Configuration[LogLevel], out var logLevel))
                {
                    var level = (LogLevel)logLevel;
                    builder.SetMinimumLevel(level);
                }
            };
    }
}
