using Phoenix.AssetTool.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetTool.Cli
{
    public class InitCommand : ICommand
    {
        public string Name => "init";

        public void Execute(string[] args)
        {
            if (args.Length > 0 && args[0] == "force")
                File.Delete(AssetToolCli.DefaultPath);

            if (File.Exists(AssetToolCli.DefaultPath))
            {
                Console.WriteLine("Manifest already exists.");
                return;
            }

            JsonIOTools.Save(AssetToolCli.DefaultPath, new AssetManifest());
            Console.WriteLine($"Created {AssetToolCli.DefaultPath}");
        }
    }

}
