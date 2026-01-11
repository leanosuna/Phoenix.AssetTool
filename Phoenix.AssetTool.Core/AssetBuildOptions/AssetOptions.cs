using Phoenix.AssetTool.Core.Model;
using Phoenix.AssetTool.Core.Shader;
using Phoenix.AssetTool.Core.Texture;
using Silk.NET.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core.AssetBuildOptions
{
    public static class AssetOptions
    {
        const string DefaultPath = "asset-load-options.json";
        private static string _absolutePath = "";
        private static AssetLoadOptions _alo = default!;
        public static void Init(string manifestRootDirectory)
        {
            var optionsPath = Path.Combine(manifestRootDirectory, DefaultPath).Replace('\\', '/');
            _absolutePath = optionsPath;
            if (!File.Exists(optionsPath))
            {
                _alo = new AssetLoadOptions();
                JsonIOTools.Save(optionsPath, _alo);
            }
            else
            {
                JsonIOTools.Load(optionsPath, out _alo);
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

        public static void Set<T>(string path, T options)
        {
            switch(options)
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

            }
            Save();
        }

        public static void Save()
        {
            JsonIOTools.Save(_absolutePath, _alo);
        }

    }
}
