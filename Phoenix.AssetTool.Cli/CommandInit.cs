using AssetTool.Cli;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.Shader;
using System;
using System.CommandLine;

namespace Phoenix.AssetTool.Cli
{
    internal static class CommandInit
    {
        public static Command Setup()
        {
            Option<bool> force = new("-force")
            {
                Description = "If found, replace"
            };

            Command command = new("init", "create a manifest")
            {
                force,
            };

            command.SetAction(res =>
            {
                var forced = res.GetValue(force);
                if (!AssetToolCli.TryCreateManifest(res, forced))
                {
                    Console.Error.WriteLine("Init failed");
                    return;
                }
                Console.WriteLine("Init OK");

                GlContext.LoadConfig(Manifest.BaseDirectory);

                if (forced)
                    CommandClean.Clean();
            });

            return command;
        }
    }
}
