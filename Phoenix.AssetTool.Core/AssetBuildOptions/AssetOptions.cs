using Phoenix.AssetTool.Core.Model;
using Phoenix.AssetTool.Core.Shader;
using Phoenix.AssetTool.Core.Texture;
using System.IO;

namespace Phoenix.AssetTool.Core.AssetBuildOptions
{
    public static class AssetOptions
    {
        const string DefaultName = "asset-load-options.json";
        private static string _absolutePath = "";
        private static AssetLoadOptions _alo = default!;
        public static void Init()
        {
            var optionsPath = Path.Combine(Manifest.BaseDirectory, DefaultName).Replace('\\', '/');
            _absolutePath = optionsPath;
            if (!File.Exists(optionsPath))
            {
                _alo = new AssetLoadOptions();

                JsonIOTools.Save(_absolutePath, _alo);
            }
            else
            {
                try
                {
                    if (JsonIOTools.Load(_absolutePath, out AssetLoadOptions alo))
                    {
                        _alo = alo;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"warning: could not read '{DefaultName}', using defaults. ({ex.Message})");
                    _alo = new AssetLoadOptions();
                    JsonIOTools.Save(_absolutePath, _alo);
                }
            }
        }
                
        public static ModelLoadOptions OfModel(string path)
        {
            if (!_alo.Models.TryGetValue(path, out var options))
                if(!_alo.Models.TryAdd(path, options = new ModelLoadOptions()))
                    throw new Exception($"try add model {path} failed");

            return options;
        }
        public static TextureLoadOptions OfTexture(string path)
        {
            if (!_alo.Textures.TryGetValue(path, out var options))
                if (!_alo.Textures.TryAdd(path, options = new TextureLoadOptions()))
                    throw new Exception($"try add tex {path} failed");

            return options;
        }
        public static ShaderLoadOptions OfShader(string path)
        {
            if (!_alo.Shaders.TryGetValue(path, out var options))
                if(!_alo.Shaders.TryAdd(path, options = new ShaderLoadOptions()))
                    throw new Exception($"try add shader {path} failed");

            return options;
        }

        public static bool TryGet(string path, out object? options)
        {
            options = null;
            if (_alo == null)
                return false;

            if (_alo.Models.TryGetValue(path, out var model))
            {
                options = model;
                return true;
            }
            if (_alo.Textures.TryGetValue(path, out var texture))
            {
                options = texture;
                return true;
            }
            if (_alo.Shaders.TryGetValue(path, out var shader))
            {
                options = shader;
                return true;
            }
            return false;
        }

        public static void Set<T>(string path, T options)
        {
            if (_alo == null)
            {
                Console.Error.WriteLine("error: AssetOptions not initialized.");
                return;
            }

            switch (options)
            {
                case ModelLoadOptions model:
                    _alo.Models[path] = model;
                    break;
                case TextureLoadOptions tex:
                    _alo.Textures[path] = tex;
                    break;
                case ShaderLoadOptions sh:
                    _alo.Shaders[path] = sh;
                    break;
                default:
                    Console.Error.WriteLine($"error: unsupported options type '{typeof(T).Name}'.");
                    return;
            }
            Save();
        }

        public static void Save()
        {
            JsonIOTools.Save(_absolutePath, _alo);
            //File.WriteAllText(_absolutePath, JsonConvert.SerializeObject(_alo, Formatting.Indented));
        }

    }
}
