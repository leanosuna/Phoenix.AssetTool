using AssetTool.Cli;
using Phoenix.AssetTool.Core;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;

namespace Phoenix.AssetTool.Cli
{
    internal static class CommandInit
    {
        public static Command Setup()
        {
            
            Option<bool> force = new("-force")
            {
                Description = "If found, replace"
            };
            
            Command command = new("init", "create a manifest")
            {
                force,
            };

            command.SetAction(res =>
            {
                var forced = res.GetValue(force);
                if(!AssetToolCli.TryCreateManifest(res, forced))
                {
                    Console.Error.WriteLine("Init failed");
                    return;
                }
                Console.WriteLine("Init OK");

                if (forced)
                    CommandClean.Clean();
            });

            return command;
        }

        public static void ListFiles(bool listAll, string? extFilter)
        {
            PrintDirectory(
                Manifest.BaseDirectory,
                indent: "",
                isLast: true,
                listAll: listAll,
                extFilter: extFilter
                );

        }

        private static void PrintDirectory(string currentDir, string indent, bool isLast, 
            bool listAll, string? extFilter)
        {
            var contentBin = Path.Combine(Manifest.BaseDirectory, "ContentBin");
            if (Path.GetFullPath(currentDir)
                .Equals(Path.GetFullPath(contentBin), StringComparison.OrdinalIgnoreCase))
                return;

            var dirs = Directory.GetDirectories(currentDir);
            var files = Directory.GetFiles(currentDir);

            var entries = new List<(string path, bool isDir)>();
            entries.AddRange(dirs.Select(d => (d, true)));
            entries.AddRange(files.Select(f => (f, false)));


            for (int i = 0; i < entries.Count; i++)
            {
                bool lastEntry = i == entries.Count - 1;
                var (path, isDir) = entries[i];

                PrintEntry(
                    path,
                    isDir,
                    indent,
                    lastEntry,
                    listAll,
                    extFilter
                );

                if (isDir)
                {
                    var nextIndent = indent + (lastEntry ? "   " : "│  ");
                    PrintDirectory(path, nextIndent, lastEntry, listAll, extFilter);
                }
            }

        }

        private static void PrintEntry(string path, bool isDir, string indent, bool isLast,
            bool listAll, string? extFilter)
        {
            var branch = isLast ? "└─ " : "├─ ";
            var name = Path.GetFileName(path);

            

            if (name.Equals(AssetToolCli.DefaultPath))
                return;

            if (isDir)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"{indent}{branch}{name}/");
                Console.ResetColor();
                return;
            }
            if (!string.IsNullOrEmpty(extFilter) && Path.GetExtension(path) != extFilter)
                return;

            var relative = Path.GetRelativePath(Manifest.BaseDirectory, path).Replace('\\', '/');

           
            bool isTracked = Manifest.Assets.Any(a =>
                a.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase));

            var builtPath = Path.Combine(Manifest.BaseDirectory, "ContentBin", relative).Replace('\\', '/');
            var builtPathWithExtension = Path.ChangeExtension(builtPath, ".bin");
            bool isBuilt = isTracked && File.Exists(builtPathWithExtension);


            if (!isTracked && !listAll)
                return;

            Console.Write($"{indent}{branch}");
            if (isBuilt)
                Console.ForegroundColor = ConsoleColor.Cyan;
            else if (isTracked)
                Console.ForegroundColor = ConsoleColor.Green;
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
            }    

            Console.WriteLine($"{name}");
            Console.ResetColor();
        }


    }
}
