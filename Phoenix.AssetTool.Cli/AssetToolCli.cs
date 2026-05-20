using FFMpegCore;
using FFMpegCore.Extensions.Downloader;
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
        public static bool TryLoadManifest(ParseResult res, bool silent = false)
        {
            FileInfo? manFileInfo = null;
            try
            {
                manFileInfo = res.GetValue(_argumentManifest);
            }
            catch(Exception e)
            {
                Console.Error.WriteLine("manifest argument error.");
                return false;
            }

            if (manFileInfo == null)
            {
                if(!silent)
                    Console.Error.WriteLine("manifest argument empty.\n Usage: pat [manifest path] [command]");
                return false;
            }


            var absolutePath = manFileInfo.FullName;
            if(string.IsNullOrEmpty(absolutePath))
            {
                if (!silent)
                    Console.WriteLine("manifest argument empty.\n Usage: pat [manifest path] [command]");

                return false;
            }
            

            if (!File.Exists(absolutePath))
            {
                if (!silent)
                    Console.Error.WriteLine($"manifest file not found at [{manFileInfo}] ");
                return false;
            }
            var fileName = Path.GetFileName(absolutePath);

            if(!silent)
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
        static Argument<FileInfo> _argumentManifest = default!;
        static void Main(string[] args)
        {
            InitFFMpeg();

            Manifest.RegisterNotifyAction(() => { AssetOptions.Init(); });

            _argumentManifest = new("manifest")
            {
                Description = "The asset manifest file",
                Arity = ArgumentArity.ZeroOrOne
                //DefaultValueFactory = ()=> { return new FileInfo("a"); }
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
        static void InitFFMpeg()
        {
            var binaryFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg");
            GlobalFFOptions.Configure(options => options.BinaryFolder = binaryFolder);

            var ffmpegPath = Path.Combine(binaryFolder, "ffmpeg.exe");
            var ffprobePath = Path.Combine(binaryFolder, "ffprobe.exe");

            if (!File.Exists(ffmpegPath) || !File.Exists(ffprobePath))
            {
                Console.WriteLine("Downloading FFmpeg binaries...");
                Directory.CreateDirectory(binaryFolder);
                FFMpegDownloader.DownloadBinaries().Wait();
                Console.WriteLine("FFmpeg binaries downloaded.");
            }
        }
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

    }
}
