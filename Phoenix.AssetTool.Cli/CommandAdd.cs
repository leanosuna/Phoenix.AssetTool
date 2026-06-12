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
                Description = "One or more file paths or directories to add to the manifest",
                Arity = ArgumentArity.OneOrMore
            };

            Command command = new("add", "Add files to the manifest")
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
                    var root = 
                        filePath.Equals(".", StringComparison.InvariantCulture) ||
                        filePath.Equals(Manifest.BaseDirectory, StringComparison.InvariantCultureIgnoreCase);
                    
                    var resolvedPath =  root? Manifest.BaseDirectory : filePath;
                    var absolutePath = Path.GetFullPath(resolvedPath);
                    
                    if (Directory.Exists(absolutePath))
                    {
                        FileTools.AddDirectory(absolutePath, silent: false);
                        if (root)
                            break;
                    }
                    else if (File.Exists(absolutePath))
                    {
                        AddFile(absolutePath);
                    }
                    else
                    {
                        Console.Error.WriteLine($"error: '{filePath}' not found.");
                        AssetToolCli.ExitCode = -1;
                    }
                }
                Manifest.Save();
            });

            return command;
        }

       

        private static void AddFile(string absolutePath)
        {
            var relative = Path.GetRelativePath(Manifest.BaseDirectory, absolutePath)
                .Replace('\\', '/');

            if (relative.StartsWith(".."))
            {
                Console.Error.WriteLine($"error: file '{absolutePath}' is outside the Content directory.");
                AssetToolCli.ExitCode = -1;
                return;
            }

            FileTools.AddFile(relative, save: false, silent: false);
            Console.WriteLine($"Added '{relative}'");
        }
    }
}
