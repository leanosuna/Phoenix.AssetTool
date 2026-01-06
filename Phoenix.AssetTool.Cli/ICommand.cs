using System;
using System.Collections.Generic;
using System.Text;

namespace AssetTool.Cli
{
    public interface ICommand
    {
        string Name { get; }
        void Execute(string[] args);
    }

}
