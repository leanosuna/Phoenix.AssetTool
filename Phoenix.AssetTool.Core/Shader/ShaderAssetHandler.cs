using Phoenix.AssetTool.Core.Build;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace Phoenix.AssetTool.Core.Shader
{
    internal static class ShaderAssetHandler
    {
        static Dictionary<AssetEntry, AssetEntry?> pairs = new Dictionary<AssetEntry, AssetEntry?>();
        public static void Build(List<AssetBuildStatus> items)
        {
            var allPairsFound = GeneratePairs(items);

            if (!allPairsFound)
            {
                items.ForEach(item =>
                {
                    item.State = item.State != AssetBuildState.Failed ? AssetBuildState.Skipped : AssetBuildState.Failed;
                    item.Error = item.Error == "" ? "Build aborted" : item.Error;
                });

                return;
            }


        }

        static bool GeneratePairs(List<AssetBuildStatus> items)
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

                if (ext == "shader")
                {
                    skipPairIndices.Add(i);
                    pairs.Add(asset, null);
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(asset.RelativePath);
                skipPairIndices.Add(i);

                var found = false;
                for (var j = 0; j < items.Count; i++)
                {
                    if (skipPairIndices.Contains(j))
                        continue;

                    var pairItem = items[i];
                    var pairAsset = pairItem.Asset;
                    var pairName = Path.GetFileNameWithoutExtension(pairAsset.RelativePath);

                    if (name.Equals(pairName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        found = true;
                        pairs.Add(asset, pairAsset);

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
    

}
