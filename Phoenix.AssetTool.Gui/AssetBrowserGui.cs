using ImGuiNET;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.Build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace Phoenix.AssetTool.Gui
{
    public static class AssetBrowserGui
    {
        static string manifestAbsolutePath;
        static string manifestRootDirectory;
        static AssetManifest manifest;
        public static void SetManifest(AssetManifest assetManifest, string root)
        {
            manifest = assetManifest;
            manifestAbsolutePath = root;
            manifestRootDirectory = assetManifest.BaseDirectory;
            FileTools.SetManifest(assetManifest, root);
            AssetOptionsGui.SetManifest(assetManifest);
        }
        public static void DrawDirFileTree(float dt)
        {
            if (manifest == null)
                return;

            if (ImGui.Button("Build"))
            {
                AssetBuildController.StartBuild(manifest, false);
            }
            ImGui.SameLine();
            if (ImGui.Button("Rebuild"))
            {
                AssetBuildController.StartBuild(manifest, true);
            }

            AssetBuildGui.DrawBuildWindow(dt);

            ImGui.NewLine();



            if (string.IsNullOrEmpty(manifestRootDirectory) ||
                !Directory.Exists(manifestRootDirectory))
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "Invalid Content directory");
                return;
            }

            DrawDirectoryRecursive(manifestRootDirectory);
        }

        private static void DrawDirectoryRecursive(string currentDir)
        {
            
            foreach (var dir in Directory.GetDirectories(currentDir))
            {
                var name = Path.GetFileName(dir);
                if (name.Equals("ContentBin", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (ImGui.TreeNode(name))
                {
                    ImGui.SameLine();
                    if (ImGui.SmallButton("Toggle folder"))
                    {
                        FileTools.ToggleDirectory(dir);
                        FileTools.SaveManifest();
                       
                    }

                    DrawDirectoryRecursive(dir);
                    ImGui.TreePop();
                }
            }

            foreach (var file in Directory.GetFiles(currentDir))
            {
                DrawFile(file);
            }
        }
        public static (AssetEntry asset, AssetType type, string path) SelectedFileOptions = (null, AssetType.Unknown, "");
        private static void DrawFile(
            string absoluteFilePath)
        {
            // Filter out manifest json itself
            if (Path.GetFileName(absoluteFilePath)
                .Equals(Path.GetFileName(manifestAbsolutePath), StringComparison.OrdinalIgnoreCase))
                return;

            var type = FileTools.GuessType(absoluteFilePath);
            if (type == AssetType.Unknown)
                return;

            var relative = Path
                .GetRelativePath(manifestRootDirectory, absoluteFilePath)
                .Replace('\\', '/');

            var asset = manifest.Assets
                .FirstOrDefault(a =>
                    a.RelativePath.Equals(relative, StringComparison.OrdinalIgnoreCase));

            (var tracked, var built) = FileTools.VerifyAsset(asset);
            var color = FileTools.GetColor(tracked, built);

            ImGui.PushStyleColor(ImGuiCol.Text, color);
            bool sel = false;
            //assetOptions.showOptions = false;


            if (ImGui.Selectable(Path.GetFileName(absoluteFilePath),ref sel, ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.AllowOverlap))
            {
                SelectedFileOptions.asset = asset;
                SelectedFileOptions.type = type;
                SelectedFileOptions.path = relative;
                if (ImGui.IsMouseDoubleClicked(0))
                {
                    FileTools.ToggleFile(relative, true);
                }
            }
            ImGui.SameLine();
            
            if(ImGui.Checkbox("##", ref tracked))
            {
                FileTools.AddFile(relative);
            }

            ImGui.PopStyleColor();
        }

        public static void RefreshSelection()
        {
            SelectedFileOptions.asset = manifest.Assets
                .FirstOrDefault(a =>
                    a.RelativePath.Equals(SelectedFileOptions.path, StringComparison.OrdinalIgnoreCase));
        }
    }
}
