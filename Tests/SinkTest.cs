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
            try
            {
                Logger log1 = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
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

                Thread.Sleep(2000);

                string console_log = File.ReadAllText("garrysmod/console.log");

                if (!Regex.IsMatch(console_log, @$"\[Verbose\].+{DebugMessage1}$", RegexOptions.ECMAScript | RegexOptions.Multiline | RegexOptions.Compiled))
                {
                    throw new Exception("Verbose message 1 test failed");
                }

                File.WriteAllText("test-success.txt", "success");

                lua.Print("Tests PASSED!");
            }
            catch (Exception e)
            {
                lua.Print($"ERROR Tests faild: {e.ToString()}");
            }
            finally
            {
                lua.CloseGame();
            }
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

        public static void CloseGame(this ILua lua)
        {
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.GetField(-1, "engine");
            lua.GetField(-1, "CloseServer");
            lua.MCall(0, 0);
            lua.Pop(2);
        }
    }
}
