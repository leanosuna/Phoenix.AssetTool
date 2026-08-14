using AssetTool.Cli;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.Build;
using System;
using System.Collections.Generic;
using System.CommandLine;

namespace Phoenix.AssetTool.Cli
{
    internal static class CommandAuto
    {
        public static Command Setup()
        {

            Command command = new("auto", "Automatically track files in the manifest and rebuild changes")
            {
            };

            command.SetAction(static res =>
            {
                if (!AssetToolCli.TryLoadManifest(res))
                {
                    return;
                }

                AssetToolCli.InitGL();
                AssetToolCli.KeepAlive = true;

                Console.WriteLine("Auto mode enabled.");

                foreach(var a in Manifest.Assets)
                    absolutePaths.TryAdd(Path.Combine(Manifest.BaseDirectory.Replace('\\', '/'), a.RelativePath).Replace('\\', '/'), a);
                    

                var mfw = new MultiFileWatcher(absolutePaths.Keys);
                mfw.FileChanged += path => 
                {
                    FileChanged(path); 
                };

            });

            return command;
        }


        static List<AssetEntry> buildList = new();

        static Dictionary<string, AssetEntry> absolutePaths = new();
        private static void FileChanged(string file)
        {
            file = file.Replace('\\', '/');

            if (absolutePaths.TryGetValue(file, out var asset))
            {
                Console.WriteLine($"asset {asset.RelativePath} changed");

                buildList.Add(asset);

                if(asset.Type == AssetType.Shader)
                {
                    var pairAsset = Manifest.Assets.Find(a => FileTools.IsShaderPair(a, asset));
                    if (pairAsset != null)
                        buildList.Add(pairAsset);
                }

                if(AssetBuildController.Status.State != Core.Build.BuildState.BUSY)
                {
                    var buildCpy = new List<AssetEntry>();
                    buildCpy.AddRange(buildList);

                    buildList.Clear();

                    AssetToolCli.EnqueueBuild(buildCpy);
                }

            }

        }
    }
}
