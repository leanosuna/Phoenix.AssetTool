using ImGuiNET;
using NativeFileDialogNET;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.Model;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;

namespace Phoenix.AssetTool.Gui
{
    public class AssetToolGui
    {
        static IWindow Window;
        static ImGuiController _controller;
        public static GL GL;
        static InputManager InputManager;
        private static Dictionary<int, ImFontPtr> _fonts = new Dictionary<int, ImFontPtr>();

        static int WindowWidth => Window.Size.X;
        static int WindowHeight => Window.Size.Y;

        static string DefaultManifestName = "asset-manifest.json";
        static AssetManifest assetManifest;
        static string assetManifestPath;
        static bool manifestLoaded = false;
        public static void Main()
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(1280, 720);
            options.Title = "Phoenix Asset Tool";
            options.VSync = true;
            //options.WindowState = WindowState.Maximized;
            var glApi = new APIVersion(4, 1);
            options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, glApi);

            Window = Silk.NET.Windowing.Window.Create(options);
            Window.Load += Load;
            Window.Update += Update;
            Window.Render += Render;
            Window.FramebufferResize += FramebufferResize;
            Window.Closing += OnClose;

            Window.Run();
        }
        public static void Start()
        {

        }
        private static void Load()
        {
            GL = GL.GetApi(Window);
            Window.WindowState = WindowState.Maximized;
            Window.Center();
            //var max = Window.Monitor.Bounds.Max;
            //Window.Position = Vector2D<int>.Zero;
            //Window.Size = max;

            InputManager = new InputManager(Window);
            var inputContext = InputManager.GetInputContext();
            _controller = new ImGuiController(GL, Window, inputContext);

            LoadDefaultFont();

            //manifestLoaded = JsonIOTools.Load(DefaultManifestName, out assetManifest);
            //if (manifestLoaded)
            //{
            //    assetManifestPath = DefaultManifestName;
            //    AssetBrowserGui.SetManifest(assetManifest, assetManifestPath);

            //}
        }
        static int mouseWheelVal = 0 ;
        static bool showDemo = false;
        private static void Update(double deltaTime)
        {
            InputManager.Update();
            if (InputManager.KeyDownOnce(Key.Escape))
                Window.Close();
            if (InputManager.KeyDownOnce(Key.F12))
                showDemo = !showDemo;

            var diff = InputManager.MouseWheelValue - mouseWheelVal;
            mouseWheelVal = InputManager.MouseWheelValue;

            if(InputManager.KeyDown(Key.ControlLeft))
            {
                if (diff > 0)
                {
                    currentFontSize += 5;
                    if (currentFontSize > 100)
                        currentFontSize = 100;
                }
                else if (diff < 0)
                {
                    currentFontSize -= 5;
                    if (currentFontSize < 10)
                        currentFontSize = 10;
                }
            }
        }
        static (bool selected, string dir, string path) res = (false, "", "");
        static int currentFontSize = 20;
        private static void Render(double deltaTime)
        {
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            _controller.Update((float)deltaTime);
            SetFontSize(currentFontSize);
            //ImGui.Text("hello imgui test");
            //ImGui.ShowDemoWindow();

            if (showDemo)
                ImGui.ShowDemoWindow();

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(WindowWidth / 2, WindowHeight));
            ImGui.Begin("main tool ui", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize);
            
            if(manifestLoaded)
            {
                ImGui.Text("Files: "); SL();
                ImGui.ColorButton("", FileTools.ColorWhite,ImGuiColorEditFlags.NoInputs| ImGuiColorEditFlags.NoTooltip); SL();
                ImGui.Text("untracked"); SL();

                ImGui.ColorButton("", FileTools.ColorYellow, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoTooltip); SL();
                ImGui.Text("added"); SL();

                ImGui.ColorButton("", FileTools.ColorGreen, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoTooltip); SL();
                ImGui.Text("built"); 
            }
            ImGui.NewLine();

            if (!manifestLoaded)
            {
                if (ImGui.Button("Create Manifest file in a folder..."))
                {
                    res = OpenManifestFolderPicker();

                    if(res.selected)
                    {
                        if (File.Exists(res.path))
                            ImGui.OpenPopup("replace-manifest");
                        else
                        {
                            var am = new AssetManifest { BaseDirectory = res.dir};
                            JsonIOTools.Save(res.path, am);
                            assetManifest = am;
                            manifestLoaded = true;
                            assetManifestPath = res.dir;
                            AssetBrowserGui.SetManifest(assetManifest, assetManifestPath);
                        }

                    }
                }

                if (ImGui.BeginPopup("replace-manifest", ImGuiWindowFlags.NoMove))
                {
                    ImGui.Text("Asset manifest found in this directory.");
                    if (ImGui.Button("Open existing manifest"))
                    {
                        if(JsonIOTools.Load(res.dir, out AssetManifest am))
                        {
                            assetManifest = am;
                            manifestLoaded = true;
                            assetManifestPath = res.dir;
                            AssetBrowserGui.SetManifest(assetManifest, assetManifestPath);
                        }
                        else
                        {
                            Console.WriteLine("Error loading manifest");
                        }

                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Replace existing manifest"))
                    {
                        File.Delete(res.path);

                        var am = new AssetManifest();
                        JsonIOTools.Save(res.path, am);
                        assetManifest = am;
                        manifestLoaded = true;
                        assetManifestPath = res.dir;
                        AssetBrowserGui.SetManifest(assetManifest, assetManifestPath);

                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.EndPopup();
                }

                ImGui.Text("or");
                if (ImGui.Button("Select Manifest file..."))
                {
                    OpenManifestFilePicker();
                }
            }
            else
            {
                AssetBrowserGui.DrawDirFileTree((float)deltaTime);
                
                ImGui.SetNextWindowPos(new Vector2(WindowWidth / 2, 0));
                ImGui.SetNextWindowSize(new Vector2(WindowWidth / 2, WindowHeight));
                ImGui.Begin("asset options ui", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize);
                //AssetBrowserGui.DrawOptions();
                var sfo = AssetBrowserGui.SelectedFileOptions;
                AssetOptionsGui.Draw(sfo.asset, sfo.type, sfo.path);

            }


            _controller.Render();
        }

        public static void BuildAsset(AssetEntry asset, bool rebuild = false)
        {
            //AssetBuildPipeline.BuildAsset(assetManifest, asset, rebuild);
        }


        static (bool selected, string dir, string path)OpenManifestFolderPicker()
        {
            using var dlg = new NativeFileDialog()
            .SelectFolder();

            DialogResult result = dlg.Open(out string[]? folders, defaultPath: Environment.CurrentDirectory);
            if (result == DialogResult.Okay && folders != null && folders.Length > 0)
            {
                var dir = folders[0];
                var path = Path.Combine(dir, DefaultManifestName).Replace('\\', '/');

                return (true, dir, path);

            }
            return (false, "", "");
        }




        static void OpenManifestFilePicker()
        {
            using var dlg = new NativeFileDialog()
            .SelectFile()
            .AddFilter("manifest files", "json");

            DialogResult result = dlg.Open(out string[]? files, defaultPath: Environment.CurrentDirectory);
            if (result == DialogResult.Okay && files != null && files.Length > 0)
            {
                var path = files[0];
                manifestLoaded = JsonIOTools.Load(path, out assetManifest);
                if (manifestLoaded)
                {
                    assetManifestPath = path.Replace('\\', '/');
                    AssetBrowserGui.SetManifest(assetManifest, assetManifestPath);
                }
            }
            else
            {
            }

        }
        internal static void OpenAnimationFilePicker(ModelLoadOptions options)
        {
            using var dlg = new NativeFileDialog()
            .SelectFile()
            .AllowMultiple()
            .AddFilter("animation files", "fbx");
            
            DialogResult result = dlg.Open(out string[]? files, defaultPath: Environment.CurrentDirectory);
            if (result == DialogResult.Okay && files != null && files.Length > 0)
            {
                options.AnimationFiles = files.ToList();
            }
            else
            {
            }

        }
        public static void ShowHelpTooltip(string desc, bool sameLine = true)
        {
            if (sameLine)
                SL();
            ImGui.TextDisabled("(?)");

            if (ImGui.BeginItemTooltip())
            {
                ImGui.Text(desc);
                ImGui.EndTooltip();
            }
        }
        public static void SL()
        {
            ImGui.SameLine();
        }

        private static void FramebufferResize(Vector2D<int> size)
        {
            GL.Viewport(size);
        }
        private static void OnClose()
        {
        }
        public static void LoadDefaultFont()
        {
            List<int> sizes = new List<int>();
            for (int i = 10; i <= 100; i += 5)
                sizes.Add(i);

            LoadFontTTF(FileTools.ExtractPath("CascadiaMono.ttf", ""), sizes.ToArray());
            //LoadFontTTF("CascadiaMono.ttf", sizes.ToArray());
        }
        
        public unsafe static void LoadFontTTF(string path, int[] sizes)
        {
            if (sizes.Length == 0)
                throw new Exception("must contain at least one font size");

            var io = ImGui.GetIO();

            _fonts.Clear();

            foreach (var size in sizes)
                _fonts[size] = io.Fonts.AddFontFromFileTTF(path, size);


            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);

            uint fontTex;
            GL.GenTextures(1, out fontTex);
            GL.BindTexture(TextureTarget.Texture2D, fontTex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                          (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

            io.Fonts.SetTexID((nint)fontTex);

            io.Fonts.ClearTexData();

            var first = _fonts.First();
            ImGui.PushFont(first.Value);

            SetFontSize(first.Key);
        }
        public static void SetFontSize(int size)
        {

            if (!_fonts.TryGetValue(size, out var font))
                throw new Exception($"font size {size} not found");

            ImGui.PushFont(font);
        }

        
    }
}