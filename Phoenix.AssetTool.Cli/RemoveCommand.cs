using Phoenix.AssetTool.Core;

namespace AssetTool.Cli
{
    public class RemoveCommand : ICommand
    {
        public string Name => "remove";

        public void Execute(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: remove <path | .>");
                return;
            }

            var inputPath = args[0];

            var resolvedPath = inputPath == "."
                ? Directory.GetCurrentDirectory()
                : Path.GetFullPath(inputPath);

            if (!JsonIOTools.Load(AssetToolCli.DefaultPath, out AssetManifest manifest))
            {
                Console.WriteLine($"Asset manifest not found");
                return;
            }
            if (File.Exists(resolvedPath))
            {
                RemoveFile(manifest, resolvedPath);
            }
            else if (Directory.Exists(resolvedPath))
            {
                RemoveDirectoryRecursive(manifest, resolvedPath);
            }
            else
            {
                Console.WriteLine($"Path not found: {inputPath}");
                return;
            }

            JsonIOTools.Save(AssetToolCli.DefaultPath, manifest);
        }

        private void RemoveDirectoryRecursive(AssetManifest manifest, string absoluteDir)
        {
            var relativeDir = Path
                .GetRelativePath(manifest.BaseDirectory, absoluteDir)
                .Replace('\\', '/')
                .TrimEnd('/');

            var before = manifest.Assets.Count;

            manifest.Assets.RemoveAll(a =>
                a.RelativePath.StartsWith(relativeDir + "/", StringComparison.OrdinalIgnoreCase));

            var removed = before - manifest.Assets.Count;
            Console.WriteLine($"Removed {removed} asset(s)");
        }

        private void RemoveFile(AssetManifest manifest, string absoluteFilePath)
        {
            var relative = Path
                .GetRelativePath(manifest.BaseDirectory, absoluteFilePath)
                .Replace('\\', '/');

            var removed = manifest.Assets.RemoveAll(a =>
                string.Equals(a.RelativePath, relative, StringComparison.OrdinalIgnoreCase));

            if (removed == 0)
                Console.WriteLine("Asset not found.");
            else
                Console.WriteLine($"Removed {relative}");
        }
    }

}
