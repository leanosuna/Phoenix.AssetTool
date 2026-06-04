using Phoenix.AssetTool.Cli;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.AssetBuildOptions;
using System.CommandLine;
using System.Text;

namespace AssetTool.Cli
{
    class AssetToolCli
    {
        public const string DefaultPath = "asset-manifest.json";
        public static bool KeepAlive;
        private static Argument<FileInfo> _argumentManifest = default!;
        
        static void Main(string[] args)
        {
            Manifest.RegisterNotifyAction(() => { AssetOptions.Init(); });

            _argumentManifest = new("manifest")
            {
                Description = "The asset manifest file",
                Arity = ArgumentArity.ZeroOrOne                
            };
            
            RootCommand rootCommand = new("Register and build assets to be used in your Phoenix project");
            rootCommand.Arguments.Add(_argumentManifest);
            rootCommand.Subcommands.Add(CommandGui.Setup());
            rootCommand.Subcommands.Add(CommandList.Setup());
            rootCommand.Subcommands.Add(CommandBuild.Setup());
            rootCommand.Subcommands.Add(CommandAuto.Setup());
            rootCommand.Subcommands.Add(CommandInit.Setup());

            rootCommand.SetAction(parseResult =>
            {
                var res = parseResult.GetValue(_argumentManifest);
                if(res == null)
                    Console.Error.WriteLine("manifest argument empty.\n Usage: pat [manifest path] [command]");

            });

            ParseResult parseResult = rootCommand.Parse(args);
            
            parseResult.Invoke();


            if(KeepAlive)
                Console.ReadLine();
        }
        static bool pendingLoop = false;
        public static void StartBuildPendingLoop()
        {
            pendingLoop = true;
            Console.Clear();

            Task.Run(() => {
                int loading = 0;
                while (pendingLoop)
                {
                    Console.Clear();
                    StringBuilder sb = new StringBuilder();
                    sb.Append($"Building");
                    for (int i = 0; i < loading; i++)
                        sb.Append(".");

                    Console.WriteLine(sb.ToString());

                    loading++;
                    loading %= 4;
                    Thread.Sleep(250);
                }
                
            });
        }
        public static void StopBuildPendingLoop()
        {
            pendingLoop = false;
        }

        public static bool TryParse(ParseResult res, out string absolutePath, bool silent = false)
        {
            absolutePath = "";
            FileInfo? manFileInfo = null;
            try
            {
                manFileInfo = res.GetValue(_argumentManifest);

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("manifest argument error.");
                return false;
            }

            if (manFileInfo == null)
            {
                if (!silent)
                    Console.Error.WriteLine("manifest argument empty.\n Usage: pat [manifest path] [command]");
                return false;
            }

            absolutePath = manFileInfo.FullName;

            if (string.IsNullOrEmpty(absolutePath))
            {
                if (!silent)
                    Console.WriteLine("manifest argument empty.\n Usage: pat [manifest path] [command]");

                return false;
            }
            return true;
        }
        public static bool TryLoadManifest(ParseResult res, bool silent = false)
        {
            if (!TryParse(res, out var absolutePath, silent))
                return false;

            if (!File.Exists(absolutePath))
            {
                if (!silent)
                    Console.Error.WriteLine($"manifest file not found at [{absolutePath}] ");
                return false;
            }
            var fileName = Path.GetFileName(absolutePath);

            if (!silent)
                Console.WriteLine($"Loading {fileName}");

            if (!Manifest.Load(absolutePath))
            {
                if (!silent)
                    Console.Error.WriteLine($"manifest failed to load.");
                return false;
            }
            if (!silent)
                Console.WriteLine($"Found {fileName}");

            return true;
        }

        public static bool TryCreateManifest(ParseResult res, bool replaceOnFound = false)
        {
            if (!TryParse(res, out var absolutePath, silent: false))
                return false;

            if (File.Exists(absolutePath))
            {
                if (!replaceOnFound)
                {
                    Console.Error.WriteLine($"manifest file found at [{absolutePath}], use -force if youd like to replace it");

                    return false;
                }

                File.Delete(absolutePath);
            }

            Manifest.CreateAbsolute(absolutePath);

            return true;
        }
    }
}
