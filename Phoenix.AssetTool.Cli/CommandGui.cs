using AssetTool.Cli;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.Build;
using Phoenix.AssetTool.Gui;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;

namespace Phoenix.AssetTool.Cli
{
    internal static class CommandGui
    {
        public static Command Setup()
        {
            Command command = new("gui", "Start the AssetTool GUI");
            {
            };

            command.SetAction(async res =>
            {
                var found = AssetToolCli.TryLoadManifest(res, true);
                var man = found ? "[manifest found]" : "[manifest not found]";
                Console.WriteLine($"opening GUI {man}");
                AssetToolGui.Main();


            });

            return command;
        }

        

    }
}
