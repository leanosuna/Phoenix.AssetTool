using AssetTool.Cli;
using Phoenix.AssetTool.Core;
using System;
using System.CommandLine;

namespace Phoenix.AssetTool.Cli
{
    internal static class CommandClean
    {
        public static Command Setup()
        {
            Command command = new("clean", "delete the ContentBin folder and all built assets");

            command.SetAction(res =>
            {
                if (!AssetToolCli.TryLoadManifest(res))
                    return;

                Clean();
            });

            return command;
        }

        public static void Clean()
        {
            FileTools.CleanContentBin();
        }
    }
}
