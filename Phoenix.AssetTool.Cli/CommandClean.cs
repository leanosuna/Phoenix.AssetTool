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
            var contentBin = Path.Combine(Manifest.BaseDirectory, "ContentBin");

            if (!Directory.Exists(contentBin))
            {
                Console.WriteLine("ContentBin folder not found. Nothing to clean.");
                return;
            }

            Directory.Delete(contentBin, recursive: true);
            Console.WriteLine("ContentBin folder deleted.");
        }
    }
}
