using Phoenix.AssetTool.Core.Model;
using Phoenix.AssetTool.Core.Shader;
using Phoenix.AssetTool.Core.Texture;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core.Build
{
    public static class AssetBuildPipeline
    {
        public static async Task BuildAsync(AssetManifest manifest, IReadOnlyList<AssetBuildStatus> buildList, bool rebuild, CancellationToken token = default)
        {
            var tasks = buildList.Select(status =>
                Task.Run(() =>
                {
                    if (token.IsCancellationRequested)
                        return;

                    try
                    {
                        status.State = AssetBuildState.Building;
                        var built = BuildAsset(manifest, status.Asset, rebuild);
                        status.State = built? AssetBuildState.Built : AssetBuildState.Skipped;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        status.State = AssetBuildState.Failed;
                        status.Error = ex.Message;
                    }
                }, token)
            ).ToArray();

            await Task.WhenAll(tasks);
        }

        private static bool BuildAsset(AssetManifest manifest, AssetEntry asset, bool rebuild)
        {

            //Thread.Sleep((int)(new Random().NextDouble() * 5000));
            var sourcePath = Path.Combine(
                manifest.BaseDirectory,
                asset.RelativePath);

            var outputPath = Path.Combine(
                manifest.BaseDirectory,
                asset.OutputFilePath);
            var fileExists = File.Exists(outputPath);
            if (fileExists && !rebuild)
            {
                Console.WriteLine($"skipping {asset.RelativePath}");
                return false;
            }

            switch (asset.Type)
            {
                case AssetType.Model:
                    var modelOptions = GetModelLoadOptions(asset.RelativePath);
                    ModelBinaryWriter.Build(modelOptions, sourcePath, outputPath);
                    break;

                case AssetType.Texture:
                    var texOptions = GetTextureLoadOptions(asset.RelativePath);
                    break;

                case AssetType.Shader:
                    var shOptions = GetShaderLoadOptions(asset.RelativePath);
                    break;
            }
            return true;
        }

        public static ModelLoadOptions GetModelLoadOptions(string path)
        {
            var assetLoadOptions = FileTools.LoadAssetOptions();
            if (!assetLoadOptions.Models.TryGetValue(path, out var options))
            {
                options = new ModelLoadOptions();
                assetLoadOptions.Models[path] = options;
                FileTools.SaveAssetOptions();
            }
            return options;
        }
        public static TextureLoadOptions GetTextureLoadOptions(string path)
        {
            var assetLoadOptions = FileTools.LoadAssetOptions();
            if (!assetLoadOptions.Textures.TryGetValue(path, out var options))
            {
                options = new TextureLoadOptions();
                assetLoadOptions.Textures[path] = options;
                FileTools.SaveAssetOptions();
            }
            return options;
        }
        public static ShaderLoadOptions GetShaderLoadOptions(string path)
        {
            var assetLoadOptions = FileTools.LoadAssetOptions();
            if (!assetLoadOptions.Shaders.TryGetValue(path, out var options))
            {
                options = new ShaderLoadOptions();
                assetLoadOptions.Shaders[path] = options;
                FileTools.SaveAssetOptions();
            }
            return options;
        }
    }
}

