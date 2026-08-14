using AssetTool.Cli;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.AssetBuildOptions;
using System;
using System.CommandLine;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Phoenix.AssetTool.Cli
{
    internal static class CommandOptions
    {
        public static Command Setup()
        {
            Argument<string[]> files = new("files")
            {
                Description = "One or more tracked file paths to show options for",
                Arity = ArgumentArity.OneOrMore
            };

            Command command = new("opt", "Show the current load options for tracked assets")
            {
                files
            };

            command.SetAction(res =>
            {
                if (!AssetToolCli.TryLoadManifest(res))
                    return;

                var filePaths = res.GetValue(files);
                if (filePaths == null) return;

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                foreach (var filePath in filePaths)
                {
                    var absolutePath = Path.GetFullPath(filePath);
                    var relative = FileTools.ToRelative(absolutePath);

                    if (relative == null)
                    {
                        Console.Error.WriteLine($"error: '{filePath}' is outside the Content directory.");
                        AssetToolCli.ExitCode = -1;
                        continue;
                    }

                    var asset = Manifest.Assets.FirstOrDefault(a =>
                        a.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase));

                    if (asset == null)
                    {
                        Console.Error.WriteLine($"error: '{relative}' is not tracked. Use 'add' first.");
                        AssetToolCli.ExitCode = -1;
                        continue;
                    }

                    if (!AssetOptions.TryGet(relative, out var stored))
                    {
                        Console.WriteLine($"{relative}: no options stored, defaults will be used.");
                        continue;
                    }

                    Console.WriteLine($"{relative} [{asset.Type}]:");
                    Console.WriteLine(JsonSerializer.Serialize(stored, options));
                }
            });

            return command;
        }
    }
}
