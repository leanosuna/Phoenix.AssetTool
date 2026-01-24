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

        public static void StartBuild(bool rebuild, Action? onFinish = null)
        {
            if (IsBuilding)
                return;

            BuildList.Clear();

            foreach (var asset in Manifest.Assets)
            {
                BuildList.Add(new AssetBuildStatus
                {
                    Asset = asset
                });
            }

            _cts = new CancellationTokenSource();
            IsBuilding = true;

            _ = RunBuildAsync(rebuild, _cts.Token, onFinish);
        }

        public static void Cancel()
        {
            _cts?.Cancel();
        }

        private static async Task RunBuildAsync(
            bool rebuild,
            CancellationToken token,
            Action? onFinish)
        {
            try
            {
                await AssetBuildPipeline.BuildAsync(
                    BuildList,
                    rebuild,
                    token);
            }
            finally
            {
                IsBuilding = false;
                onFinish?.Invoke();
            }
        }


        public static void StartBuildAsset(AssetEntry asset, bool rebuild, Action? onFinish = null)
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

            _ = RunBuildAsync(rebuild, _cts.Token, onFinish);
        }

    }

}
