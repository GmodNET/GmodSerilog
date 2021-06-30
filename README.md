# GmodSerilog
A [Serilog](https://serilog.net/) Sink for Garry's Mod clients' and servers' consoles for [GmodDotNet](https://github.com/GmodNET/GmodDotNet) modules.

## Usage
GmodNET.Serilog.Sink can be used with Serilog logger as [any other standard sink](https://github.com/serilog/serilog/wiki/Configuration-Basics) by calling corresponding extension method on `LoggerSinkConfiguration`. Here is an example of usage of GmodNET.Serilog.Sink with [GmodDotNet](https://github.com/GmodNET/GmodDotNet) module:

```csharp
using GmodNET.API;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using GmodNET.Serilog.Sink;

namespace Tests
{
    public class ExampleModule : IModule
    {
        public string ModuleName => "ExampleModule";

        public string ModuleVersion => "1.0.0";
        public void Load(ILua lua, bool is_serverside, ModuleAssemblyLoadContext assembly_context)
        {
            Logger logger = new LoggerConfiguration() // Create a logger configuration
                .MinimumLevel.Information() // Set a global minimal event level for logger to Information
                .WriteTo.GmodSink(restrictedToMinimumLevel: LogEventLevel.Warning) // Add a game console sink which writes only events of Warning severity level and above
                .CreateLogger();

            logger.Warning("Here is a warning to game console!");
        }

        public void Unload(ILua lua)
        {

        }
    }
}
```
