using AssetTool.Cli;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.Build;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;

namespace Phoenix.AssetTool.Cli
{
    internal static class CommandBuild
    {
        public static Command Setup()
        {
            
            Option<bool> optRebuild = new("-force")
            {
                Description = "Force rebuild everything"
            };

            Command command = new("build", "Build all of the files in the manifest")
            {
                optRebuild,
            };

            command.SetAction(async res =>
            {
                if (!AssetToolCli.TryLoadManifest(res))
                {
                    return;
                }
                
                AssetToolCli.StartBuildPendingLoop();

                var buildRes = await AssetBuildController.StartBuild(res.GetValue(optRebuild));

                var resStr = buildRes.State.ToString();

                AssetToolCli.StopBuildPendingLoop();
                Console.WriteLine($"Build {resStr}");
                if (buildRes.State == Core.Build.BuildState.FAILED)
                {
                    Console.WriteLine(buildRes.Message);
                    AssetToolCli.ExitCode = -1;
                }
           

                
            });

            return command;
        }

        

    }
}
