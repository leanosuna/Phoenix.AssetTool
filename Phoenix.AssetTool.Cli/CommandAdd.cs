using AssetTool.Cli;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.AssetBuildOptions;
using Phoenix.AssetTool.Core.Model;
using Phoenix.AssetTool.Core.Texture;
using System;
using System.CommandLine;
using System.IO;
using File = System.IO.File;

namespace Phoenix.AssetTool.Cli
{
    internal static class CommandAdd
    {
        public static Command Setup()
        {
            Argument<string[]> files = new("files")
            {
                Description = "One or more file paths or directories to add to the manifest",
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

            Command command = new("add", "Add files to the manifest")
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

                var errors = new List<string>();
                var modelOptions = OptionParsers.BuildModelOptions(
                    res.GetValue(extractTextures),
                    res.GetValue(assimpFlags),
                    res.GetValue(animated),
                    res.GetValue(preTransform),
                    res.GetValue(scale),
                    res.GetValue(animations),
                    Manifest.BaseDirectory,
                    errors);
                var textureOptions = OptionParsers.BuildTextureOptions(
                    res.GetValue(mipmaps),
                    res.GetValue(format),
                    res.GetValue(wrapS),
                    res.GetValue(wrapT),
                    res.GetValue(minFilter),
                    res.GetValue(magFilter),
                    res.GetValue(anisotropy),
                    errors);

                AssetToolCli.PrintMessages(errors);

                foreach (var filePath in filePaths)
                {
                    var root =
                        filePath.Equals(".", StringComparison.InvariantCulture) ||
                        filePath.Equals(Manifest.BaseDirectory, StringComparison.InvariantCultureIgnoreCase);

                    var resolvedPath = root ? Manifest.BaseDirectory : filePath;
                    var absolutePath = Path.GetFullPath(resolvedPath);

                    if (Directory.Exists(absolutePath))
                    {
                        FileTools.AddDirectory(absolutePath, silent: false, modelOptions, textureOptions);
                        if (root)
                            break;
                    }
                    else if (File.Exists(absolutePath))
                    {
                        var relative = FileTools.ToRelative(absolutePath);
                        if (relative == null)
                        {
                            Console.Error.WriteLine($"error: file '{absolutePath}' is outside the Content directory.");
                            AssetToolCli.ExitCode = -1;
                            continue;
                        }
                        AssetToolCli.PrintMessages(FileTools.AddAssetFile(relative, modelOptions, textureOptions));
                    }
                    else
                    {
                        Console.Error.WriteLine($"error: '{filePath}' not found.");
                        AssetToolCli.ExitCode = -1;
                    }
                }

                AssetOptions.Save();
                Manifest.Save();
            });

            return command;
        }
    }
}
