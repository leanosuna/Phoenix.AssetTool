using System;
using System.Collections.Generic;
using System.Text;

namespace AssetTool.Cli
{
    static class CommandRegistry
    {
        private static readonly Dictionary<string, ICommand> _commands = new();

        static CommandRegistry()
        {
            Register(new InitCommand());
            Register(new ListCommand());
            Register(new AddCommand());
            Register(new RemoveCommand());
            Register(new BuildCommand());
            Register(new GuiCommand());
        }

        public static void Register(ICommand cmd)
            => _commands[cmd.Name] = cmd;

        public static ICommand? Get(string name)
            => _commands.TryGetValue(name, out var cmd) ? cmd : null;
    }

}
