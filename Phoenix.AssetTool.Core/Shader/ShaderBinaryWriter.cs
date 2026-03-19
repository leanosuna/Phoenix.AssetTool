using Phoenix.AssetTool.Core.Build;
using Phoenix.AssetTool.Core.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;


namespace Phoenix.AssetTool.Core.Shader
{
    public static class ShaderBinaryWriter
    {
        public static void Build(AssetBuildStatus status, ShaderLoadOptions options,
            string sourcePath, string outputPath)
        {
            
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            Log.Debug($"shader: {sourcePath}");

            var name = Path.GetFileNameWithoutExtension(sourcePath);
            
            
            Log.Debug($"processing");
            var shData = ProcessShaderSource(sourcePath);

            var sourceDir = Path.GetDirectoryName(sourcePath)!;
            

        }

        public static ShaderData ProcessShaderSource(string sourcePath)
        {
            string shaderSource = File.ReadAllText(sourcePath);

            //vertex[0].Remove(0, 4);
            
            var markVertex = "#vert";
            var markFragment = "#frag";

            string vertex,fragment = "";
            
            var split = shaderSource.Split(markVertex);
            if (split.Length != 2)
                throw new Exception($"marker {markVertex} not found");

            if (split[0].Length == 0)
            {
                
                var split2 = split[1].Split(markFragment);

                if (split2.Length != 2)
                    throw new Exception($"marker {markFragment} not found");
                vertex = split2[0];
                fragment = split2[1];
            }
            else
            {
                var split3 = split[0].Split(markFragment);

                if (split3.Length != 2)
                    throw new Exception($"marker {markFragment} not found");

                vertex = split[1];
                fragment = split3[1];
            }

            return new ShaderData
            {
                Stages = 
                [
                    new ShaderStage { Source = vertex, Kind = ShaderKind.VertexShader },
                    new ShaderStage { Source = fragment, Kind = ShaderKind.FragmentShader}
                ]
            };
        }
    }

    

    public class ShaderData
    {
        public List<ShaderStage> Stages { get; internal set; } = default!;
        
        public ShaderData()
        {

        }
    }

    public class ShaderStage
    {
        public string Source { get; internal set; } = default!;
        public ShaderKind Kind { get; internal set; } = default!;

        public ShaderStage()
        {

        }
    }
}
