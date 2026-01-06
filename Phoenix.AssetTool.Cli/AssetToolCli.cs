
namespace AssetTool.Cli
{
    class AssetToolCli
    {
        public const string DefaultPath = "asset-manifest.json";
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            var cmd = CommandRegistry.Get(args[0]);
            if (cmd == null)
            {
                Console.WriteLine($"Unknown command '{args[0]}'");
                return;
            }

            cmd.Execute(args.Skip(1).ToArray());
        }

        private static void PrintHelp()
        {
            Console.WriteLine($"Single fire: phoenix-asset run <command>");
            Console.WriteLine($"Commands available:");
            Console.WriteLine("Init | Creates files");
            Console.WriteLine("List | Lists files in dir");
            Console.WriteLine("Add <path> | Adds an asset to the build");
            Console.WriteLine("Remove <path> | Removes an asset from the build");
            Console.WriteLine("Build | Builds the assets into binary");


        }
    }
}
