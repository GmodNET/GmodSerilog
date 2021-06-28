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

        string tickEventId;

        public SinkTest()
        {
            tickEventId = Guid.NewGuid().ToString();
        }

        public void Load(ILua lua, bool is_serverside, ModuleAssemblyLoadContext assembly_context)
        {
            lua.PushGlobalTable();
            lua.GetField(-1, "hook");
            lua.GetField(-1, "Add");
            lua.PushString("Tick");
            lua.PushString(tickEventId);
            lua.PushManagedFunction(lua =>
            {
                try
                {
                    lua.Print("Test started");

                    Logger log1 = new LoggerConfiguration()
                        .MinimumLevel.Verbose()
                        .WriteTo.GmodSink()
                        .CreateLogger();

                    string VerboseMessage1 = Guid.NewGuid().ToString();
                    string DebugMessage1 = Guid.NewGuid().ToString();
                    string InformationMessage1 = Guid.NewGuid().ToString();
                    string WarningMessage1 = Guid.NewGuid().ToString();
                    string ErrorMessage1 = Guid.NewGuid().ToString();
                    string FatalMessage1 = Guid.NewGuid().ToString();
                    string FatalWithExceptionMessage1 = Guid.NewGuid().ToString();
                    Exception FatalException1 = new Exception(Guid.NewGuid().ToString());

                    log1.Verbose(VerboseMessage1);
                    log1.Debug(DebugMessage1);
                    log1.Information(InformationMessage1);
                    log1.Warning(WarningMessage1);
                    log1.Error(ErrorMessage1);
                    log1.Fatal(FatalMessage1);
                    log1.Fatal(FatalException1, FatalWithExceptionMessage1);

                    Logger log2 = new LoggerConfiguration()
                        .MinimumLevel.Verbose()
                        .WriteTo.GmodSink(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)
                        .CreateLogger();

                    string InformationMessage2 = Guid.NewGuid().ToString();
                    string WarningMessage2 = Guid.NewGuid().ToString();
                    string ErrorMessage2 = Guid.NewGuid().ToString();

                    log2.Information(InformationMessage2);
                    log2.Warning(WarningMessage2);
                    log2.Error(ErrorMessage2);

                    Thread.Sleep(2000);

                    FileStream fileStream = File.Open("garrysmod/console.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    StreamReader streamReader = new StreamReader(fileStream);

                    string console_log = streamReader.ReadToEnd();

                    if (!Regex.IsMatch(console_log, @$"\[Verbose\].+{VerboseMessage1}$", RegexOptions.ECMAScript | RegexOptions.Multiline | RegexOptions.Compiled))
                    {
                        throw new Exception("Verbose message 1 test failed");
                    }
                    if (!Regex.IsMatch(console_log, @$"\[Debug\].+{DebugMessage1}$", RegexOptions.ECMAScript | RegexOptions.Multiline | RegexOptions.Compiled))
                    {
                        throw new Exception("Debug message 1 test failed");
                    }
                    if (!Regex.IsMatch(console_log, @$"\[Information\].+{InformationMessage1}$", RegexOptions.ECMAScript | RegexOptions.Multiline | RegexOptions.Compiled))
                    {
                        throw new Exception("Information message 1 test failed");
                    }
                    if (!Regex.IsMatch(console_log, @$"\[Warning\].+{WarningMessage1}$", RegexOptions.ECMAScript | RegexOptions.Multiline | RegexOptions.Compiled))
                    {
                        throw new Exception("Warning message 1 test failed");
                    }
                    if (!Regex.IsMatch(console_log, @$"\[Error\].+{ErrorMessage1}$", RegexOptions.ECMAScript | RegexOptions.Multiline | RegexOptions.Compiled))
                    {
                        throw new Exception("Error message 1 test failed");
                    }
                    if (!Regex.IsMatch(console_log, @$"\[Fatal\].+{FatalMessage1}$", RegexOptions.ECMAScript | RegexOptions.Multiline | RegexOptions.Compiled))
                    {
                        throw new Exception("Fatal message 1 test failed");
                    }
                    if (!Regex.IsMatch(console_log, @$"\[Fatal\].+{FatalWithExceptionMessage1}\n{FatalException1.ToString()}$", 
                        RegexOptions.ECMAScript | RegexOptions.Multiline | RegexOptions.Compiled))
                    {
                        throw new Exception("Fatal message with exception 1 test failed");
                    }

                    if (Regex.IsMatch(console_log, @$"\[Information\].+{InformationMessage2}$", RegexOptions.ECMAScript | RegexOptions.Multiline | RegexOptions.Compiled))
                    {
                        throw new Exception("Information message 2 test failed (must not be present in the log)");
                    }
                    if (!Regex.IsMatch(console_log, @$"\[Warning\].+{WarningMessage2}$", RegexOptions.ECMAScript | RegexOptions.Multiline | RegexOptions.Compiled))
                    {
                        throw new Exception("Warning message 2 test failed");
                    }
                    if (!Regex.IsMatch(console_log, @$"\[Error\].+{ErrorMessage2}$", RegexOptions.ECMAScript | RegexOptions.Multiline | RegexOptions.Compiled))
                    {
                        throw new Exception("Error message 2 test failed");
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
                    lua.PushGlobalTable();
                    lua.GetField(-1, "hook");
                    lua.GetField(-1, "Remove");
                    lua.PushString("Tick");
                    lua.PushString(this.tickEventId);
                    lua.MCall(2, 0);
                    lua.Pop(2);

                    lua.CloseGame();
                }

                return 0;
            });
            lua.MCall(3, 0);
            lua.Pop(2);
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
