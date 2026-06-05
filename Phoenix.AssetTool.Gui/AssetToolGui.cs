using ImGuiNET;
using NativeFileDialogNET;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.AssetBuildOptions;
using Phoenix.AssetTool.Core.Texture;
using Silk.NET.Core;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        private static void Load()
        {
            GL = GL.GetApi(Window);
            Window.WindowState = WindowState.Maximized;
            Window.Center();

            using Image<Rgba32> image = Image.Load<Rgba32>(FileTools.ExtractPath("tool.png",""));
            (Vector2 size, byte[] buffer) data = TextureBinaryWriter.ImageToBytes(image);

            RawImage img = new RawImage((int)data.size.X, (int)data.size.Y, (Memory<byte>)data.buffer);

            Window.SetWindowIcon(ref img);

            Log.Enabled = true;
            Log.ClearLog();
            Log.Info("Asset tool GUI");

            InputManager = new InputManager(Window);
            var inputContext = InputManager.GetInputContext();
            _controller = new ImGuiController(GL, Window, inputContext);

            LoadDefaultFont();
            
            if(Manifest.Loaded)
            {
                DarkTheme = Manifest.DarkTheme;
                AssetOptions.Init();
            }
            
            UpdateTheme(false);
            Manifest.RegisterNotifyAction(() => {

                DarkTheme = Manifest.DarkTheme;
                UpdateTheme(false);
                AssetOptions.Init();

            });
            BrowserSize = new Vector2(WindowWidth / 2, WindowHeight);

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
                if (diff < 0)
                {
                    FontSizeUp();
                }
                else if (diff > 0)
                {
                    FontSizeDown();
                   
                }
            }
        }
        static (bool selected, bool existed, string path) res = (false, false, "");
        static int currentFontSize = 17;
        public static Vector2 BrowserSize;
        public static bool DarkTheme = true;

        public static void FontSizeUp()
        {
            currentFontSize += 1;
            if (currentFontSize > 50)
                currentFontSize = 50;
        }
        public static void FontSizeDown()
        {
            currentFontSize -= 1;
            if (currentFontSize < 10)
                currentFontSize = 10;
        }
        public static void ToggleTheme()
        {
            DarkTheme = !DarkTheme;
            UpdateTheme();
        }
        private static void UpdateTheme(bool save = true)
        {
            if (DarkTheme)
                SetMocha();
            else
                SetLatte();
            if (save)
            {
                Manifest.DarkTheme = DarkTheme;
                Manifest.Save();
            }
            if(Manifest.Loaded)
                AssetBrowserGui.UpdateDirectory();
        }
        private static void Render(double deltaTime)
        {
            if(DarkTheme)
                GL.ClearColor(0, 0, 0, 1);
            else
                GL.ClearColor(.9f, .9f, .9f, 1);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            _controller.Update((float)deltaTime);
            SetFontSize(currentFontSize);
            
            

            if (showDemo)
                ImGui.ShowDemoWindow();

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(BrowserSize);
            ImGui.Begin("main tool ui", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);
            
            var drawOptions = false;
            if (!Manifest.Loaded)
            {
                if (ImGui.Button("Create Manifest file in a folder... (C)") ||
                    InputManager.KeyDownOnce(Key.C))
                {
                    res = Manifest.PickFolderToCreate();

                    if (res.selected)
                    {
                        if (res.existed)
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
                    ImGui.TextColored(new Vector4(0.5f, 0, 0, 1), "Asset manifest found in this directory.");
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

                drawOptions = true;
                
            }
            var size = ImGui.GetWindowSize();

            BrowserSize.X = size.X;
            ImGui.End();
            if(drawOptions)
            {
                ImGui.SetNextWindowPos(new Vector2(BrowserSize.X, 0));
                ImGui.SetNextWindowSize(new Vector2(WindowWidth - BrowserSize.X, WindowHeight));
                ImGui.Begin("asset options ui", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize);

                if(AssetBrowserGui.ShowOptions)
                    AssetOptionsGui.Draw(AssetBrowserGui.SelectedFileOptions);
                else
                {
                    var item = AssetBuildGui.Selected;
                    ImGui.Text($"{Path.GetFileName(item.Asset.RelativePath)} FAILED:");
                    var e = item.Error;

                    var sz = ImGui.CalcTextSize(e) + new Vector2(30,10);
                    
                    
                    ImGui.InputTextMultiline("##error",ref e, 1000, sz, ImGuiInputTextFlags.ReadOnly);
                    
                }
            }

            _controller.Render();
        }
        static Vector4 Hex(string hex, float alpha = 1.0f)
        {
            hex = hex.TrimStart('#');

            float r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
            float g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
            float b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;

            return new Vector4(r, g, b, alpha);
        }
        static void SetLatte()
        {
            ImGuiStylePtr style = ImGui.GetStyle();
            var colors = style.Colors;

            style.WindowRounding = 8f;
            style.ChildRounding = 8f;
            style.FrameRounding = 6f;
            style.PopupRounding = 6f;
            style.ScrollbarRounding = 6f;
            style.GrabRounding = 6f;
            style.TabRounding = 6f;

            // Catppuccin Latte
            const string Rosewater = "#dc8a78";
            const string Flamingo = "#dd7878";
            const string Pink = "#ea76cb";
            const string Mauve = "#8839ef";
            const string Red = "#d20f39";
            const string Peach = "#fe640b";
            const string Yellow = "#df8e1d";
            const string Green = "#40a02b";
            const string Teal = "#179299";
            const string Sky = "#04a5e5";
            const string Sapphire = "#209fb5";
            const string Blue = "#1e66f5";
            const string Lavender = "#7287fd";

            //const string Text = "#4c4f69";
            const string Text = "#202020";

            const string Subtext1 = "#5c5f77";
            const string Subtext0 = "#6c6f85";

            const string Overlay2 = "#7c7f93";
            const string Overlay1 = "#8c8fa1";
            const string Overlay0 = "#9ca0b0";

            const string Surface2 = "#acb0be";
            const string Surface1 = "#bcc0cc";
            const string Surface0 = "#ccd0da";

            const string Base = "#eff1f5";
            const string Mantle = "#e6e9ef";
            const string Crust = "#dce0e8";

            SetColor(ref colors, ImGuiCol.Text, Text);
            SetColor(ref colors, ImGuiCol.TextDisabled, Overlay1);

            SetColor(ref colors, ImGuiCol.WindowBg, Base);
            SetColor(ref colors, ImGuiCol.ChildBg, Base);
            SetColor(ref colors, ImGuiCol.PopupBg, Mantle, 0.98f);

            SetColor(ref colors, ImGuiCol.Border, Surface2, 0.6f);
            SetColor(ref colors, ImGuiCol.BorderShadow, Crust, 0.0f);

            SetColor(ref colors, ImGuiCol.FrameBg, Surface0);
            SetColor(ref colors, ImGuiCol.FrameBgHovered, Surface1);
            SetColor(ref colors, ImGuiCol.FrameBgActive, Surface2);

            SetColor(ref colors, ImGuiCol.TitleBg, Mantle);
            SetColor(ref colors, ImGuiCol.TitleBgActive, Surface0);
            SetColor(ref colors, ImGuiCol.TitleBgCollapsed, Crust);

            SetColor(ref colors, ImGuiCol.MenuBarBg, Mantle);

            SetColor(ref colors, ImGuiCol.ScrollbarBg, Crust);
            SetColor(ref colors, ImGuiCol.ScrollbarGrab, Overlay0);
            SetColor(ref colors, ImGuiCol.ScrollbarGrabHovered, Overlay1);
            SetColor(ref colors, ImGuiCol.ScrollbarGrabActive, Overlay2);

            SetColor(ref colors, ImGuiCol.CheckMark, Blue);

            SetColor(ref colors, ImGuiCol.SliderGrab, Sapphire);
            SetColor(ref colors, ImGuiCol.SliderGrabActive, Blue);

            SetColor(ref colors, ImGuiCol.Button, Surface0);
            SetColor(ref colors, ImGuiCol.ButtonHovered, Surface1);
            SetColor(ref colors, ImGuiCol.ButtonActive, Surface2);

            //SetColor(ref colors, ImGuiCol.Header, Surface0);
            //SetColor(ref colors, ImGuiCol.HeaderHovered, Surface1);
            //SetColor(ref colors, ImGuiCol.HeaderActive, Surface2);
            SetColor(ref colors, ImGuiCol.Header, Sapphire);
            SetColor(ref colors, ImGuiCol.HeaderHovered, Surface1);
            SetColor(ref colors, ImGuiCol.HeaderActive, Surface2);

            SetColor(ref colors, ImGuiCol.Separator, Overlay0);
            SetColor(ref colors, ImGuiCol.SeparatorHovered, Overlay1);
            SetColor(ref colors, ImGuiCol.SeparatorActive, Overlay2);

            SetColor(ref colors, ImGuiCol.ResizeGrip, Overlay0, 0.4f);
            SetColor(ref colors, ImGuiCol.ResizeGripHovered, Blue, 0.7f);
            SetColor(ref colors, ImGuiCol.ResizeGripActive, Sapphire);

            SetColor(ref colors, ImGuiCol.Tab, Mantle);
            SetColor(ref colors, ImGuiCol.TabHovered, Surface1);
            SetColor(ref colors, ImGuiCol.TabActive, Surface0);
            SetColor(ref colors, ImGuiCol.TabUnfocused, Crust);
            SetColor(ref colors, ImGuiCol.TabUnfocusedActive, Surface0);

            SetColor(ref colors, ImGuiCol.DockingPreview, Blue, 0.5f);
            SetColor(ref colors, ImGuiCol.DockingEmptyBg, Mantle);

            SetColor(ref colors, ImGuiCol.PlotLines, Blue);
            SetColor(ref colors, ImGuiCol.PlotLinesHovered, Red);

            SetColor(ref colors, ImGuiCol.PlotHistogram, Green);
            SetColor(ref colors, ImGuiCol.PlotHistogramHovered, Teal);

            SetColor(ref colors, ImGuiCol.TextSelectedBg, Blue, 0.25f);
            SetColor(ref colors, ImGuiCol.DragDropTarget, Red);

            SetColor(ref colors, ImGuiCol.NavHighlight, Blue);
            SetColor(ref colors, ImGuiCol.NavWindowingHighlight, Lavender);
            SetColor(ref colors, ImGuiCol.NavWindowingDimBg, Crust, 0.5f);
            SetColor(ref colors, ImGuiCol.ModalWindowDimBg, Crust, 0.5f);
        }
        static void SetMocha()
        {
            ImGuiStylePtr style = ImGui.GetStyle();
            var colors = style.Colors;

            style.WindowRounding = 8f;
            style.ChildRounding = 8f;
            style.FrameRounding = 6f;
            style.PopupRounding = 6f;
            style.ScrollbarRounding = 6f;
            style.GrabRounding = 6f;
            style.TabRounding = 6f;

            // Catppuccin Mocha
            const string Rosewater = "#f5e0dc";
            const string Flamingo = "#f2cdcd";
            const string Pink = "#f5c2e7";
            const string Mauve = "#cba6f7";
            const string Red = "#f38ba8";
            const string Peach = "#fab387";
            const string Yellow = "#f9e2af";
            const string Green = "#a6e3a1";
            const string Teal = "#94e2d5";
            const string Sky = "#89dceb";
            const string Sapphire = "#74c7ec";
            const string Blue = "#89b4fa";
            const string Lavender = "#b4befe";

            const string Text = "#cdd6f4";
            const string Subtext1 = "#bac2de";
            const string Subtext0 = "#a6adc8";

            const string Overlay2 = "#9399b2";
            const string Overlay1 = "#7f849c";
            const string Overlay0 = "#6c7086";

            const string Surface2 = "#585b70";
            const string Surface1 = "#45475a";
            const string Surface0 = "#313244";

            const string Base = "#1e1e2e";
            const string Mantle = "#181825";
            const string Crust = "#11111b";

            SetColor(ref colors, ImGuiCol.Text, Text);
            SetColor(ref colors, ImGuiCol.TextDisabled, Overlay1);

            SetColor(ref colors, ImGuiCol.WindowBg, Base);
            SetColor(ref colors, ImGuiCol.ChildBg, Base);
            SetColor(ref colors, ImGuiCol.PopupBg, Mantle, 0.98f);

            SetColor(ref colors, ImGuiCol.Border, Surface2, 0.5f);
            SetColor(ref colors, ImGuiCol.BorderShadow, Crust, 0.0f);

            SetColor(ref colors, ImGuiCol.FrameBg, Surface0);
            SetColor(ref colors, ImGuiCol.FrameBgHovered, Surface1);
            SetColor(ref colors, ImGuiCol.FrameBgActive, Surface2);

            SetColor(ref colors, ImGuiCol.TitleBg, Mantle);
            SetColor(ref colors, ImGuiCol.TitleBgActive, Surface0);
            SetColor(ref colors, ImGuiCol.TitleBgCollapsed, Crust);

            SetColor(ref colors, ImGuiCol.MenuBarBg, Mantle);

            SetColor(ref colors, ImGuiCol.ScrollbarBg, Crust);
            SetColor(ref colors, ImGuiCol.ScrollbarGrab, Overlay0);
            SetColor(ref colors, ImGuiCol.ScrollbarGrabHovered, Overlay1);
            SetColor(ref colors, ImGuiCol.ScrollbarGrabActive, Overlay2);

            SetColor(ref colors, ImGuiCol.CheckMark, Blue);

            SetColor(ref colors, ImGuiCol.SliderGrab, Sapphire);
            SetColor(ref colors, ImGuiCol.SliderGrabActive, Blue);

            SetColor(ref colors, ImGuiCol.Button, Surface0);
            SetColor(ref colors, ImGuiCol.ButtonHovered, Surface1);
            SetColor(ref colors, ImGuiCol.ButtonActive, Surface2);

            SetColor(ref colors, ImGuiCol.Header, Blue, 0.65f);
            SetColor(ref colors, ImGuiCol.HeaderHovered, Surface1, 0.85f);
            SetColor(ref colors, ImGuiCol.HeaderActive, Surface2, 1.0f);

            SetColor(ref colors, ImGuiCol.Separator, Overlay0);
            SetColor(ref colors, ImGuiCol.SeparatorHovered, Overlay1);
            SetColor(ref colors, ImGuiCol.SeparatorActive, Overlay2);

            SetColor(ref colors, ImGuiCol.ResizeGrip, Overlay0, 0.4f);
            SetColor(ref colors, ImGuiCol.ResizeGripHovered, Blue, 0.7f);
            SetColor(ref colors, ImGuiCol.ResizeGripActive, Sapphire);

            SetColor(ref colors, ImGuiCol.Tab, Mantle);
            SetColor(ref colors, ImGuiCol.TabHovered, Surface1);
            SetColor(ref colors, ImGuiCol.TabActive, Surface0);
            SetColor(ref colors, ImGuiCol.TabUnfocused, Crust);
            SetColor(ref colors, ImGuiCol.TabUnfocusedActive, Surface0);

            SetColor(ref colors, ImGuiCol.DockingPreview, Blue, 0.5f);
            SetColor(ref colors, ImGuiCol.DockingEmptyBg, Mantle);

            SetColor(ref colors, ImGuiCol.PlotLines, Blue);
            SetColor(ref colors, ImGuiCol.PlotLinesHovered, Red);

            SetColor(ref colors, ImGuiCol.PlotHistogram, Green);
            SetColor(ref colors, ImGuiCol.PlotHistogramHovered, Teal);

            SetColor(ref colors, ImGuiCol.TextSelectedBg, Blue, 0.35f);
            SetColor(ref colors, ImGuiCol.DragDropTarget, Red);

            SetColor(ref colors, ImGuiCol.NavHighlight, Blue);
            SetColor(ref colors, ImGuiCol.NavWindowingHighlight, Lavender);
            SetColor(ref colors, ImGuiCol.NavWindowingDimBg, Crust, 0.5f);
            SetColor(ref colors, ImGuiCol.ModalWindowDimBg, Crust, 0.7f);
        }
        static void SetColor(ref RangeAccessor<Vector4> colors, ImGuiCol idx, string hex, float alpha = 1.0f)
        {
            colors[(int)idx] = Hex(hex, alpha);
        }
        public static void SetColor(ref RangeAccessor<Vector4> colors, ImGuiCol idx, float r, float g, float b, float a)
        {
            colors[(int)idx] = new Vector4(r, g, b, a);   
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

        public static bool OpenDirectoryPicker(out string[] filesOut)
        {
            using var dlg = new NativeFileDialog()
            .SelectFolder();
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
            var offset = 50;
            if (BrowserSize.X >= size.X - offset)
                BrowserSize.X = size.X - offset;
            BrowserSize.Y = size.Y;
        }
        private static void OnClose()
        {
        }
        public static void LoadDefaultFont()
        {
            List<int> sizes = new List<int>();
            for (int i = 10; i <= 50; i += 1)
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