using AssetTool.Cli;
using Phoenix.AssetTool.Core;
using System;
using System.CommandLine;
using System.IO;

namespace Phoenix.AssetTool.Cli
{
    internal static class CommandRemove
    {
        public static Command Setup()
        {
            Argument<string[]> files = new("files")
            {
                Description = "One or more file paths to remove from the manifest",
                Arity = ArgumentArity.OneOrMore
            };

            Command command = new("commandremove", "Remove files from the manifest")
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
                    var relative = Path.GetRelativePath(Manifest.BaseDirectory, absolutePath)
                        .Replace('\\', '/');

                    FileTools.RemoveFile(relative, false);
                    Console.WriteLine($"Removed '{relative}'");
                }

                Manifest.Save();
            });

            return command;
        }
    }
}
