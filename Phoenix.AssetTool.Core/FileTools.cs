using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Text;

namespace Phoenix.AssetTool.Core
{
    public static class FileTools
    {
        public static readonly Vector4 ColorWhite = new(1f, 1f, 1f, 1f);
        public static readonly Vector4 ColorYellow = new(1f, 1f, 0f, 1f);
        public static readonly Vector4 ColorGreen = new(0f, 1f, 0f, 1f);
        public static readonly Vector4 ColorRed = new(1f, 0f, 0f, 1f);

        static string manifestAbsolutePath = "";
        static string manifestRootDirectory = "";
        static AssetManifest manifest = default!;
        static AssetLoadOptions assetLoadOptions = default!;

        const string AssetLoadOptionsDefaultPath ="asset-load-options.json";
        static string assetLoadOptionsAbsolutePath = "";

        public static void SetManifest(AssetManifest assetManifest, string root)
        {
            manifest = assetManifest;
            manifestAbsolutePath = root;
            manifestRootDirectory = assetManifest.BaseDirectory;

            LoadAssetOptions();
        }
        public static void SaveManifest()
        {
            JsonIOTools.Save(manifestAbsolutePath, manifest);
        }
        public static void SaveAssetOptions()
        {
            JsonIOTools.Save(assetLoadOptionsAbsolutePath, assetLoadOptions);
        }

        public static AssetLoadOptions LoadAssetOptions()
        {
            if (assetLoadOptions == null)
            {
                var optionsPath = Path.Combine(manifestRootDirectory, AssetLoadOptionsDefaultPath).Replace('\\', '/');
                assetLoadOptionsAbsolutePath = optionsPath;
                if (!File.Exists(optionsPath))
                {
                    //Console.WriteLine("created");
                    JsonIOTools.Save(optionsPath, new AssetLoadOptions());
                }

                //Console.WriteLine("loaded");
                JsonIOTools.Load(optionsPath, out assetLoadOptions);
            }
            //Console.WriteLine("accessed");
            return assetLoadOptions;
        }

        public static void ToggleFile(string relative, bool save = true)
        {
            var existing = manifest.Assets
                .FirstOrDefault(a =>
                    a.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                manifest.Assets.Remove(existing);
                return;
            }

            var output = Path.ChangeExtension(
                Path.Combine("ContentBin", relative),
                ".bin"
            ).Replace('\\', '/');

            manifest.Assets.Add(new AssetEntry
            {
                RelativePath = relative,
                OutputFilePath = output,
                Type = GuessType(relative)
            });

            if(save)
                JsonIOTools.Save(manifestAbsolutePath, manifest);
        }

        public static void AddFile(string relative, bool save = true)
        {
            
            var existing = manifest.Assets
                .FirstOrDefault(a =>
                    a.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                return;
            }

            var output = Path.ChangeExtension(
                Path.Combine("ContentBin", relative),
                ".bin"
            ).Replace('\\', '/');

            manifest.Assets.Add(new AssetEntry
            {
                RelativePath = relative,
                OutputFilePath = output,
                Type = GuessType(relative)
            });
            if (save)
                JsonIOTools.Save(manifestAbsolutePath, manifest);
        }
        public static void RemoveFile(string relative, bool save = true)
        {
            var existing = manifest.Assets
                .FirstOrDefault(a =>
                    a.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                return;
            }
            manifest.Assets.Remove(existing);

            if (save)
                JsonIOTools.Save(manifestAbsolutePath, manifest);
        }
        private static Dictionary<string, bool> _addedDirectories = new Dictionary<string, bool>();
        public static void ToggleDirectory(
            string absoluteDir)
        {
            var files = Directory.EnumerateFiles(
                absoluteDir,
                "*.*",
                SearchOption.AllDirectories);

            if (!_addedDirectories.TryGetValue(absoluteDir, out bool added))
            {
                _addedDirectories[absoluteDir] = false;
            }
            _addedDirectories[absoluteDir] = !_addedDirectories[absoluteDir];

            foreach (var file in files)
            {


                if (GuessType(file) == AssetType.Unknown)
                    continue;

                var relative = Path
                    .GetRelativePath(manifestRootDirectory, file)
                    .Replace('\\', '/');

                if (_addedDirectories[absoluteDir])
                    AddFile(relative, false);
                else
                    RemoveFile(relative, false);
            }
            JsonIOTools.Save(manifestAbsolutePath, manifest);
        }

        public static (bool tracked, bool built) VerifyAsset(AssetEntry asset)
        {
            if(asset == null)
                return (false, false);

            var builtPath = Path.Combine(
                manifest.BaseDirectory,
                asset.OutputFilePath
            );

            return (true, File.Exists(builtPath));
        }

        public static Vector4 GetColor(
            AssetEntry? asset)
        {
            (bool tracked, bool built) = VerifyAsset(asset);

            return GetColor(tracked, built);
        }

        public static Vector4 GetColor(
             bool tracked, bool built)
        {
            if (!tracked)
                return ColorWhite;

            return built ? ColorGreen : ColorYellow;
        }

        public static AssetType GuessType(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".fbx" or ".gltf" or ".glb" => AssetType.Model,
                ".png" or ".jpg" or ".jpeg" or ".tga" => AssetType.Texture,
                ".vert" or ".frag" or ".comp" => AssetType.Shader,
                _ => AssetType.Unknown
            };
        }

        public static string ExtractPath(string fileName, string assemblyPath)
        {
            var resourceName = $"Phoenix.AssetTool.Core{assemblyPath}.{fileName}";

            string tempFile = Path.Combine(Path.GetTempPath(), fileName);
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
                using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fileStream);
                }
            }
            return tempFile;
        }
    }
}
