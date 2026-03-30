using Phoenix.AssetTool.Cli;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.AssetBuildOptions;
using Phoenix.AssetTool.Core.Build;
using Silk.NET.Vulkan;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Runtime.CompilerServices;
using System.Text;

namespace AssetTool.Cli
{
    class AssetToolCli
    {
        public const string DefaultPath = "asset-manifest.json";
        public static bool KeepAlive;
        public static bool TryLoadManifest(ParseResult res)
        {
            var manFileInfo = res.GetValue(_argumentManifest);
            if (manFileInfo == null)
            {
                Console.Error.WriteLine("manifest argument empty.\n Usage: pat [manifest path] [command]");
                return false;
            }
            if (!manFileInfo.Exists)
            {
                Console.Error.WriteLine($"manifest not found. \n{manFileInfo} ");
                return false;
            }
            if (!Manifest.Load(manFileInfo.FullName))
            {
                Console.Error.WriteLine($"manifest failed to load.");
                return false;
            }
            return true;
        }
        static Argument<FileInfo> _argumentManifest = default!;
        static void Main(string[] args)
        {
            Manifest.RegisterNotifyAction(() => { AssetOptions.Init(); });

            _argumentManifest = new("manifest")
            {
                Description = "The asset manifest file"
            };
            
            RootCommand rootCommand = new("Register and build assets to be used in your Phoenix project");
            rootCommand.Arguments.Add(_argumentManifest);
            rootCommand.Subcommands.Add(CommandGui.Setup());
            rootCommand.Subcommands.Add(CommandList.Setup());
            rootCommand.Subcommands.Add(CommandBuild.Setup());
            rootCommand.Subcommands.Add(CommandAuto.Setup());

            rootCommand.SetAction(parseResult =>
            {
                var res = parseResult.GetValue(_argumentManifest);

                //Console.WriteLine($"root parsing {res}");

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
                    //Console.SetCursorPosition(0, 0);
                    StringBuilder sb = new StringBuilder();
                    sb.Append($"Building");
                    for (int i = 0; i < loading; i++)
                        sb.Append(".");

                    Console.WriteLine(sb.ToString());

                    loading++;
                    loading %= 4;
                    Thread.Sleep(250);
                }
                //Console.Clear();
            });
        }
        public static void StopBuildPendingLoop()
        {
            pendingLoop = false;
        }

    }
}
