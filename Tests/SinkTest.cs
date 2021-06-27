using System;
using GmodNET.API;
using GmodNET.Serilog.Sink;
using System.Diagnostics;
using Serilog.Core;
using Serilog;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

namespace Tests
{
    public class SinkTest : IModule
    {
        public string ModuleName => "Tests for GmodNET.Serilog.Sink";

        public string ModuleVersion => FileVersionInfo.GetVersionInfo(typeof(IModule).Assembly.Location).FileVersion;

        public void Load(ILua lua, bool is_serverside, ModuleAssemblyLoadContext assembly_context)
        {
            Logger log1 = new LoggerConfiguration()
                .WriteTo.GmodSink()
                .CreateLogger();

            string VerboseMessage1 = Guid.NewGuid().ToString();
            string DebugMessage1 = Guid.NewGuid().ToString();
            string InformationMessage1 = Guid.NewGuid().ToString();
            string ErrorMessage1 = Guid.NewGuid().ToString();
            string FatalMessage1 = Guid.NewGuid().ToString();

            log1.Verbose(VerboseMessage1);
            log1.Debug(DebugMessage1);
            log1.Information(InformationMessage1);
            log1.Error(ErrorMessage1);
            log1.Fatal(FatalMessage1);

            Thread.Sleep(1000);

            string console_log;

            try
            {
                console_log = File.ReadAllText("console.log");
            }
            catch(Exception e)
            {
                lua.Print($"ERROR: Unable to read console log file: {e.ToString()}");
                return;
            }

            if(!Regex.IsMatch(console_log, @$"\[Verbose\].+{DebugMessage1}$", RegexOptions.ECMAScript | RegexOptions.Multiline | RegexOptions.Compiled))
            {
                lua.Print("ERROR: Verbose message 1 is incorrect");
                return;
            }

            File.WriteAllText("test-success.txt", "success");
        }

        public void Unload(ILua lua)
        {
            
        }
    }

    public static class Helpers
    {
        public static void Print(this ILua lua, string message)
        {
            lua.PushGlobalTable();
            lua.GetField(-1, "print");
            lua.PushString(message);
            lua.MCall(1, 0);
            lua.Pop(1);
        }
    }
}
