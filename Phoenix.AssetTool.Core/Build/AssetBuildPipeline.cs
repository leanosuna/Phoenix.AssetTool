using Phoenix.AssetTool.Core.AssetBuildOptions;
using Phoenix.AssetTool.Core.Model;
using Phoenix.AssetTool.Core.Shader;
using Phoenix.AssetTool.Core.Texture;
using Phoenix.AssetTool.Core.Video;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core.Build
{
    public static class AssetBuildPipeline
    {
        public static async Task BuildAsync(IReadOnlyList<AssetBuildStatus> buildList, bool rebuild, CancellationToken token = default)
        {
            AssetOptions.Save();

            var taskList = new List<Task>
            {
                Task.Run(() =>
                {
                    ShaderAssetHandler.Build(buildList.Where(status => status.Asset.Type == AssetType.Shader).ToList());
                })
            };

            taskList.AddRange(buildList.Select(status =>
                Task.Run(() =>
                {
                    if (token.IsCancellationRequested)
                        return;
                    if (status.Asset.Type == AssetType.Shader)
                        return;

                    try
                    {
                        status.State = AssetBuildState.Building;
                        var built = BuildAsset(status, rebuild);
                        status.State = built ? AssetBuildState.Built : AssetBuildState.Skipped;
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
            ).ToList());

            await Task.WhenAll(taskList);
            
        }


        private static bool BuildAsset(AssetBuildStatus status, bool rebuild)
        {
            var asset = status.Asset;
            //Thread.Sleep((int)(new Random().NextDouble() * 5000));
            var sourcePath = Path.Combine(
                Manifest.BaseDirectory,
                asset.RelativePath);

            var outputPath = Path.Combine(
                Manifest.BaseDirectory,
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
                    var texNames = ModelBinaryWriter.Build(status, modelOptions, sourcePath, outputPath);
                    AddEmbTexToManifest(asset.RelativePath, texNames);
                    break;

                case AssetType.Texture:
                    var texOptions = AssetOptions.OfTexture(asset.RelativePath);
                    TextureBinaryWriter.Build(status, texOptions, sourcePath, outputPath);
                    break;

                case AssetType.Video:
                    var videoOptions = AssetOptions.OfVideo(asset.RelativePath);
                    VideoBinaryWriter.Build(status, videoOptions, sourcePath, outputPath);
                    break;

                //case AssetType.Shader:
                //    var shOptions = AssetOptions.OfShader(asset.RelativePath);
                //    //ShaderAssetHandler.Build(status, shOptions, sourcePath, outputPath);
                //    ShaderBinaryWriter.Build(status, shOptions, sourcePath, outputPath);
                //    break;
            }
            return true;
        }
        
        private static void AddEmbTexToManifest(string assetPath, List<string> names)
        {
            var assetBaseDir = Path.GetDirectoryName(assetPath)!;
            var texType = AssetType.ExtTexture;
            
            foreach(var name in names)
            {
                var texRelative = Path.Combine(assetBaseDir, $"{name}.bin").Replace("\\","/");
                var entry = new AssetEntry { RelativePath = texRelative, Type = texType};

                if (Manifest.Assets.Any(e => e.Type == texType && e.RelativePath == texRelative))
                    return;
                Manifest.Assets.Add(entry);
            }

            Manifest.Save();
        }
    }
}

