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
    /// <summary>
    /// A Serilog Sink which logs events to Garry's Mod game console.
    /// </summary>
    public class GmodSink : ILogEventSink
    {
        IFormatProvider formatProvider;

        LogEventLevel logEventLevel;

        BlockingCollection<LogEvent> messages = new BlockingCollection<LogEvent>();

        CancellationTokenSource cancellationTokenSource;

        Thread writerThread;

        /// <summary>
        /// Initializes a new instance of <see cref="GmodSink"/>.
        /// </summary>
        /// <param name="formatProvider">A format provider to format event message.</param>
        /// <param name="logEventLevel">A minimum level of the events to log.</param>
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

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            messages.Add(logEvent);
        }
    }

    /// <summary>
    /// Extends <see cref="LoggerSinkConfiguration"/> with methods to add Garry's Mod console sinks.
    /// </summary>
    public static class GmodSinkExtensions
    {
        /// <summary>
        /// Write log events to the Garry's Mod console.
        /// </summary>
        /// <param name="loggerConfiguration">Logger sink configuration.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for events passed through the sink.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        public static LoggerConfiguration GmodSink(this LoggerSinkConfiguration loggerConfiguration, LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose, IFormatProvider formatProvider = null)
        {
            return loggerConfiguration.Sink(new GmodSink(formatProvider, restrictedToMinimumLevel));
        }
    }
}
