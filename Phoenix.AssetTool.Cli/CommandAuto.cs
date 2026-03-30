using AssetTool.Cli;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.Build;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;

namespace Phoenix.AssetTool.Cli
{
    internal static class CommandAuto
    {
        public static Command Setup()
        {

            Command command = new("auto", "Automatically track files in the manifest and rebuild changes")
            {
            };

            command.SetAction(static async res =>
            {
                AssetToolCli.KeepAlive = true;
                if (AssetToolCli.TryLoadManifest(res))
                {
                    StartFileWatcherLoop();

                    foreach(var a in Manifest.Assets)
                        absolutePaths.TryAdd(Path.Combine(Manifest.BaseDirectory.Replace('\\', '/'), a.RelativePath).Replace('\\', '/'), a);
                    

                    var mfw = new MultiFileWatcher(absolutePaths.Keys);
                    mfw.FileChanged += path => 
                    {
                        _ = FileChanged(path); 
                    };
                }

                

            });

            return command;
        }

        static bool watcherLoop = false;
        public static void StartFileWatcherLoop()
        {
            watcherLoop = true;
            Console.Clear();

            Task.Run(() => {
                while (watcherLoop)
                {
                    //Console.WriteLine("asd");
                    Thread.Sleep(1);
                }
            });
        }

        public static bool IsShaderPair(AssetEntry pair, AssetEntry entry)
        {
            var rp1 = pair.RelativePath;
            var rp2 = entry.RelativePath;

            var dir1 = Path.GetDirectoryName(rp1)!;
            var dir2 = Path.GetDirectoryName(rp2)!;

            var ext1 = Path.GetExtension(rp1);
            var ext2 = Path.GetExtension(rp2);

            var n1 = Path.GetFileNameWithoutExtension(rp1);
            var n2 = Path.GetFileNameWithoutExtension(rp2);

            return
                dir1.Equals(dir2, StringComparison.InvariantCultureIgnoreCase) &&
                n1.Equals(n2, StringComparison.InvariantCultureIgnoreCase) &&
                !ext1.Equals(ext2, StringComparison.InvariantCultureIgnoreCase);

        }

        static List<AssetEntry> buildList = new();

        static Dictionary<string, AssetEntry> absolutePaths = new();
        private static async Task FileChanged(string file)
        {
            file = file.Replace('\\', '/');

            //Console.WriteLine($"change detected: {file} ");
            if (absolutePaths.TryGetValue(file, out var asset))
            {
                Console.WriteLine($"asset {asset.RelativePath} changed");

                buildList.Add(asset);

                if(asset.Type == AssetType.Shader)
                {
                    var pairAsset = Manifest.Assets.Find(a => IsShaderPair(a, asset));
                    if (pairAsset != null)
                        buildList.Add(pairAsset);
                }

                if(AssetBuildController.Status.State != Core.Build.BuildState.BUSY)
                {
                    var buildCpy = new List<AssetEntry>();
                    buildCpy.AddRange(buildList);

                    buildList.Clear();

                    AssetToolCli.StartBuildPendingLoop();

                    var buildRes = await AssetBuildController.StartBuild(buildCpy, true);
                    
                    var resStr = buildRes.State.ToString();

                    AssetToolCli.StopBuildPendingLoop();
                    Console.WriteLine($"Build {resStr}");
                    if (buildRes.State == Core.Build.BuildState.FAILED)
                    {
                        Console.WriteLine(buildRes.Message);
                    }

                }

            }

        }
    }
}
