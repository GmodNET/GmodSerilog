using System;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;
using System.Threading;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.IO;
using System.Runtime.Loader;
using System.Diagnostics;

namespace GmodNET.Serilog.Sink
{
    public class GmodSink : ILogEventSink
    {
        IFormatProvider formatProvider;

        LogEventLevel logEventLevel;

        BlockingCollection<LogEvent> messages = new BlockingCollection<LogEvent>();

        CancellationTokenSource cancellationTokenSource;

        Thread writerThread;

        public unsafe GmodSink(IFormatProvider formatProvider, LogEventLevel logEventLevel)
        {
            this.formatProvider = formatProvider;

            this.logEventLevel = logEventLevel;

            cancellationTokenSource = new CancellationTokenSource();

            IntPtr lib_handle;

            delegate* unmanaged[Cdecl] <IntPtr, void> print_to_console;
            delegate* unmanaged[Cdecl]<IntPtr, void> warning_to_console;

            if (OperatingSystem.IsWindows())
            {
                lib_handle = NativeLibrary.Load(Directory.GetCurrentDirectory() + "\\bin\\win64\\tier0.dll");
            }
            else if (OperatingSystem.IsMacOS())
            {
                lib_handle = NativeLibrary.Load(Directory.GetCurrentDirectory() + "/GarrysMod_Signed.app/Contents/MacOS/libtier0.dylib");
            }
            else if(OperatingSystem.IsLinux())
            {
                if (!NativeLibrary.TryLoad(Directory.GetCurrentDirectory() + "/bin/linux64/libtier0.so", out lib_handle))
                {
                    lib_handle = NativeLibrary.Load(Directory.GetCurrentDirectory() + "/bin/linux64/libtier0_client.so");
                }
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            print_to_console = (delegate* unmanaged[Cdecl]<IntPtr, void>)NativeLibrary.GetExport(lib_handle, "Msg");
            warning_to_console = (delegate* unmanaged[Cdecl]<IntPtr, void>)NativeLibrary.GetExport(lib_handle, "Warning");

            writerThread = new Thread(() =>
            {
                while(true)
                {
                    try
                    {
                        LogEvent logEvent = messages.Take(cancellationTokenSource.Token);
                        if (logEvent.Level >= this.logEventLevel)
                        {
                            string message = $"[{logEvent.Timestamp.ToString(this.formatProvider)}] ";
                            message += logEvent.Level switch
                            {
                                LogEventLevel.Verbose => "[Verbose] ",
                                LogEventLevel.Debug => "[Debug] ",
                                LogEventLevel.Information => "[Information] ",
                                LogEventLevel.Warning => "[Warning] ",
                                LogEventLevel.Error => "[Error] ",
                                LogEventLevel.Fatal => "[Fatal] ",
                                _ => "[Unknown log level] "
                            };
                            if (OperatingSystem.IsLinux() && Process.GetCurrentProcess().ProcessName == "srcds")
                            {
                                if (logEvent.Level == LogEventLevel.Warning)
                                {
                                    message = "\u001B[38;5;11m" + message + "\u001B[0m";
                                }
                                else if (logEvent.Level >= LogEventLevel.Error)
                                {
                                    message = "\u001B[38;5;9m" + message + "\u001B[0m";
                                }
                                else if (logEvent.Level == LogEventLevel.Information)
                                {
                                    message = "\u001B[38;5;10m" + message + "\u001B[0m";
                                }
                                else
                                {
                                    message = "\u001B[38;5;14m" + message + "\u001B[0m";
                                }
                            }
                            message += logEvent.RenderMessage(this.formatProvider) + "\n";
                            if(logEvent.Exception is not null)
                            {
                                message += logEvent.Exception.ToString() + "\n";
                            }
                            IntPtr c_string = Marshal.StringToCoTaskMemUTF8(message);
                            if (logEvent.Level >= LogEventLevel.Warning)
                            {
                                warning_to_console(c_string);
                            }
                            else
                            {
                                print_to_console(c_string);
                            }
                            Marshal.FreeCoTaskMem(c_string);
                        }
                    }
                    catch(OperationCanceledException)
                    {
                        break;
                    }
                }
            });

            writerThread.Start();

            AssemblyLoadContext module_context = AssemblyLoadContext.GetLoadContext(typeof(GmodSink).Assembly);
            module_context.Unloading += _ =>
            {
                cancellationTokenSource.Cancel();
                writerThread.Join();
            };
        }

        public void Emit(LogEvent logEvent)
        {
            messages.Add(logEvent);
        }
    }

    public static class GmodSinkExtensions
    {
        public static LoggerConfiguration GmodSink(this LoggerSinkConfiguration loggerConfiguration, IFormatProvider formatProvider = null)
        {
            return loggerConfiguration.Sink(new GmodSink(formatProvider, LogEventLevel.Verbose));
        }
    }
}
