using Phoenix.AssetTool.Core.Model;
using Phoenix.AssetTool.Core.Shader;
using Phoenix.AssetTool.Core.Texture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Phoenix.AssetTool.Core
{
    //public static class AssetBuildPipeline
    //{
        

    //    public static void Build(AssetManifest manifest, bool rebuild = false)
    //    {
    //        Console.WriteLine($"Building assets...");
    //        Parallel.ForEach(manifest.Assets, (asset) => 
    //        {
    //            BuildAsset(manifest, asset, rebuild);
    //        });
    //        Console.WriteLine($"Done.");
    //    }

    //    public static void BuildAsset(AssetManifest manifest, AssetEntry asset, bool rebuild = false)
    //    {
            

    //        var sourcePath = Path.Combine(
    //                manifest.BaseDirectory,
    //                asset.RelativePath
    //            );

    //        var outputPath = Path.Combine(
    //            manifest.BaseDirectory,
    //            asset.OutputFilePath
    //        );
    //        var name = Path.GetFileNameWithoutExtension(asset.OutputFilePath);

            
    //        if (!rebuild && File.Exists(asset.OutputFilePath))
    //        {
    //            Console.WriteLine($"{name} already exists, skipping...");
    //        }
    //        Console.WriteLine($"> {name}");
    //        switch (asset.Type)
    //        {
    //            case AssetType.Model:
    //                var modelOptions = GetModelLoadOptions(asset.RelativePath);
    //                ModelBinaryWriter.Build(modelOptions, sourcePath, outputPath); 
    //                break;
    //            case AssetType.Texture: 
    //                var texOptions = GetTextureLoadOptions(asset.RelativePath);
    //                break;
    //            case AssetType.Shader:
    //                var shOptions = GetShaderLoadOptions(asset.RelativePath);
    //                break;

    //        }
    //    }
    //    public static ModelLoadOptions GetModelLoadOptions(string path)
    //    {
    //        var assetLoadOptions = FileTools.LoadAssetOptions();
    //        if(!assetLoadOptions.Models.TryGetValue(path, out var options))
    //        {
    //            options = new ModelLoadOptions();
    //            assetLoadOptions.Models[path] = options;
    //            FileTools.SaveAssetOptions();
    //        }
    //        return options;
    //    }
    //    public static TextureLoadOptions GetTextureLoadOptions(string path)
    //    {
    //        var assetLoadOptions = FileTools.LoadAssetOptions();
    //        if (!assetLoadOptions.Textures.TryGetValue(path, out var options))
    //        {
    //            options = new TextureLoadOptions();
    //            assetLoadOptions.Textures[path] = options;
    //            FileTools.SaveAssetOptions();
    //        }
    //        return options;
    //    }
    //    public static ShaderLoadOptions GetShaderLoadOptions(string path)
    //    {
    //        var assetLoadOptions = FileTools.LoadAssetOptions();
    //        if (!assetLoadOptions.Shaders.TryGetValue(path, out var options))
    //        {
    //            options = new ShaderLoadOptions();
    //            assetLoadOptions.Shaders[path] = options;
    //            FileTools.SaveAssetOptions();
    //        }
    //        return options;
    //    }

    //}

}
