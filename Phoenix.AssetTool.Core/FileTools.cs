using NativeFileDialogNET;
using Phoenix.AssetTool.Core.AssetBuildOptions;
using Phoenix.AssetTool.Core.Model;
using Phoenix.AssetTool.Core.Shader;
using Phoenix.AssetTool.Core.Texture;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
namespace Phoenix.AssetTool.Core
{
    public enum AddFileResult
    {
        Added,
        Exists,
        UnknownType
    }

    public static class FileTools
    {
        public static readonly Vector4 ColorWhite = new(1f, 1f, 1f, 1f); 
        public static readonly Vector4 ColorYellow = new(1f, 1f, 0f, 1f);
        public static readonly Vector4 ColorGreen = new(0f, 1f, 0f, 1f);
        public static readonly Vector4 ColorRed = new(1f, 0f, 0f, 1f);
        public static readonly Vector4 ColorCyan = new(0f, 1f, 1f, 1f);

        public static readonly Vector4 ColorBlack = new(0.1255f, 0.1255f, 0.1255f, 1f);
        public static readonly Vector4 ColorYellowDark = new(.6f, .6f, 0f, 1f);
        public static readonly Vector4 ColorGreenDark = new(0f, .75f, 0f, 1f);
        public static readonly Vector4 ColorRedDark = new(.75f, 0f, 0f, 1f);
        public static readonly Vector4 ColorCyanDark = new(0f, .5f, .5f, 1f);
        public static AssetLoadOptions AssetLoadOptions = default!;

        
        public static void ToggleFile(string relative, bool save = true)
        {
            var existing = Manifest.Assets
                .FirstOrDefault(a =>
                    a.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                Manifest.Assets.Remove(existing);
                return;
            }

            
            Manifest.Assets.Add(new AssetEntry
            {
                RelativePath = relative,
                Type = GuessType(relative)
            });

            if (save)
                Manifest.Save();
        }

        public static AddFileResult AddFile(string relative, bool save = true, bool silent = true)
        {
            var type = GuessType(relative);
            if (type == AssetType.Unknown)
                return AddFileResult.UnknownType;

            var existing = Manifest.Assets
                .FirstOrDefault(a =>
                    a.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
                return AddFileResult.Exists;

            Manifest.Assets.Add(new AssetEntry
            {
                RelativePath = relative,
                Type = type
            });

            if (!silent)
                Console.WriteLine($"added {relative}");
            if (save)
                Manifest.Save();

            return AddFileResult.Added;
        }
        public static void RemoveFile(string relative, bool save = true)
        {
            var existing = Manifest.Assets
                .FirstOrDefault(a =>
                    a.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
                return;
            
            Manifest.Assets.Remove(existing);

            if (save)
                Manifest.Save();
        }
        private static Dictionary<string, bool> _addedDirectories = new Dictionary<string, bool>();
        public static void ToggleDirectory(string absoluteDir)
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
                    .GetRelativePath(Manifest.BaseDirectory, file)
                    .Replace('\\', '/');

                if (relative.StartsWith("ContentBin/", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (_addedDirectories[absoluteDir])
                    AddFile(relative, false);
                else
                    RemoveFile(relative, false);
            }
            Manifest.Save();
        }

        public static int AddDirectory(
            string absoluteDir,
            bool silent = true,
            ModelLoadOptions? modelOptions = null,
            TextureLoadOptions? textureOptions = null,
            ShaderLoadOptions? shaderOptions = null)
        {
            int added = 0;
            var files = Directory.EnumerateFiles(
                absoluteDir,
                "*.*",
                SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var relative = Path
                    .GetRelativePath(Manifest.BaseDirectory, file)
                    .Replace('\\', '/');

                if (relative.StartsWith("ContentBin/", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (relative.StartsWith(".."))
                {
                    if (!silent)
                        Console.Error.WriteLine($"error: file '{file}' is outside the Content directory.");
                    continue;
                }

                if (AddFile(relative, false, silent) == AddFileResult.Added)
                    added++;

                if (modelOptions != null && GuessType(relative) == AssetType.Model)
                    AssetOptions.Set(relative, modelOptions);
                if (textureOptions != null && GuessType(relative) == AssetType.Texture)
                    AssetOptions.Set(relative, textureOptions);
                if (shaderOptions != null && GuessType(relative) == AssetType.Shader)
                    AssetOptions.Set(relative, shaderOptions);
            }
            AssetOptions.Save();
            Manifest.Save();
            return added;
        }

        public static List<string> AddAssetFile(
            string relative,
            ModelLoadOptions? modelOptions,
            TextureLoadOptions? textureOptions)
        {
            var messages = new List<string>();
            var type = GuessType(relative);
            var result = AddFile(relative, save: false, silent: true);

            switch (result)
            {
                case AddFileResult.Added:
                    messages.Add($"Added '{relative}'");
                    break;
                case AddFileResult.Exists:
                    var overridden = (type == AssetType.Model && modelOptions != null) ||
                                     (type == AssetType.Texture && textureOptions != null);
                    if (overridden)
                    {
                        var existing = Manifest.Assets.FirstOrDefault(a =>
                            a.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase));
                        if (existing != null)
                            existing.Type = type;
                        messages.Add($"Updated '{relative}'");
                    }
                    else
                    {
                        messages.Add($"Already tracked '{relative}'");
                    }
                    break;
                case AddFileResult.UnknownType:
                    messages.Add($"error: unsupported asset type for '{relative}'.");
                    break;
            }

            if (type == AssetType.Model && modelOptions != null)
                AssetOptions.Set(relative, modelOptions);
            if (type == AssetType.Texture && textureOptions != null)
                AssetOptions.Set(relative, textureOptions);

            return messages;
        }

        public static void CleanContentBin()
        {
            var contentBin = Path.Combine(Manifest.BaseDirectory, "ContentBin");

            if (!Directory.Exists(contentBin))
            {
                Console.WriteLine("ContentBin folder not found. Nothing to clean.");
                return;
            }

            Directory.Delete(contentBin, recursive: true);
            Console.WriteLine("ContentBin folder deleted.");
        }


        public static (bool tracked, bool built) VerifyAsset(AssetEntry? asset)
        {
            if(asset == null)
                return (false, false);

            var builtPath = Path.Combine(
                Manifest.BaseDirectory,
                "ContentBin",
                asset.RelativePath);
            builtPath = Path.ChangeExtension(builtPath, "bin");

            return (true, File.Exists(builtPath));
        }

        static string[] models = [".fbx", ".gltf", ".glb", ".obj", ".dae", ".stl"];
        static string[] textures = [".png", ".jpg", ".jpeg", ".tga"];
        static string[] shaders = [".glsl", ".shader", ".vert", ".frag", ".comp"];
        static string[] videos = [".mp4", ".webm", ".avi", ".mkv", ".mov"];
        //TODO: verify 3DS, PLY, STL
        //CAD STEP(.stp), IFC, DXF
        //Game Engines    MD2, MD3, MDL, X, B3D, MS3D
        //Motion Capture BVH, ASF/AMC
        //3D Printing	3MF, AMF
        public static AssetType GuessType(string path)
        {
            var type = Path.GetExtension(path).ToLowerInvariant();

            if (models.Contains(type))
                return AssetType.Model;
            if (textures.Contains(type))
                return AssetType.Texture;
            if (shaders.Contains(type))
                return AssetType.Shader;
            //if (videos.Contains(type))
            //    return AssetType.Video;

            return AssetType.Unknown;
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


        public static bool FolderPicker(out string dir)
        {
            dir = "";
            using var dlg = new NativeFileDialog()
            .SelectFolder();

            var result = dlg.Open(out string[]? folders, defaultPath: Environment.CurrentDirectory);
            if (result == DialogResult.Okay && folders != null && folders.Length > 0)
            {
                dir = folders[0].Replace('\\', '/');
                    
                return true;
            }
            return false;
        }
    }
}
