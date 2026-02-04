using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.Build;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetTool.Cli
{
    public class BuildCommand : ICommand
    {
        public string Name => "build";

        public void Execute(string[] args)
        {
            var path = AssetToolCli.DefaultPath;

            if (args.Length == 1)
                path = args[0];

            if (!JsonIOTools.Load(path, out AssetManifest manifest))
            {
                Console.WriteLine("Asset manifest not found.");
                return;
            }
            AssetBuildController.StartBuild(false);
            
            Console.WriteLine("Build finished.");
        }
    }

}
