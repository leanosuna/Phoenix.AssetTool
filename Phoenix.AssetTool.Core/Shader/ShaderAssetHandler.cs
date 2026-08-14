using Phoenix.AssetTool.Core.Build;
using Silk.NET.Core.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace Phoenix.AssetTool.Core.Shader
{
    internal static class ShaderAssetHandler
    {
        //static Dictionary<AssetBuildStatus, AssetBuildStatus?> pairs = new Dictionary<AssetBuildStatus, AssetBuildStatus?>();
        static List<Shader> shaders = new List<Shader>();
        public static void Build(List<AssetBuildStatus> items)
        {
            
            var allPairsFound = FindPairs(items);

            if (!allPairsFound)
            {
                items.ForEach(item =>
                {
                    item.State = item.State != AssetBuildState.Failed ? AssetBuildState.Skipped : AssetBuildState.Failed;
                    item.Error = item.Error == "" ? "Build aborted" : item.Error;
                });

                return;
            }
            foreach (var s in shaders)
            {
                s.ProcessFiles();
                s.StatusA.Step +=1;
                s.StatusB?.Step +=1;
                var name = Path.GetFileNameWithoutExtension(s.StatusA.Asset.RelativePath);
                Log.Debug($"Compiling {name}");


                var outputPath = Path.Combine(
                    Manifest.BaseDirectory,
                    "ContentBin",
                    s.StatusA.Asset.RelativePath);

                var dir = Path.GetDirectoryName(outputPath)!;
                Directory.CreateDirectory(dir);

                var result = GLCompiler.Compile(s.SourceVert, s.SourceFrag);
                if(!result.Success)
                {
                    s.StatusA.State = AssetBuildState.Failed;
                    s.StatusB?.State = AssetBuildState.Failed;

                    s.StatusA.Error = result.ErrorMessage;
                    s.StatusB?.Error = result.ErrorMessage;
                    continue;
                }
                var fileName = Path.Combine(dir, name + ".vert");
                File.WriteAllText(fileName, s.SourceVert);

                fileName = Path.Combine(dir, name + ".frag");
                File.WriteAllText(fileName, s.SourceFrag);

                ShaderHelperClassGenerator.Generate(dir, Manifest.Namespace, name, s.StatusA.Asset.RelativePath, result.UniformsInfo);

                s.StatusA.State = AssetBuildState.Built;
                s.StatusB?.State = AssetBuildState.Built;
                
            }
        }

        static bool FindPairs(List<AssetBuildStatus> items)
        {
            List<int> skipPairIndices = new List<int>();
            List<int> skipIndices = new List<int>();

            for (var i = 0; i < items.Count; i++)
            {
                if (skipIndices.Contains(i))
                    continue;

                var item = items[i];
                var asset = item.Asset;

                var ext = Path.GetExtension(asset.RelativePath);

                if (ext == ".shader" || ext == ".glsl")
                {
                    skipPairIndices.Add(i);
                    shaders.Add(new Shader(item));
                    continue;
                }

                if (i + 1 >= items.Count)
                {
                    item.State = AssetBuildState.Failed;
                    item.Error = "Shader Pair not found";
                    return false;
                }
                var found = false;

                var nextItem = items[i + 1];
                
                var name = Path.GetFileNameWithoutExtension(asset.RelativePath);
                skipPairIndices.Add(i);
                var nextAsset = nextItem.Asset;
                var nextName = Path.GetFileNameWithoutExtension(nextAsset.RelativePath);

                if (name.Equals(nextName, StringComparison.InvariantCultureIgnoreCase))
                {
                    found = true;
                    shaders.Add(new Shader(item, nextItem));
                    skipIndices.Add(i + 1);
                    skipPairIndices.Add(i+1);
                    continue;
                }


                for (var j = 0; j < items.Count; j++)
                {
                    if (skipPairIndices.Contains(j))
                        continue;

                    var pairItem = items[j];
                    var pairAsset = pairItem.Asset;
                    var pairName = Path.GetFileNameWithoutExtension(pairAsset.RelativePath);

                    if (name.Equals(pairName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        found = true;
                        shaders.Add(new Shader(item, pairItem));

                    }
                }
                if (!found)
                {
                    item.State = AssetBuildState.Failed;
                    item.Error = "Shader Pair not found";
                    return false;
                }
            }
            return true;
        }
    }
    
    public class Shader
    {
        public string SourceVert { get; private set; } = "";
        public string SourceFrag { get; private set; } = "";
        public AssetBuildStatus StatusA { get; private set; }
        public AssetBuildStatus StatusB { get; private set; } = default!;

        bool _singleFile = false;
        public Shader(AssetBuildStatus statusA, AssetBuildStatus statusB)
        {
            StatusA = statusA;
            StatusB = statusB;
        }
        public Shader(AssetBuildStatus status)
        {
            StatusA = status;
            _singleFile = true;
        }

        
        public void ProcessFiles()
        {
            var pathA = Path.Combine(
                Manifest.BaseDirectory,
                StatusA.Asset.RelativePath);

            var stageA = File.ReadAllText(pathA);

            if(_singleFile)
            {
                (SourceVert, SourceFrag) = SplitFromSingleFile(stageA);
                return;
            }

            var pathB = Path.Combine(
                Manifest.BaseDirectory,
                StatusB.Asset.RelativePath);

            var stageB = File.ReadAllText(pathB);


            if (Path.GetExtension(pathA) == ".vert")
            {
                SourceVert = stageA;
                SourceFrag = stageB;
            }
            else
            {
                SourceVert = stageB;
                SourceFrag = stageA;
            }
        }
        public (string, string) SplitFromSingleFile(string shaderSource)//add status
        {
            var markVertex = "#vert";
            var markFragment = "#frag";

            string vertex, fragment = "";

            var split = shaderSource.Split(markVertex);
            if (split.Length != 2) //replace with status.state status.errormessage
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
            return (vertex, fragment);
        }
    }

}
