using Phoenix.AssetTool.Cli;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.AssetBuildOptions;
using Phoenix.AssetTool.Core.Shader;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.CommandLine;
using System.Text;

namespace AssetTool.Cli
{
    class AssetToolCli
    {
        public const string DefaultPath = "asset-manifest.json";
        public static bool KeepAlive;
        public static int ExitCode = 0;
        private static Argument<FileInfo> _argumentManifest = default!;
        
        static int Main(string[] args)
        {
            Console.WriteLine("[ Phoenix Asset Tool ]");

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
            rootCommand.Subcommands.Add(CommandAdd.Setup());
            rootCommand.Subcommands.Add(CommandRemove.Setup());
            rootCommand.Subcommands.Add(CommandClean.Setup());

            rootCommand.SetAction(parseResult =>
            {
                if (TryLoadManifest(parseResult, silent: false))
                {
                    Console.Error.WriteLine("Command not found.");
                    ExitCode = -1;
                }
                
                //Console.WriteLine(res);
            });

            ParseResult parseResult = rootCommand.Parse(args);
            
            InitGL();
            
            parseResult.Invoke();

            if(KeepAlive)
                Console.ReadLine();

            return ExitCode;
        }
        static bool pendingLoop = false;
        public static void StartBuildPendingLoop()
        {
            pendingLoop = true;
            
            Task.Run(() => {
                int loading = 0;
                while (pendingLoop)
                {
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
                ExitCode = -1;
                return false;
            }

            if (manFileInfo == null)
            {
                if (!silent)
                    Console.Error.WriteLine("manifest argument empty.\n Usage: pat [manifest path] [command]");
                ExitCode = -1;
                return false;
            }

            absolutePath = manFileInfo.FullName;

            if (string.IsNullOrEmpty(absolutePath))
            {
                if (!silent)
                    Console.WriteLine("manifest argument empty.\n Usage: pat [manifest path] [command]");
                ExitCode = -1;
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
                ExitCode = -1;
                return false;
            }
            var fileName = Path.GetFileName(absolutePath);

            if (!Manifest.Load(absolutePath))
            {
                if (!silent)
                    Console.Error.WriteLine($"manifest failed to load.");
                ExitCode = -1;
                return false;
            }
            if (!silent)
                Console.WriteLine($"Loaded {fileName}");

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
            
            return Manifest.CreateAbsolute(absolutePath);
        }

        static void InitGL()
        {
            try
            {
                var options = WindowOptions.Default;
                options.Size = new Vector2D<int>(1, 1);
                options.IsVisible = false;
                options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(4, 1));

                var window = Window.Create(options);
                window.Initialize();
                var gl = GL.GetApi(window);
                GLCompiler.Init(gl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not initialize OpenGL context: {ex.Message}");
                ExitCode = -1;
            }
        }
    }
}
