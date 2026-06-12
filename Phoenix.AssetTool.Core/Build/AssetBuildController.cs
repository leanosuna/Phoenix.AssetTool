using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Text;

namespace Phoenix.AssetTool.Core.Build
{
    public static class AssetBuildController
    {
        //public static List<AssetBuildStatus> BuildList { get; } = new();
        //public static bool IsBuilding { get; private set; }

        private static CancellationTokenSource? _cts;

        public static BuildStatus Status = new();

        public static async Task<BuildStatus> StartBuild(bool rebuild, Action<bool>? onFinish = null)
        {
            return await StartBuild(Manifest.Assets, rebuild, onFinish);
        }

        public static async Task<BuildStatus> StartBuild(AssetEntry asset, bool rebuild, Action<bool>? onFinish = null)
        {
            return await StartBuild([asset], rebuild, onFinish);
        }

        public static async Task<BuildStatus> StartBuild(List<AssetEntry> assets, bool rebuild, Action<bool>? onFinish = null)
        {
            if (Status.State == BuildState.BUSY)
                return Status;

            Status.State = BuildState.BUSY;
            Status.Message = "";
            Status.BuildList.Clear();
            foreach (var asset in assets)
            {
                if (asset.RelativePath.StartsWith("ContentBin/", StringComparison.OrdinalIgnoreCase))
                    continue;

                Console.WriteLine($"Building {asset.RelativePath}");
                Status.BuildList.Add(new AssetBuildStatus
                {
                    Asset = asset
                });
            }

            _cts = new CancellationTokenSource();

            return await RunBuildAsync(rebuild, _cts.Token, onFinish);
        }

        public static void Cancel()
        {
            _cts?.Cancel();
        }

        private static async Task<BuildStatus> RunBuildAsync(bool rebuild, CancellationToken token, Action<bool>? onFinish)
        {
            bool resultOk = true;
            try
            {
                await AssetBuildPipeline.BuildAsync(
                    Status.BuildList,
                    rebuild,
                    token);
            }
            catch (Exception e){
                resultOk = false;
                Console.WriteLine($"{e.Message} {e.StackTrace}");
            }
            finally
            {
                foreach (var a in Status.BuildList)
                {
                    if (a.State == AssetBuildState.Failed)
                    {
                        resultOk = false;

                        Status.Message += $"Error: {a.Asset.RelativePath}";
                        Status.Message += $"\t{a.Error}";
                    }
                }
                
            }

            Status.State = resultOk ? BuildState.OK : BuildState.FAILED;

            onFinish?.Invoke(resultOk);

            return Status;
        }


        

        
    }

}
