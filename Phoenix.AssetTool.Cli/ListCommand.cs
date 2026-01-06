using Phoenix.AssetTool.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetTool.Cli
{
    public class ListCommand : ICommand
    {
        public string Name => "list";

        public void Execute(string[] args)
        {
            //foreach (var asset in manifest.Assets)
            //{
            //    Console.WriteLine($"> {asset.Type} {asset.Path}");
            //}
            if(!JsonIOTools.Load(AssetToolCli.DefaultPath, out AssetManifest manifest))
            {
                Console.WriteLine("Asset manifest not found");
                return;
            }
                //var manifest = ;
            var baseDir = manifest.BaseDirectory;

            PrintDirectory(
            baseDir,
            baseDir,
            manifest,
            indent: "",
            isLast: true
        );
        }

        private void PrintDirectory(
            string root,
            string currentDir,
            AssetManifest manifest,
            string indent,
            bool isLast)
        {
            var contentBin = Path.Combine(root, "ContentBin");
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
                    root,
                    path,
                    isDir,
                    manifest,
                    indent,
                    lastEntry
                );

                if (isDir)
                {
                    var nextIndent = indent + (lastEntry ? "   " : "│  ");
                    PrintDirectory(root, path, manifest, nextIndent, lastEntry);
                }
            }
        }


        private void PrintEntry(
            string root,
            string path,
            bool isDir,
            AssetManifest manifest,
            string indent,
            bool isLast)
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

            var relative = Path.GetRelativePath(root, path).Replace('\\', '/');

            if(name.Equals("aztec.fbx"))
                Console.WriteLine($"rel {relative}");

            bool isTracked = manifest.Assets.Any(a =>
                a.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase));

            var builtPath = Path.Combine(root, "ContentBin", relative).Replace('\\', '/');
            var builtPathWithExtension = Path.ChangeExtension(builtPath, ".bin");
            if (name.Equals("aztec.fbx"))
                Console.WriteLine($"build {builtPathWithExtension}");
            
            bool isBuilt = isTracked && File.Exists(builtPathWithExtension);


            Console.Write($"{indent}{branch}");
            if (isBuilt)
                Console.ForegroundColor = ConsoleColor.Cyan;
            else if (isTracked)
                Console.ForegroundColor = ConsoleColor.Green;
            else
                Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine($"{name}");
            Console.ResetColor();
        }



    }

}
