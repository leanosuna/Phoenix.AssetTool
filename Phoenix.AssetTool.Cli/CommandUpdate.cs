using AssetTool.Cli;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.AssetBuildOptions;
using Phoenix.AssetTool.Core.Model;
using Phoenix.AssetTool.Core.Texture;
using System;
using System.CommandLine;
using System.IO;

namespace Phoenix.AssetTool.Cli
{
    internal static class CommandUpdate
    {
        public static Command Setup()
        {
            Argument<string[]> files = new("files")
            {
                Description = "One or more tracked file paths to update in the manifest",
                Arity = ArgumentArity.OneOrMore
            };

            Option<bool?> extractTextures = new("-extract-textures")
            {
                Description = "Extract embedded textures when building (Model)"
            };
            Option<string> assimpFlags = new("-flags")
            {
                Description = "Comma separated Assimp post-process flags (Model). Use '+Name'/'-Name' to toggle relative to the defaults, 'default' to restore the defaults, 'none' for no flags"
            };
            Option<bool?> animated = new("-animated")
            {
                Description = "Treat the model as animated (Model)"
            };
            Option<bool?> preTransform = new("-pre-transform")
            {
                Description = "Pre-transform vertices with the model matrix (Model)"
            };
            Option<float?> scale = new("-scale")
            {
                Description = "Scale factor applied when pre-transform is enabled (Model)"
            };
            Option<string[]> animations = new("-animations")
            {
                Description = "One or more animation files (.fbx), absolute or relative to the content directory (Model)",
                AllowMultipleArgumentsPerToken = true
            };

            Option<bool?> mipmaps = new("-mipmaps")
            {
                Description = "Generate mipmaps (Texture)"
            };
            Option<string> format = new("-format")
            {
                Description = "Compression format: RGBA, BC1, BC3, BC5 (Texture)"
            };
            Option<string> wrapS = new("-wrap-s")
            {
                Description = "Horizontal wrap: Repeat, MirroredRepeat, ClampToEdge, ClampToBorder (Texture)"
            };
            Option<string> wrapT = new("-wrap-t")
            {
                Description = "Vertical wrap: Repeat, MirroredRepeat, ClampToEdge, ClampToBorder (Texture)"
            };
            Option<string> minFilter = new("-min")
            {
                Description = "Minification filter: Nearest, Linear, NearestMipmapNearest, LinearMipmapNearest, NearestMipmapLinear, LinearMipmapLinear (Texture)"
            };
            Option<string> magFilter = new("-mag")
            {
                Description = "Magnification filter: Nearest, Linear (Texture)"
            };
            Option<float?> anisotropy = new("-anisotropy")
            {
                Description = "Anisotropic filtering level, 0 disables (Texture)"
            };

            Command command = new("upd", "Update load options of tracked assets, keeping any option that is not specified")
            {
                files,
                extractTextures,
                assimpFlags,
                animated,
                preTransform,
                scale,
                animations,
                mipmaps,
                format,
                wrapS,
                wrapT,
                minFilter,
                magFilter,
                anisotropy
            };

            command.SetAction(res =>
            {
                if (!AssetToolCli.TryLoadManifest(res))
                    return;

                var filePaths = res.GetValue(files);
                if (filePaths == null) return;

                var modelProvided = !OptionParsers.HasModelOptions(
                    res.GetValue(extractTextures), res.GetValue(assimpFlags), res.GetValue(animated),
                    res.GetValue(preTransform), res.GetValue(scale), res.GetValue(animations));
                var textureProvided = !OptionParsers.HasTextureOptions(
                    res.GetValue(mipmaps), res.GetValue(format), res.GetValue(wrapS), res.GetValue(wrapT),
                    res.GetValue(minFilter), res.GetValue(magFilter), res.GetValue(anisotropy));

                if (!modelProvided && !textureProvided)
                {
                    Console.Error.WriteLine("error: no options provided. Pass at least one option flag.");
                    AssetToolCli.ExitCode = -1;
                    return;
                }

                var errors = new List<string>();

                foreach (var filePath in filePaths)
                {
                    var absolutePath = Path.GetFullPath(filePath);
                    var relative = Path.GetRelativePath(Manifest.BaseDirectory, absolutePath)
                        .Replace('\\', '/');

                    if (relative.StartsWith(".."))
                    {
                        Console.Error.WriteLine($"error: file '{filePath}' is outside the Content directory.");
                        AssetToolCli.ExitCode = -1;
                        continue;
                    }

                    if (!File.Exists(absolutePath))
                    {
                        Console.Error.WriteLine($"error: '{filePath}' not found.");
                        AssetToolCli.ExitCode = -1;
                        continue;
                    }

                    var asset = Manifest.Assets.FirstOrDefault(a =>
                        a.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase));

                    if (asset == null)
                    {
                        Console.Error.WriteLine($"error: '{relative}' is not tracked. Use 'add' first.");
                        AssetToolCli.ExitCode = -1;
                        continue;
                    }

                    UpdateAsset(asset, relative, modelProvided, textureProvided, res,
                        extractTextures, assimpFlags, animated, preTransform, scale, animations,
                        mipmaps, format, wrapS, wrapT, minFilter, magFilter, anisotropy, errors);
                }

                foreach (var error in errors)
                {
                    Console.Error.WriteLine(error);
                    AssetToolCli.ExitCode = -1;
                }

                AssetOptions.Save();
                Manifest.Save();
            });

            return command;
        }

        private static void UpdateAsset(
            AssetEntry asset,
            string relative,
            bool modelProvided,
            bool textureProvided,
            ParseResult res,
            Option<bool?> extractTextures,
            Option<string> flags,
            Option<bool?> animated,
            Option<bool?> preTransform,
            Option<float?> scale,
            Option<string[]> animations,
            Option<bool?> mipmaps,
            Option<string> format,
            Option<string> wrapS,
            Option<string> wrapT,
            Option<string> min,
            Option<string> mag,
            Option<float?> anisotropy,
            List<string> errors)
        {
            switch (asset.Type)
            {
                case AssetType.Model:
                    var modelOptions = AssetOptions.OfModel(relative);
                    if (modelProvided)
                    {
                        OptionParsers.ApplyModelOptions(
                            modelOptions,
                            res.GetValue(extractTextures), res.GetValue(flags), res.GetValue(animated),
                            res.GetValue(preTransform), res.GetValue(scale), res.GetValue(animations),
                            Manifest.BaseDirectory, errors);
                        AssetOptions.Set(relative, modelOptions);
                    }
                    break;

                case AssetType.Texture:
                    var textureOptions = AssetOptions.OfTexture(relative);
                    if (textureProvided)
                    {
                        OptionParsers.ApplyTextureOptions(
                            textureOptions,
                            res.GetValue(mipmaps), res.GetValue(format), res.GetValue(wrapS), res.GetValue(wrapT),
                            res.GetValue(min), res.GetValue(mag), res.GetValue(anisotropy), errors);
                        AssetOptions.Set(relative, textureOptions);
                    }
                    break;

                case AssetType.Shader:
                case AssetType.ExtTexture:
                    Console.Error.WriteLine($"error: asset type '{asset.Type}' has no load options to update.");
                    AssetToolCli.ExitCode = -1;
                    return;
            }

            Console.WriteLine($"Updated '{relative}'");
        }
    }
}
