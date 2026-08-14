using AssetTool.Cli;
using Phoenix.AssetTool.Core;
using System;
using System.CommandLine;

namespace Phoenix.AssetTool.Cli
{
    internal static class CommandList
    {
        public static Command Setup()
        {
            Option<bool> filterAll = new("-all")
            {
                Description = "List all of the files, added or not"
            };
            Option<string> filterExt = new("-e")
            {
                Description = "Filter files by extension"
            };

            Command command = new("list", "List all of the files in the manifest")
            {
                filterAll,
                filterExt,
            };

            command.SetAction(res =>
            {
                if (!AssetToolCli.TryLoadManifest(res))
                    return;

                ListFiles(res.GetValue(filterAll), res.GetValue(filterExt));
            });

            return command;
        }

        public static void ListFiles(bool listAll, string? extFilter)
        {
            var root = FileTree.Build(Manifest.BaseDirectory);
            PrintDirectory(root, indent: "", isLast: true, listAll, extFilter);
        }

        private static void PrintDirectory(FileTreeNode node, string indent, bool isLast, bool listAll, string? extFilter)
        {
            for (int i = 0; i < node.Children.Count; i++)
            {
                bool lastEntry = i == node.Children.Count - 1;
                var child = node.Children[i];

                PrintEntry(child, indent, lastEntry, listAll, extFilter);

                if (child.IsDirectory)
                {
                    var nextIndent = indent + (lastEntry ? "   " : "│  ");
                    PrintDirectory(child, nextIndent, lastEntry, listAll, extFilter);
                }
            }
        }

        private static void PrintEntry(FileTreeNode node, string indent, bool isLast, bool listAll, string? extFilter)
        {
            var branch = isLast ? "└─ " : "├─ ";

            if (node.IsDirectory)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"{indent}{branch}{node.Name}/");
                Console.ResetColor();
                return;
            }

            if (!string.IsNullOrEmpty(extFilter) && Path.GetExtension(node.FullPath) != extFilter)
                return;

            if (!node.Tracked && !listAll)
                return;

            Console.Write($"{indent}{branch}");
            if (node.Built)
                Console.ForegroundColor = ConsoleColor.Cyan;
            else if (node.Tracked)
                Console.ForegroundColor = ConsoleColor.Green;
            else
                Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine($"{node.Name}");
            Console.ResetColor();
        }
    }
}
