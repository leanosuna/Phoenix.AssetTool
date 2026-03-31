using ImGuiNET;
using NativeFileDialogNET;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.AssetBuildOptions;
using Phoenix.AssetTool.Core.Model;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.IO;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;

namespace Phoenix.AssetTool.Gui
{
    public class AssetToolGui
    {
        static IWindow Window = default!;
        static ImGuiController _controller = default!;
        public static GL GL = default!;
        static InputManager InputManager = default!;
        private static Dictionary<int, ImFontPtr> _fonts = new Dictionary<int, ImFontPtr>();

        static int WindowWidth => Window.Size.X;
        static int WindowHeight => Window.Size.Y;
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

            Log.Enabled = true;
            Log.ClearLog();
            Log.Info("Asset tool GUI");

            InputManager = new InputManager(Window);
            var inputContext = InputManager.GetInputContext();
            _controller = new ImGuiController(GL, Window, inputContext);

            LoadDefaultFont();

            Manifest.RegisterNotifyAction(() => { AssetOptions.Init(); });
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
        //static (bool selected, string dir, string path) res = (false, "", "");
        static (bool selected, bool existed, string path) res = (false, false, "");
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
            
            if(Manifest.Loaded)
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

            if (!Manifest.Loaded)
            {
                if (ImGui.Button("Create Manifest file in a folder... (C)") ||
                    InputManager.KeyDownOnce(Key.C))
                {
                    res = Manifest.PickFolderToCreate();

                    if(res.selected)
                    {
                        if(res.existed)
                            ImGui.OpenPopup("replace-manifest");
                        
                    }
                }
                ImGui.Text("or");
                if (ImGui.Button("Select Manifest file... (S)") ||
                    InputManager.KeyDownOnce(Key.S))
                {
                    Manifest.FilePicker();
                }

                if (ImGui.BeginPopup("replace-manifest", ImGuiWindowFlags.NoMove))
                {
                    ImGui.TextColored(new Vector4(0.5f,0,0,1),"Asset manifest found in this directory.");
                    if (ImGui.Button("Open existing manifest (E)") || InputManager.KeyDownOnce(Key.E)) //TODO: add colors
                        Manifest.Load(res.path);
                    
                    if (ImGui.Button("Clear existing manifest (R)") || InputManager.KeyDownOnce(Key.R))
                        Manifest.Clear(res.path);
                    
                    ImGui.EndPopup();
                }
                
            }
            else
            {

                AssetBrowserGui.DrawDirFileTree((float)deltaTime);

                ImGui.SetNextWindowPos(new Vector2(WindowWidth / 2, 0));
                ImGui.SetNextWindowSize(new Vector2(WindowWidth / 2, WindowHeight));
                ImGui.Begin("asset options ui", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize);

                AssetOptionsGui.Draw(AssetBrowserGui.SelectedFileOptions);

            }




            _controller.Render();
        }

        public static bool OpenAssetFilePicker(out string[] filesOut)
        {
            using var dlg = new NativeFileDialog()
            .SelectFile()
            .AddFilter("Asset files", "*.*")
            .AllowMultiple();
            DialogResult result = dlg.Open(out string[]? files, defaultPath: Environment.CurrentDirectory);
            if (result == DialogResult.Okay && files != null && files.Length > 0)
            {
                filesOut = files;
                return true;
            }
            filesOut = [];
            return false;

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