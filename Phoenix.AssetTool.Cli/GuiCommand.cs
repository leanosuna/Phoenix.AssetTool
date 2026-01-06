using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Gui;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetTool.Cli
{
    public class GuiCommand : ICommand
    {
        public string Name => "gui";

        public void Execute(string[] args)
        {
            AssetToolGui.Main();
        }
    }

}
