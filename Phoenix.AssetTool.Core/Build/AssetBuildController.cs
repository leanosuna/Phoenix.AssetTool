using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core.Build
{
    public static class AssetBuildController
    {
        public static List<AssetBuildStatus> BuildList { get; } = new();
        public static bool IsBuilding { get; private set; }

        private static CancellationTokenSource? _cts;

        public static void StartBuild(AssetManifest manifest, bool rebuild)
        {
            if (IsBuilding)
                return;

            BuildList.Clear();

            foreach (var asset in manifest.Assets)
            {
                BuildList.Add(new AssetBuildStatus
                {
                    Asset = asset
                });
            }

            _cts = new CancellationTokenSource();
            IsBuilding = true;

            _ = RunBuildAsync(manifest, rebuild, _cts.Token);
        }

        public static void Cancel()
        {
            _cts?.Cancel();
        }

        private static async Task RunBuildAsync(
            AssetManifest manifest,
            bool rebuild,
            CancellationToken token)
        {
            try
            {
                await AssetBuildPipeline.BuildAsync(
                    manifest,
                    BuildList,
                    rebuild,
                    token);
            }
            finally
            {
                IsBuilding = false;
            }
        }


        public static void StartBuildAsset(AssetManifest manifest, AssetEntry asset, bool rebuild)
        {
            if (IsBuilding)
                return;

            BuildList.Clear();

            BuildList.Add(new AssetBuildStatus
            {
                Asset = asset
            });
            

            _cts = new CancellationTokenSource();
            IsBuilding = true;

            _ = RunBuildAsync(manifest, rebuild, _cts.Token);
        }

    }

}
