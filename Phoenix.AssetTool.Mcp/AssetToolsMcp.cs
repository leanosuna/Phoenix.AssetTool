using ModelContextProtocol.Server;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.AssetBuildOptions;
using Phoenix.AssetTool.Core.Build;
using Phoenix.AssetTool.Core.Shader;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace Phoenix.AssetTool.Mcp
{
    [McpServerToolType]
    public static class AssetToolsMcp
    {
        private static readonly SemaphoreSlim _gate = new(1, 1);

        [McpServerTool, Description("Add one or more asset files or directories to a Phoenix asset manifest. Re-adding a tracked file with options overrides its stored options.")]
        public static async Task<string> AddAssets(
            string manifestPath,
            string[] paths,
            bool? extractTextures = null,
            string? flags = null,
            bool? animated = null,
            bool? preTransform = null,
            float? scale = null,
            string[]? animations = null,
            bool? mipmaps = null,
            string? format = null,
            string? wrapS = null,
            string? wrapT = null,
            string? min = null,
            string? mag = null,
            float? anisotropy = null)
        {
            return await Run(() =>
            {
                if (!TryLoadManifest(manifestPath, out var loadError))
                    return loadError;

                var errors = new List<string>();
                var modelOptions = OptionParsers.BuildModelOptions(
                    extractTextures, flags, animated, preTransform, scale, animations,
                    Manifest.BaseDirectory, errors);
                var textureOptions = OptionParsers.BuildTextureOptions(
                    mipmaps, format, wrapS, wrapT, min, mag, anisotropy, errors);

                var sb = new StringBuilder();
                foreach (var error in errors)
                    sb.AppendLine(error);

                foreach (var filePath in paths)
                {
                    var absolutePath = Path.GetFullPath(filePath);

                    if (Directory.Exists(absolutePath))
                    {
                        var relDir = Path.GetRelativePath(Manifest.BaseDirectory, absolutePath).Replace('\\', '/');
                        if (relDir.StartsWith(".."))
                        {
                            sb.AppendLine($"error: directory '{filePath}' is outside the Content directory.");
                            continue;
                        }

                        var added = FileTools.AddDirectory(absolutePath, silent: true, modelOptions, textureOptions);
                        sb.AppendLine($"Added {added} files from '{filePath}'");
                    }
                    else if (File.Exists(absolutePath))
                    {
                        var relative = Path.GetRelativePath(Manifest.BaseDirectory, absolutePath).Replace('\\', '/');
                        if (relative.StartsWith(".."))
                        {
                            sb.AppendLine($"error: file '{filePath}' is outside the Content directory.");
                            continue;
                        }

                        foreach (var message in FileTools.AddAssetFile(relative, modelOptions, textureOptions))
                            sb.AppendLine(message);
                    }
                    else
                    {
                        sb.AppendLine($"error: '{filePath}' not found.");
                    }
                }

                AssetOptions.Save();
                Manifest.Save();
                return sb.ToString().TrimEnd();
            });
        }

        [McpServerTool, Description("Remove asset files from a Phoenix asset manifest.")]
        public static async Task<string> RemoveAssets(string manifestPath, string[] paths)
        {
            return await Run(() =>
            {
                if (!TryLoadManifest(manifestPath, out var loadError))
                    return loadError;

                var sb = new StringBuilder();
                foreach (var path in paths)
                {
                    var absolutePath = Path.GetFullPath(path);
                    var relative = Path.GetRelativePath(Manifest.BaseDirectory, absolutePath).Replace('\\', '/');

                    if (relative.StartsWith(".."))
                    {
                        sb.AppendLine($"error: '{path}' is outside the Content directory.");
                        continue;
                    }
                    if (!File.Exists(absolutePath))
                    {
                        sb.AppendLine($"error: '{path}' not found.");
                        continue;
                    }

                    FileTools.RemoveFile(relative, false);
                    sb.AppendLine($"Removed '{relative}'");
                }

                Manifest.Save();
                return sb.ToString().TrimEnd();
            });
        }

        [McpServerTool, Description("List the assets tracked in a Phoenix asset manifest, with their type and build status.")]
        public static async Task<string> ListAssets(string manifestPath)
        {
            return await Run(() =>
            {
                if (!TryLoadManifest(manifestPath, out var loadError))
                    return loadError;

                if (Manifest.Assets.Count == 0)
                    return "No assets tracked.";

                var sb = new StringBuilder();
                sb.AppendLine("path\ttype\tbuilt");
                foreach (var asset in Manifest.Assets)
                {
                    var (_, built) = FileTools.VerifyAsset(asset);
                    sb.AppendLine($"{asset.RelativePath}\t{asset.Type}\t{built}");
                }
                return sb.ToString().TrimEnd();
            });
        }

        [McpServerTool, Description("Build all tracked assets (or a subset given by paths) in a Phoenix asset manifest into ContentBin.")]
        public static async Task<string> BuildAssets(string manifestPath, bool rebuild = false, string[]? paths = null)
        {
            return await RunAsync(async () =>
            {
                if (!TryLoadManifest(manifestPath, out var loadError))
                    return loadError;

                var assets = Manifest.Assets.ToList();
                if (paths != null && paths.Length > 0)
                {
                    var wanted = paths
                        .Select(p => Path.GetRelativePath(Manifest.BaseDirectory, Path.GetFullPath(p)).Replace('\\', '/'))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    assets = assets.Where(a => wanted.Contains(a.RelativePath)).ToList();
                    if (assets.Count == 0)
                        return "error: no tracked assets matched the given paths.";
                }

                var needsShader = assets.Any(a => a.Type == AssetType.Shader);
                var glOk = !needsShader || GlContext.InitHiddenWindow(Manifest.BaseDirectory);
                var buildAssets = assets.Where(a => a.Type != AssetType.Shader || glOk).ToList();

                var sb = new StringBuilder();
                if (needsShader && !glOk)
                    sb.AppendLine("error: could not initialize an OpenGL context; shaders were skipped.");

                var status = await AssetBuildController.StartBuild(buildAssets, rebuild);
                foreach (var item in status.BuildList)
                {
                    var line = $"{item.Asset.RelativePath}\t{item.State}";
                    if (item.State == AssetBuildState.Failed && !string.IsNullOrEmpty(item.Error))
                        line += $"\t{item.Error}";
                    sb.AppendLine(line);
                }
                return sb.ToString().TrimEnd();
            });
        }

        [McpServerTool, Description("Delete the ContentBin folder for a Phoenix asset manifest.")]
        public static async Task<string> Clean(string manifestPath)
        {
            return await Run(() =>
            {
                if (!TryLoadManifest(manifestPath, out var loadError))
                    return loadError;

                FileTools.CleanContentBin();
                return "ContentBin folder deleted.";
            });
        }

        [McpServerTool, Description("Get the stored load options (JSON) for a tracked asset in a Phoenix asset manifest.")]
        public static async Task<string> GetAssetOptions(string manifestPath, string path)
        {
            return await Run(() =>
            {
                if (!TryLoadManifest(manifestPath, out var loadError))
                    return loadError;

                var relative = ToRelative(path);
                if (relative == null)
                    return $"error: '{path}' is outside the Content directory.";

                if (!AssetOptions.TryGet(relative, out var options))
                    return $"No options stored for '{relative}'. Defaults will be used.";

                return JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true });
            });
        }

        [McpServerTool, Description("Set the load options for a tracked asset in a Phoenix asset manifest. Replaces any existing options for that asset.")]
        public static async Task<string> SetAssetOptions(
            string manifestPath,
            string path,
            bool? extractTextures = null,
            string? flags = null,
            bool? animated = null,
            bool? preTransform = null,
            float? scale = null,
            string[]? animations = null,
            bool? mipmaps = null,
            string? format = null,
            string? wrapS = null,
            string? wrapT = null,
            string? min = null,
            string? mag = null,
            float? anisotropy = null)
        {
            return await Run(() =>
            {
                if (!TryLoadManifest(manifestPath, out var loadError))
                    return loadError;

                var relative = ToRelative(path);
                if (relative == null)
                    return $"error: '{path}' is outside the Content directory.";

                var errors = new List<string>();
                var modelOptions = OptionParsers.BuildModelOptions(
                    extractTextures, flags, animated, preTransform, scale, animations,
                    Manifest.BaseDirectory, errors);
                var textureOptions = OptionParsers.BuildTextureOptions(
                    mipmaps, format, wrapS, wrapT, min, mag, anisotropy, errors);

                var sb = new StringBuilder();
                foreach (var error in errors)
                    sb.AppendLine(error);

                if (modelOptions == null && textureOptions == null)
                    return "error: no options provided.";

                var type = Manifest.Assets.FirstOrDefault(a =>
                    a.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase))?.Type
                    ?? FileTools.GuessType(relative);

                if (type == AssetType.Model && modelOptions != null)
                    AssetOptions.Set(relative, modelOptions);
                else if (type == AssetType.Texture && textureOptions != null)
                    AssetOptions.Set(relative, textureOptions);
                else
                    sb.AppendLine($"error: provided option type does not match asset type '{type}' for '{relative}'.");

                AssetOptions.Save();

                if (sb.Length == 0)
                    sb.AppendLine($"Saved options for '{relative}'.");

                return sb.ToString().TrimEnd();
            });
        }

        private static bool TryLoadManifest(string manifestPath, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
            {
                error = $"error: manifest file not found: {manifestPath}";
                return false;
            }

            if (!Manifest.Load(manifestPath))
            {
                error = $"error: failed to load manifest: {manifestPath}";
                return false;
            }

            return true;
        }

        private static string? ToRelative(string path)
        {
            var full = Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(Manifest.BaseDirectory, path));

            var relative = Path.GetRelativePath(Manifest.BaseDirectory, full).Replace('\\', '/');
            return relative.StartsWith("..") ? null : relative;
        }

        private static async Task<string> Run(Func<string> action)
        {
            await _gate.WaitAsync();
            try
            {
                return action();
            }
            finally
            {
                _gate.Release();
            }
        }

        private static async Task<string> RunAsync(Func<Task<string>> action)
        {
            await _gate.WaitAsync();
            try
            {
                return await action();
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
