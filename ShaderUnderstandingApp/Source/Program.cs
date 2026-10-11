using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace ShaderUnderstanding;

internal static class Program {
    [STAThread]
    public static int Main(string[] arguments) {
        SessionLog? log = null;
        try {
            int logIndex = Array.IndexOf(arguments, "--log-directory");
            if (logIndex >= 0 && logIndex + 1 == arguments.Length) throw new ArgumentException("--log-directory requires a path.");
            log = new(logIndex >= 0 ? arguments[logIndex + 1] : null);
            bool verify = arguments.Contains("--verify-interface", StringComparer.Ordinal);
            if (verify) RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            Application application = new() { ShutdownMode = verify ? ShutdownMode.OnExplicitShutdown : ShutdownMode.OnMainWindowClose };
            application.Resources.MergedDictionaries.Add(new() { Source = new Uri("Source/Appearance.xaml", UriKind.Relative) });
            SessionLog session = log;
            application.DispatcherUnhandledException += (_, eventArguments) => {
                session.Write(eventArguments.Exception.ToString());
                eventArguments.Handled = true;
                application.Shutdown(1);
            };
            if (verify) {
                application.Startup += async (_, _) => {
                    try {
                        MainWindow window = new(session);
                        await InterfaceVerification.Run(window, session);
                        session.Write("Offscreen interface verification passed. No window was shown.");
                        window.Close();
                        application.Shutdown(0);
                    } catch (Exception exception) {
                        session.Write(exception.ToString());
                        application.Shutdown(1);
                    }
                };
                return application.Run();
            }
            return application.Run(new MainWindow(session));
        } catch (Exception exception) {
            log?.Write(exception.ToString());
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
