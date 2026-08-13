using Phoenix.AssetImport.Texture;
using Phoenix.AssetTool.Core.Model;
using Phoenix.AssetTool.Core.Texture;
using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using File = System.IO.File;

namespace Phoenix.AssetTool.Core.AssetBuildOptions
{
    public static class OptionParsers
    {
        public static ModelLoadOptions? BuildModelOptions(
            bool? extractTextures,
            string? flags,
            bool? animated,
            bool? preTransform,
            float? scale,
            string[]? animations,
            string baseDirectory,
            List<string> errors)
        {
            if (HasModelOptions(extractTextures, flags, animated, preTransform, scale, animations))
                return null;

            var options = new ModelLoadOptions();
            ApplyModelOptions(options, extractTextures, flags, animated, preTransform, scale, animations, baseDirectory, errors);
            return options;
        }

        public static bool HasModelOptions(
            bool? extractTextures,
            string? flags,
            bool? animated,
            bool? preTransform,
            float? scale,
            string[]? animations)
        {
            return extractTextures == null &&
                   flags == null &&
                   animated == null &&
                   preTransform == null &&
                   scale == null &&
                   HasNoAnimations(animations);
        }

        private static bool HasNoAnimations(string[]? animations) =>
            animations == null || animations.Length == 0;

        public static void ApplyModelOptions(
            ModelLoadOptions options,
            bool? extractTextures,
            string? flags,
            bool? animated,
            bool? preTransform,
            float? scale,
            string[]? animations,
            string baseDirectory,
            List<string> errors)
        {
            if (extractTextures is bool et)
                options.ExtractTextures = et;

            if (flags is string flagStr)
                options.AssimpFlags = ParseAssimpFlags(flagStr, errors);

            if (animated is bool an)
                options.IsAnimated = an;

            if (preTransform is bool pt)
                options.PreTransform = pt;

            if (scale is float sc)
                options.Scale = sc;

            if (animations is { Length: > 0 })
                options.AnimationFiles = ToRelativeAnimations(animations, baseDirectory, errors);
        }

        public static TextureLoadOptions? BuildTextureOptions(
            bool? mipmaps,
            string? format,
            string? wrapS,
            string? wrapT,
            string? min,
            string? mag,
            float? anisotropy,
            List<string> errors)
        {
            if (HasTextureOptions(mipmaps, format, wrapS, wrapT, min, mag, anisotropy))
                return null;

            var options = new TextureLoadOptions();
            ApplyTextureOptions(options, mipmaps, format, wrapS, wrapT, min, mag, anisotropy, errors);
            return options;
        }

        public static bool HasTextureOptions(
            bool? mipmaps,
            string? format,
            string? wrapS,
            string? wrapT,
            string? min,
            string? mag,
            float? anisotropy)
        {
            return mipmaps == null &&
                   format == null &&
                   wrapS == null &&
                   wrapT == null &&
                   min == null &&
                   mag == null &&
                   anisotropy == null;
        }

        public static void ApplyTextureOptions(
            TextureLoadOptions options,
            bool? mipmaps,
            string? format,
            string? wrapS,
            string? wrapT,
            string? min,
            string? mag,
            float? anisotropy,
            List<string> errors)
        {
            if (mipmaps is bool m)
            {
                options.GenerateMipMaps = m;
                if (m)
                {
                    options.Min = TextureFilter.LinearMipmapLinear;
                    options.Mag = TextureFilter.Linear;
                }
            }

            if (format is string f)
            {
                if (Enum.TryParse<AssetCompressionFormat>(f, true, out var fmt))
                    options.Format = fmt;
                else
                    errors.Add($"error: invalid compression format '{f}'. Use RGBA, BC1, BC3 or BC5.");
            }

            if (wrapS is string ws)
            {
                if (Enum.TryParse<TextureWrap>(ws, true, out var wrap))
                    options.WrapS = wrap;
                else
                    errors.Add($"error: invalid wrap mode '{ws}'. Use Repeat, MirroredRepeat, ClampToEdge or ClampToBorder.");
            }

            if (wrapT is string wt)
            {
                if (Enum.TryParse<TextureWrap>(wt, true, out var wrap))
                    options.WrapT = wrap;
                else
                    errors.Add($"error: invalid wrap mode '{wt}'. Use Repeat, MirroredRepeat, ClampToEdge or ClampToBorder.");
            }

            if (min is string minStr)
            {
                if (Enum.TryParse<TextureFilter>(minStr, true, out var filter))
                    options.Min = filter;
                else
                    errors.Add($"error: invalid minification filter '{minStr}'.");
            }

            if (mag is string magStr)
            {
                if (Enum.TryParse<TextureFilter>(magStr, true, out var filter))
                    options.Mag = filter;
                else
                    errors.Add($"error: invalid magnification filter '{magStr}'.");
            }

            if (anisotropy is float a)
                options.Anisotropic = a;
        }

        public static uint ParseAssimpFlags(string flagStr, List<string> errors)
        {
            var tokens = flagStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0)
                return 0;

            if (tokens.Length == 1 && tokens[0].Equals("default", StringComparison.OrdinalIgnoreCase))
                return new ModelLoadOptions().AssimpFlags;

            if (tokens.Length == 1 && tokens[0].Equals("none", StringComparison.OrdinalIgnoreCase))
                return 0;

            var hasExplicit = tokens.Any(t => !t.StartsWith("+") && !t.StartsWith("-"));
            uint result = hasExplicit ? 0 : new ModelLoadOptions().AssimpFlags;

            foreach (var token in tokens)
            {
                var op = '+';
                var name = token;
                if (token.StartsWith("+") || token.StartsWith("-"))
                {
                    op = token[0];
                    name = token.Substring(1);
                }

                if (!Enum.TryParse<PostProcessSteps>(name, true, out var step) ||
                    !Enum.IsDefined(step))
                {
                    errors.Add($"error: unknown assimp flag '{name}'.");
                    continue;
                }

                if (op == '+')
                    result |= (uint)step;
                else
                    result &= ~(uint)step;
            }

            return result;
        }

        public static List<string> ToRelativeAnimations(string[] paths, string baseDirectory, List<string> errors)
        {
            var relative = new List<string>();
            foreach (var path in paths)
            {
                var full = Path.IsPathRooted(path)
                    ? Path.GetFullPath(path)
                    : Path.GetFullPath(Path.Combine(baseDirectory, path));

                var rel = Path.GetRelativePath(baseDirectory, full).Replace('\\', '/');
                if (rel.StartsWith(".."))
                {
                    errors.Add($"error: animation '{path}' is outside the Content directory.");
                    continue;
                }
                if (!File.Exists(full))
                {
                    errors.Add($"error: animation '{path}' not found.");
                    continue;
                }

                relative.Add(rel);
            }
            return relative;
        }
    }
}
