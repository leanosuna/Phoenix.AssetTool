using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Phoenix.AssetTool.Core
{
    public sealed class FileTreeNode
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string Relative { get; set; } = "";
        public bool IsDirectory { get; set; }
        public List<FileTreeNode> Children { get; set; } = new();
        public AssetType Type { get; set; } = AssetType.Unknown;
        public bool Tracked { get; set; }
        public bool Built { get; set; }
    }

    public static class FileTree
    {
        public static FileTreeNode Build(string rootDir)
        {
            if (!Manifest.Loaded)
                throw new InvalidOperationException("A manifest must be loaded before building the file tree.");

            return BuildDirectory(rootDir);
        }

        private static FileTreeNode BuildDirectory(string dir)
        {
            var node = new FileTreeNode
            {
                Name = Path.GetFileName(dir),
                FullPath = dir,
                Relative = Path.GetRelativePath(Manifest.BaseDirectory, dir).Replace('\\', '/'),
                IsDirectory = true
            };

            foreach (var childDir in Directory.GetDirectories(dir))
            {
                if (Path.GetFileName(childDir).Equals("ContentBin", StringComparison.OrdinalIgnoreCase))
                    continue;
                node.Children.Add(BuildDirectory(childDir));
            }

            foreach (var file in Directory.GetFiles(dir))
                node.Children.Add(BuildFile(file));

            return node;
        }

        private static FileTreeNode BuildFile(string file)
        {
            var relative = Path.GetRelativePath(Manifest.BaseDirectory, file).Replace('\\', '/');

            var node = new FileTreeNode
            {
                Name = Path.GetFileName(file),
                FullPath = file,
                Relative = relative,
                Type = FileTools.GuessType(file)
            };

            if (node.Name.Equals(Manifest.Name, StringComparison.OrdinalIgnoreCase))
                return node;

            var asset = Manifest.Assets.FirstOrDefault(a =>
                a.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase));
            (node.Tracked, node.Built) = FileTools.VerifyAsset(asset);

            return node;
        }
    }
}
