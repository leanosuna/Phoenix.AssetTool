using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.IO;

namespace Phoenix.AssetTool.Core.Shader
{
    public class AssetToolConfig
    {
        public GlConfig Gl { get; set; } = new();
    }

    public class GlConfig
    {
        public string Version { get; set; } = "default";
    }

    public static class GlContext
    {
        public const string ConfigFileName = "asset-tool-config.json";
        public const string DefaultMode = "default";

        private static readonly object _lock = new();
        private static IWindow? _window;
        private static bool _initialized;
        private static bool _failed;

        public static string ConfigPath(string dir) =>
            Path.Combine(dir, ConfigFileName).Replace('\\', '/');

        public static string LoadConfig(string dir, bool createIfMissing = true)
        {
            var path = ConfigPath(dir);
            if (!File.Exists(path))
            {
                if (!createIfMissing)
                    return DefaultMode;
                JsonIOTools.Save(path, new AssetToolConfig());
            }

            if (JsonIOTools.Load(path, out AssetToolConfig config))
                return config.Gl?.Version ?? DefaultMode;

            return DefaultMode;
        }

        public static bool InitHiddenWindow(string dir)
        {
            lock (_lock)
            {
                if (_initialized)
                    return true;
                if (_failed)
                    return false;

                var mode = LoadConfig(dir);
                var window = CreateWindow(mode);
                if (window == null)
                {
                    _failed = true;
                    return false;
                }

                _window = window;
                var gl = GL.GetApi(window);
                GLCompiler.Init(gl);
                LogResolvedVersion(gl);
                _initialized = true;
                return true;
            }
        }

        public static APIVersion ResolveRequestedApi(string dir)
        {
            var mode = LoadConfig(dir, createIfMissing: false);
            var max = GetMaxSupported();
            var target = ResolveTarget(mode, max, out _) ?? ResolveTarget(DefaultMode, max, out _) ?? (4, 1);
            return new APIVersion(target.major, target.minor);
        }

        private static IWindow? CreateWindow(string mode)
        {
            var max = GetMaxSupported();

            var target = ResolveTarget(mode, max, out var warning);
            if (target == null)
            {
                if (warning != null)
                    Console.Error.WriteLine(warning);
                target = ResolveTarget(DefaultMode, max, out _);
            }

            return TryCreateWindow(target ?? (4, 1), out var window) ? window : null;
        }

        private static (int major, int minor) GetMaxSupported()
        {
            // GLFW on macOS caps OpenGL at 4.1 core, and any version request
            // below 3.2 fails there - so just use 4.1 without probing.
            if (OperatingSystem.IsMacOS())
                return (4, 1);

            // Requesting a version below 3.2 makes GLFW create the highest
            // context the driver supports (it never fails), which is the only
            // safe way to discover the real maximum - a failed version request
            // would abort the process via a GLFW assertion.
            if (TryCreateWindow((3, 0), out var probe))
            {
                var max = ReadVersion(probe);
                probe.Dispose();
                if (max != null)
                    return max.Value;
            }

            return (4, 1);
        }

        private static bool TryCreateWindow((int major, int minor) version, out IWindow window)
        {
            try
            {
                var options = WindowOptions.Default;
                options.Size = new Vector2D<int>(1, 1);
                options.IsVisible = false;
                options.API = new GraphicsAPI(
                    ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default,
                    new APIVersion(version.major, version.minor));

                var created = Window.Create(options);
                created.Initialize();
                window = created;
                return true;
            }
            catch
            {
                window = default!;
                return false;
            }
        }

        private static (int major, int minor)? ReadVersion(IWindow window)
        {
            try
            {
                var gl = GL.GetApi(window);
                var version = gl.GetStringS(StringName.Version); // e.g. "4.6.0 NVIDIA ..."
                var parts = version.Split('.');
                if (parts.Length >= 2 &&
                    int.TryParse(parts[0], out var major) &&
                    int.TryParse(parts[1], out var minor))
                    return (major, minor);
            }
            catch
            {
                // ignore, caller falls back to the default target
            }
            return null;
        }

        private static (int major, int minor)? ResolveTarget(
            string mode,
            (int major, int minor) max,
            out string? warning)
        {
            warning = null;

            if (mode.Equals("auto", StringComparison.OrdinalIgnoreCase))
                return max;

            if (mode.Equals("default", StringComparison.OrdinalIgnoreCase))
                return max.major > 4 || (max.major == 4 && max.minor >= 1)
                    ? (4, 1)
                    : max;

            if (TryParseVersion(mode, out var version))
            {
                var supported = version.major < max.major ||
                                (version.major == max.major && version.minor <= max.minor);
                if (supported)
                    return version;

                warning = $"warning: OpenGL {mode} is not supported (max is {max.major}.{max.minor}), falling back to default.";
                return null;
            }

            warning = $"warning: invalid GL version '{mode}' in {ConfigFileName}, using default.";
            return null;
        }

        private static bool TryParseVersion(string version, out (int major, int minor) parsed)
        {
            var parts = version.Split('.');
            if (parts.Length >= 2 &&
                int.TryParse(parts[0], out var major) &&
                int.TryParse(parts[1], out var minor))
            {
                parsed = (major, minor);
                return true;
            }

            parsed = default;
            return false;
        }

        private static void LogResolvedVersion(GL gl)
        {
            try
            {
                var version = gl.GetStringS(StringName.Version);
                Console.WriteLine($"OpenGL {version}");
            }
            catch
            {
                // logging is best-effort
            }
        }
    }
}
