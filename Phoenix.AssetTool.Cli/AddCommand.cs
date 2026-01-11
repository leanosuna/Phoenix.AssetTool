using Phoenix.AssetTool.Core;
using System;
using System.IO;
using System.Linq;

namespace AssetTool.Cli
{
    public class AddCommand : ICommand
    {
        public string Name => "add";

        public void Execute(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: add <path | .>");
                return;
            }

            var inputPath = args[0];

            // Resolve "." → current directory
            var resolvedPath = inputPath == "."
                ? Directory.GetCurrentDirectory()
                : Path.GetFullPath(inputPath);

            if (!File.Exists(resolvedPath) && !Directory.Exists(resolvedPath))
            {
                Console.WriteLine($"Path not found: {inputPath}");
                return;
            }
            if (!JsonIOTools.Load(AssetToolCli.DefaultPath, out AssetManifest manifest))
            {
                Console.WriteLine($"Asset manifest not found");
                return;
            }
            
            if (File.Exists(resolvedPath))
            {
                TryAddFile(manifest, resolvedPath);
            }
            else
            {
                AddDirectoryRecursive(manifest, resolvedPath);
            }

            JsonIOTools.Save(AssetToolCli.DefaultPath, manifest);
        }

        private void AddDirectoryRecursive(AssetManifest manifest, string dir)
        {
            var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
                TryAddFile(manifest, file);
        }

        private void TryAddFile(AssetManifest manifest, string absoluteFilePath)
        {
            var type = GuessType(absoluteFilePath);
            if (type == AssetType.Unknown)
                return;

            var relative = Path
                .GetRelativePath(manifest.BaseDirectory, absoluteFilePath)
                .Replace('\\', '/');

            if (manifest.Assets.Any(a =>
                a.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"Already added {relative}");
                return;
            }

            manifest.Assets.Add(new AssetEntry
            {
                RelativePath = relative,
                Type = type
            });

            Console.WriteLine($"Added {relative}");
        }


        private AssetType GuessType(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".fbx" or ".gltf" or ".glb" => AssetType.Model,
                ".png" or ".jpg" or ".jpeg" or ".tga" => AssetType.Texture,
                ".vert" or ".frag" or ".comp" => AssetType.Shader,
                _ => AssetType.Unknown
            };
        }
    }
}
