using BCnEncoder.Shared;
using ImGuiNET;
using NativeFileDialogNET;
using Phoenix.AssetImport.Texture;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.AssetBuildOptions;
using Phoenix.AssetTool.Core.Build;
using Phoenix.AssetTool.Core.Model;
using Phoenix.AssetTool.Core.Shader;
using Phoenix.AssetTool.Core.Texture;
using Silk.NET.Assimp;
using Silk.NET.Core.Native;
using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace Phoenix.AssetTool.Gui
{
    public static class AssetOptionsGui
    {

        //static AssetLoadOptions assetLoadOptionsCache = new AssetLoadOptions();

        static AssetManifest assetManifest = default!;

        
        static ModelLoadOptions modelOptions;
        static TextureLoadOptions textureOptions;
        static ShaderLoadOptions shaderOptions;


        public static void SetManifest(AssetManifest manifest)
        {
            assetManifest = manifest;
        }
        public static void Draw((AssetEntry? asset, AssetType type, string path) sfo)
        {
            var asset = sfo.asset;
            var type = sfo.type;
            var path = sfo.path;

            if (path == "")
                return;
            
            DrawFileHeader(asset, type, path);
            
            switch (type)
            {
                case AssetType.Model:
                    
                    DrawModelOptions(asset,path);
                    break;
                case AssetType.Texture:
                    DrawTextureOptions(asset, path);
                    break;
                case AssetType.Shader:
                    DrawShaderOptions(asset, path);
                    break;
            }
        }

        public static void DrawFileHeader(AssetEntry asset, AssetType type, string path)
        {
            var name = Path.GetFileName(path);
            ImGui.Text($"{type.ToString()} Load Options: {name}");

            (bool tracked, bool built) = FileTools.VerifyAsset(asset);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(.5f, .5f, 0, 1));
            if (ImGui.Button("Add"))
            {
                FileTools.AddFile(path);

                AssetBrowserGui.RefreshSelection();

            }
            ImGui.PopStyleColor();
            
            if (tracked)
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(.5f, 0, 0, 1));
                if (ImGui.Button("Remove"))
                {
                    FileTools.RemoveFile(path);
                    AssetBrowserGui.RefreshSelection();
                    
                }
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.Text("|");
                ImGui.SameLine();
                if (ImGui.Button("Save options"))
                {
                    
                    
                    
                }


                ImGui.SameLine();
                if(!built)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, .5f, 0, 1));
                    if (ImGui.Button("Build"))
                    {
                        SaveOptions(asset);
                        AssetBuildController.StartBuildAsset(asset, false);
                    }
                    ImGui.PopStyleColor();
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, .5f, .5f, 1));
                    if (ImGui.Button("Rebuild"))
                    {
                        SaveOptions(asset); 
                        AssetBuildController.StartBuildAsset(asset, true);
                    }
                    ImGui.PopStyleColor();
                }
            }
            ImGui.NewLine();
        }
        static void SaveOptions(AssetEntry asset)
        {
            switch (asset.Type)
            {
                case AssetType.Model:
                    AssetOptions.Set(asset.RelativePath, modelOptions);
                    break;
                case AssetType.Texture:
                    AssetOptions.Set(asset.RelativePath, textureOptions);
                    break;
                case AssetType.Shader:
                    AssetOptions.Set(asset.RelativePath, shaderOptions);
                    break;

            }
        }


        public static void DrawModelOptions(AssetEntry asset, string path)
        {
            modelOptions = AssetOptions.OfModel(path);
            var options = modelOptions;

            var flags = options.AssimpFlags;
            if (ImGui.CollapsingHeader("Assimp flags"))
            {
                ImGui.CheckboxFlags("Triangulate", ref flags, (int)PostProcessSteps.Triangulate);

                if (ImGui.CheckboxFlags("Generate Smooth Normals", ref flags, (int)PostProcessSteps.GenerateSmoothNormals))
                {
                    var en = (PostProcessSteps)flags;
                    if (en.HasFlag(PostProcessSteps.GenerateNormals))
                    {
                        en &= ~PostProcessSteps.GenerateNormals;
                        flags = (uint)en;
                    }
                }
                if (ImGui.CheckboxFlags("Generate Normals", ref flags, (int)PostProcessSteps.GenerateNormals))
                {
                    var en = (PostProcessSteps)flags;
                    if (en.HasFlag(PostProcessSteps.GenerateSmoothNormals))
                    {
                        en &= ~PostProcessSteps.GenerateSmoothNormals;
                        flags = (uint)en;
                    }
                }
                //TODO: check missing
                ImGui.CheckboxFlags("Generate UV", ref flags, (int)PostProcessSteps.GenerateUVCoords);

                ImGui.CheckboxFlags("Join Vertices", ref flags, (int)PostProcessSteps.JoinIdenticalVertices);
                ImGui.CheckboxFlags("Sort by Type", ref flags, (int)PostProcessSteps.SortByPrimitiveType);

                ImGui.CheckboxFlags("Flip UV", ref flags, (int)PostProcessSteps.FlipUVs);
                ImGui.CheckboxFlags("Improve cache locality", ref flags, (int)PostProcessSteps.ImproveCacheLocality);
                ImGui.CheckboxFlags("Optimize Graph", ref flags, (int)PostProcessSteps.OptimizeGraph);
                ImGui.CheckboxFlags("Optimize Meshes", ref flags, (int)PostProcessSteps.OptimizeMeshes);
                ImGui.CheckboxFlags("Limit bone weights", ref flags, (int)PostProcessSteps.LimitBoneWeights);
                ImGui.CheckboxFlags("Find Degenerates", ref flags, (int)PostProcessSteps.FindDegenerates);
                AssetToolGui.ShowHelpTooltip("Likely destroys most models.");
                ImGui.CheckboxFlags("Fix In Facing normals", ref flags, (int)PostProcessSteps.FixInFacingNormals);
                ImGui.CheckboxFlags("PreTransform Vertices", ref flags, (int)PostProcessSteps.PreTransformVertices);
                AssetToolGui.ShowHelpTooltip("Optimizes mesh hierarchy at the cost of per mesh drawing.");

                ImGui.CheckboxFlags("Flip winding", ref flags, (int)PostProcessSteps.FlipWindingOrder);
                ImGui.CheckboxFlags("Split Large Meshes", ref flags, (int)PostProcessSteps.SplitLargeMeshes);

                options.AssimpFlags = flags;




            }

            ImGui.Separator();
            var et = options.ExtractTextures;
            ImGui.Checkbox("Extract Textures", ref et);
            options.ExtractTextures = et;

            ImGui.Separator();
            var animated = options.IsAnimated;
            if (ImGui.Checkbox("Is Animated", ref animated))
            {
                if(animated)
                    OpenAnimationFilePicker(asset.RelativePath, options);


            }
            options.IsAnimated = animated;

            if(options.IsAnimated)
            {
                if(ImGui.CollapsingHeader("Selected Animations", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    foreach(var anim in options.AnimationFiles)
                    {
                        var name = Path.GetFileNameWithoutExtension(anim);
                        ImGui.Text(name);
                    }
                }
            }
            ImGui.Separator();

            
        }

        internal static void OpenAnimationFilePicker(string path, ModelLoadOptions options)
        {
            using var dlg = new NativeFileDialog()
            .SelectFile()
            .AllowMultiple()
            .AddFilter("animation files", "fbx");

            DialogResult result = dlg.Open(out string[]? files, defaultPath: Environment.CurrentDirectory);
            if (result == DialogResult.Okay && files != null && files.Length > 0)
            {
                options.AnimationFiles = files.ToList();
                AssetOptions.Set(path, options);
            }
            else
            {
            }

        }
        public static void DrawTextureOptions(AssetEntry asset, string path)
        {
            ImGui.Text($"TEXTURE LOAD OPTIONS");

            textureOptions = AssetOptions.OfTexture(path);
            
            var options = textureOptions;
            var mipEnabled = options.GenerateMipMaps;

            var current = (int)options.Format;

            var compList = options.Format.Strings();

            ImGui.Checkbox("Generate mipmaps", ref mipEnabled);            
            options.GenerateMipMaps = mipEnabled;

            ImGui.Text("Compression Format");
            if (ImGui.ListBox("##1", ref current, compList, compList.Length))
                options.Format = (AssetCompressionFormat)current;

            var wrapList = options.WrapS.Strings();
                        
            int current2 = options.WrapS.Index();
            ImGui.Text("Wrap Horizontal");
            if (ImGui.ListBox("##2", ref current2, wrapList, wrapList.Length))
                options.WrapS = options.WrapS.At(current2);
            
            var current3 = options.WrapT.Index();
            ImGui.Text("Wrap Vertical");
            if (ImGui.ListBox("##3", ref current3, wrapList, wrapList.Length))
                options.WrapT = options.WrapT.At(current3);

            var filterList = options.Min.Strings();

            var current4 = options.Min.Index();
            ImGui.Text("Min Filter");
            if (ImGui.ListBox("##4", ref current4, filterList, filterList.Length))
                options.Min = options.Min.At(current4);

            var current5 = options.Mag.Index();
            ImGui.Text("Mag Filter"); 
            if (ImGui.ListBox("##5", ref current5, filterList, 2))
                options.Mag = options.Mag.At(current5);

        }
        public static void DrawShaderOptions(AssetEntry asset, string path)
        {
            var pathAbs = Path.Combine(
                Manifest.BaseDirectory, asset.RelativePath);

            //var name = Path.GetFileName(asset.RelativePath);
            //ImGui.Text($"");

            var content = System.IO.File.ReadAllText(pathAbs);
            ImGui.Text(content);
        }

        
    }
}
