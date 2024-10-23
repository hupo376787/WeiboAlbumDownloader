using Sentry;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace WeiboAlbumDownloader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            SentrySdk.Init(o =>
            {
                // Tells which project in Sentry to send events to:
                o.Dsn = "https://dd07c62f4012a941162c1101ab58cf06@o1189682.ingest.us.sentry.io/4508093180411904";
                // When configuring for the first time, to see what the SDK is doing:
                o.Debug = true;
                // Set TracesSampleRate to 1.0 to capture 100% of transactions for tracing.
                // We recommend adjusting this value in production.
                o.TracesSampleRate = 1.0;
            });
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            SentrySdk.AddBreadcrumb(
                message: $"weibo.com/u/{GlobalVar.gId}，uid: {GlobalVar.gId}，DataSource: {GlobalVar.gDataSource}，Page:{GlobalVar.gPage}，SinceId: {GlobalVar.gSinceId}",
                category: "error",
                level: BreadcrumbLevel.Error
            );
            SentrySdk.CaptureException(e.Exception);

            // If you want to avoid the application from crashing:
            e.Handled = true;
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            SentrySdk.ConfigureScope(scope =>
            {
                scope.SetTag("AppName", Assembly.GetExecutingAssembly().GetName().Name!);
                scope.SetTag("DeviceName", Environment.MachineName);
            });
        }

    }
}
