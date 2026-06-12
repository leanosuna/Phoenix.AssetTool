using AssetTool.Cli;
using Phoenix.AssetTool.Core;
using System;
using System.CommandLine;
using System.IO;

namespace Phoenix.AssetTool.Cli
{
    internal static class CommandAdd
    {
        public static Command Setup()
        {
            Argument<string[]> files = new("files")
            {
                Description = "One or more file paths to add to the manifest",
                Arity = ArgumentArity.OneOrMore
            };

            Command command = new("commandadd", "Add files to the manifest")
            {
                files
            };

            command.SetAction(res =>
            {
                if (!AssetToolCli.TryLoadManifest(res))
                    return;

                var filePaths = res.GetValue(files);
                if (filePaths == null) return;

                foreach (var filePath in filePaths)
                {
                    var absolutePath = Path.GetFullPath(filePath);

                    if (!File.Exists(absolutePath))
                    {
                        Console.Error.WriteLine($"error: file '{filePath}' not found.");
                        AssetToolCli.ExitCode = -1;
                        continue;
                    }

                    var relative = Path.GetRelativePath(Manifest.BaseDirectory, absolutePath)
                        .Replace('\\', '/');

                    if (relative.StartsWith(".."))
                    {
                        Console.Error.WriteLine($"error: file '{filePath}' is outside the Content directory.");
                        AssetToolCli.ExitCode = -1;
                        continue;
                    }

                    FileTools.AddFile(relative, false);
                    Console.WriteLine($"Added '{relative}'");
                }
                Manifest.Save();
            });

            return command;
        }
    }
}
