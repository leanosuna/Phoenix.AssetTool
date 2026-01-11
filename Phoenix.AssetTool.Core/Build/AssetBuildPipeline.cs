using Phoenix.AssetTool.Core.AssetBuildOptions;
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
                        var built = BuildAsset(manifest, status, rebuild);
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
                        Console.WriteLine($"{Path.GetFileName(status.Asset.RelativePath)}: {status.Error}");
                    }
                }, token)
            ).ToArray();

            await Task.WhenAll(tasks);
            AssetOptions.Save();
        }


        private static bool BuildAsset(AssetManifest manifest, AssetBuildStatus status, bool rebuild)
        {
            var asset = status.Asset;
            //Thread.Sleep((int)(new Random().NextDouble() * 5000));
            var sourcePath = Path.Combine(
                manifest.BaseDirectory,
                asset.RelativePath);


            var outputPath = Path.Combine(
                manifest.BaseDirectory,
                "ContentBin",
                asset.RelativePath);
            outputPath = Path.ChangeExtension(outputPath, "bin");

            var fileExists = File.Exists(outputPath);
            if (fileExists && !rebuild)
            {
                Console.WriteLine($"skipping {asset.RelativePath}");
                return false;
            }

            switch (asset.Type)
            {
                case AssetType.Model:
                    var modelOptions = AssetOptions.OfModel(asset.RelativePath);
                    ModelBinaryWriter.Build(modelOptions, sourcePath, outputPath);
                    break;

                case AssetType.Texture:
                    var texOptions = AssetOptions.OfTexture(asset.RelativePath);
                    TextureBinaryWriter.Build(status, texOptions, sourcePath, outputPath);
                    break;

                case AssetType.Shader:
                    var shOptions = AssetOptions.OfShader(asset.RelativePath);
                    break;
            }
            return true;
        }
        

    }
}

