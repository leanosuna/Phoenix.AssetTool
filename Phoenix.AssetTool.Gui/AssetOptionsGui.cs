using ImGuiNET;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.Build;
using Phoenix.AssetTool.Core.Model;
using Phoenix.AssetTool.Core.Shader;
using Phoenix.AssetTool.Core.Texture;
using Silk.NET.Assimp;
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

        static AssetLoadOptions assetLoadOptions;

        static ModelLoadOptions modelOptions;
        static TextureLoadOptions textureOptions;
        static ShaderLoadOptions shaderOptions;


        public static void SetManifest(AssetManifest manifest)
        {
            assetManifest = manifest;
        }
        public static void Draw(AssetEntry asset, AssetType type, string path)
        {
            if (path == "")
                return;
            assetLoadOptions = FileTools.LoadAssetOptions();

            DrawFileHeader(asset, type, path);
            
            switch (type)
            {
                case AssetType.Model:
                    
                    DrawModelOptions(asset,path);
                    break;
                case AssetType.Texture:
                    DrawTextureOptions();
                    break;
                case AssetType.Shader:
                    DrawShaderOptions();
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
                    var assetOptions = FileTools.LoadAssetOptions();

                    switch(type)
                    {
                        case AssetType.Model:
                            assetOptions.Models[asset.RelativePath] = modelOptions;
                            break;

                    }
                    FileTools.SaveAssetOptions();
                    

                }


                ImGui.SameLine();
                if(!built)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, .5f, 0, 1));
                    if (ImGui.Button("Build"))
                    {
                        AssetBuildController.StartBuildAsset(assetManifest, asset, false);
                    }
                    ImGui.PopStyleColor();
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, .5f, .5f, 1));
                    if (ImGui.Button("Rebuild"))
                    {
                        AssetBuildController.StartBuildAsset(assetManifest, asset, true);
                    }
                    ImGui.PopStyleColor();
                }
            }
            ImGui.NewLine();
        }
        public static void DrawModelOptions(AssetEntry asset, string path)
        {
            if (!assetLoadOptions.Models.TryGetValue(path, out modelOptions))
            {
                modelOptions = new ModelLoadOptions();

                assetLoadOptions.Models[path] = modelOptions;
            }
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
                    AssetToolGui.OpenAnimationFilePicker(options);
            }
            options.IsAnimated = animated;

            if(options.IsAnimated)
            {
                if(ImGui.CollapsingHeader("Selected Animations", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    foreach(var anim in options.AnimationFiles)
                    {
                        var name = Path.GetFileName(anim);
                        ImGui.Text(name);
                    }
                }
            }
            ImGui.Separator();

            
        }
        public static void DrawTextureOptions()
        {
            ImGui.Text($"TEXTURE LOAD OPTIONS");
        }
        public static void DrawShaderOptions()
        {
            ImGui.Text($"SHADER LOAD OPTIONS");
        }

        
    }
}
