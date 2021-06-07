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

            if(OperatingSystem.IsWindows())
            {
                lib_handle = NativeLibrary.Load(Directory.GetCurrentDirectory() + "\\bin\\win64\\tier0.dll");
                print_to_console = (delegate* unmanaged[Cdecl] <IntPtr, void>)NativeLibrary.GetExport(lib_handle, "Msg");
            }
            else
            {
                print_to_console = (delegate* unmanaged[Cdecl]<IntPtr, void>)0;
            }

            writerThread = new Thread(() =>
            {
                while(true)
                {
                    try
                    {
                        LogEvent logEvent = messages.Take(cancellationTokenSource.Token);
                        string message = logEvent.RenderMessage(this.formatProvider);
                        IntPtr c_string = Marshal.StringToCoTaskMemUTF8(message);
                        print_to_console(c_string);
                        Marshal.FreeCoTaskMem(c_string);
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
