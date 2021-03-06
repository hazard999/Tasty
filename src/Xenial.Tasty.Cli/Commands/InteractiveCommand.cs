﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using StreamJsonRpc;

using Xenial.Delicious.Cli.Internal;
using Xenial.Delicious.Protocols;

using static SimpleExec.Command;
using static Xenial.Delicious.Utils.PromiseHelper;

namespace Xenial.Delicious.Cli.Commands
{
    public static class InteractiveCommand
    {
        public static async Task<int> Interactive(string project, CancellationToken cancellationToken)
        {
            try
            {
                var path = Path.GetFullPath(project);
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    var directoryName = new DirectoryInfo(path).Name;
                    var csProjFileName = Path.Combine(path, $"{directoryName}.csproj");
                    if (File.Exists(csProjFileName))
                    {
                        Console.WriteLine(csProjFileName);
                        var commander = new TastyCommander()
                            .RegisterReporter(TastyServer.ConsoleReporter.Report)
                            .RegisterReporter(TastyServer.ConsoleReporter.ReportSummary);

                        await commander.BuildProject(path, new Progress<(string line, bool isRunning, int exitCode)>(p =>
                        {
                            Console.WriteLine(p.line);
                        }), cancellationToken);

                        Console.WriteLine("Connecting to remote");
                        var remoteTask = await commander.ConnectToRemote(path, cancellationToken: cancellationToken);
                        Console.WriteLine("Connected to remote");

                        try
                        {
                            var uiTask = Task.Run(async () =>
                            {
                                var commands = await commander.ListCommands(cancellationToken);
                                await Task.Run(async () => await WaitForInput(commands, commander), cancellationToken);
                                Console.WriteLine("UI-Task ended");
                            }, cancellationToken);

                            await Task.WhenAll(remoteTask, uiTask);
                        }
                        catch (TaskCanceledException)
                        {
                            return 0;
                        }
                        catch (SimpleExec.NonZeroExitCodeException e)
                        {
                            return e.ExitCode;
                        }
                        finally
                        {
                            commander.Dispose();
                        }
                    }
                }
                return 0;
            }
            catch (TaskCanceledException)
            {
                return 1;
            }
        }

        static Task WaitForInput(IList<SerializableTastyCommand> commands, TastyCommander commander)
            => Promise(async (resolve) =>
            {
                Func<Task> cancelKeyPress = () => Promise((resolve, reject) =>
                {
                    Console.CancelKeyPress += (_, e) =>
                    {
                        if (e.Cancel)
                        {
                            Console.WriteLine("Cancelling execution...");
                            reject();
                        }
                    };
                });

                Func<Task> endTestPipelineSignaled = () => Promise((resolve) =>
                {
                    commander.EndTestPipelineSignaled = () =>
                    {
                        Console.WriteLine("Pipeline ended.");
                        resolve();
                    };
                });

                Func<Task> testPipelineCompletedSignaled = () => Promise((resolve) =>
                {
                    commander.TestPipelineCompletedSignaled = () =>
                    {
                        Console.WriteLine("Pipeline completed.");
                        resolve();
                    };
                });

                Func<Task<ConsoleKeyInfo>> readConsoleKey = () => Promise<ConsoleKeyInfo>(resolve =>
                {
                    _ = Task.Run(() =>
                    {
                        var info = Console.ReadKey(true);
                        resolve(info);
                    });
                });

                Action writeCommands = () =>
                {
                    Console.WriteLine("Interactive Mode");
                    Console.WriteLine("================");

                    foreach (var c in commands)
                    {
                        Console.WriteLine($"{c.Name} - {c.Description}" + (c.IsDefault ? " (default)" : string.Empty));
                    }

                    Console.WriteLine("c - Cancel");
                    Console.WriteLine("================");
                };

                Func<ConsoleKeyInfo, SerializableTastyCommand?> findCommand = (info) =>
                {
                    var command = info.Key == ConsoleKey.Enter
                        ? commands.FirstOrDefault(c => c.IsDefault)
                        : commands.FirstOrDefault(c => string.Equals(c.Name, info.Key.ToString(), StringComparison.OrdinalIgnoreCase));

                    return command;
                };

                Func<Task<Func<Task>>> readInput = async () =>
                {
                    var info = await readConsoleKey();
                    var command = findCommand(info);
                    if (command != null)
                    {
                        return async () =>
                        {
                            Console.WriteLine($"Executing {command.Name} - {command.Description}");

                            await commander.DoExecuteCommand(new ExecuteCommandEventArgs
                            {
                                CommandName = command.Name
                            });
                        };
                    }
                    if (info.Key == ConsoleKey.C)
                    {
                        return () => Promise(async (_, reject) =>
                        {
                            Console.WriteLine($"Requesting cancellation");

                            await commander.DoRequestCancellation();
                            reject();
                        });
                    }

                    return () => Task.CompletedTask;
                };

                Task waitForInput() => Promise(async (resolve, reject) =>
                {
                    writeCommands();
                    var input = await readInput();

                    var cancel = cancelKeyPress();
                    var endTestPipeline = endTestPipelineSignaled();
                    var completedTestPipleLine = testPipelineCompletedSignaled();

                    try
                    {
                        var inputTask = input();
                        if (inputTask.IsCanceled)
                        {
                            reject();
                            return;
                        }
                        var result = await Task.WhenAny(cancel, endTestPipeline, completedTestPipleLine);
                    }
                    catch (TaskCanceledException)
                    {
                        reject();
                        return;
                    }
                    if (endTestPipeline.IsCompletedSuccessfully)
                    {
                        resolve();
                        return;
                    }

                    await waitForInput();
                });
                await waitForInput();
                resolve();
            });

    }
}
