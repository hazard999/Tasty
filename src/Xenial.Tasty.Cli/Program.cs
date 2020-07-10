﻿using StreamJsonRpc;

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

using Xenial.Delicious.Metadata;

using static SimpleExec.Command;

namespace Xenial.Tasty.Tool
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();

            var interactiveCommand = new Command("interactive")
            {
                Handler = CommandHandler.Create<string>(Interactive)
            };
            interactiveCommand.AddAlias("i");
            var arg = new Option<string>("--project");
            arg.AddAlias("-p");
            interactiveCommand.Add(arg);
            rootCommand.Add(interactiveCommand);

            return await rootCommand.InvokeAsync(args);
        }

        static async Task<int> Interactive(string project)
        {
            var path = Path.GetFullPath(project);
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                var directoryName = new DirectoryInfo(path).Name;
                var csProjFileName = Path.Combine(path, $"{directoryName}.csproj");
                if (File.Exists(csProjFileName))
                {
                    Console.WriteLine(csProjFileName);

                    var connectionId = $"TASTY_{Guid.NewGuid()}";

                    using var stream = new NamedPipeServerStream(connectionId, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    var connectionTask = stream.WaitForConnectionAsync();
                    var remoteTask = RunAsync("dotnet", 
                        $"run -p \"{csProjFileName}\" -f netcoreapp3.1",
                        configureEnvironment: (env) =>
                        {
                            env.Add("TASTY_INTERACTIVE", "true");
                            env.Add("TASTY_INTERACTIVE_CON_TYPE", "NamedPipes");
                            env.Add("TASTY_INTERACTIVE_CON_ID", connectionId);
                        }
                    );

                    Console.WriteLine("Connecting. NamedPipeServerStream...");
                    await connectionTask;
                    var server = new TastyServer();
                    var tastyServer = JsonRpc.Attach(stream, server);
                    Console.WriteLine("Connected. NamedPipeServerStream...");
                    
                    await remoteTask;
                    Console.WriteLine("Connected. Sending request...");
                }
            }
            return 0;
        }
    }

    public class TastyServer
    {
        public async Task Report(string @case)
        {
            await Task.CompletedTask;
        }
    }
}
