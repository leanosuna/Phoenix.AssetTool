using ImGuiNET;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.Build;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace Phoenix.AssetTool.Gui
{
    public static class AssetBrowserGui
    {
        public static (AssetEntry? asset, AssetType type, string path) SelectedFileOptions = (null, AssetType.Unknown, "");
        public static bool ShowOptions = true;
        static DirectoryBrowserMeta _directoryBrowserMeta = default!;
        //static List<string> outsideContent = new();
        static List<ExternalAsset> externalAssets = new();
        public static void UpdateDirectory(bool res = false)
        {
            _directoryBrowserMeta = ProcessDirectoryRec(Manifest.BaseDirectory);
        }
        public static void DrawDirFileTree(float dt)
        {
            if (_directoryBrowserMeta == null)
                UpdateDirectory();

            Vector4 col;
            var switchTheme = AssetToolGui.DarkTheme ? "light" : "dark";
            if (ImGui.Button($"{switchTheme}"))
            {
                AssetToolGui.ToggleTheme();
            }
            ImGui.SameLine();
            ImGui.Text("zoom");
            ImGui.SameLine();
            if (ImGui.Button($"+"))
            {
                AssetToolGui.FontSizeUp();
            }
            ImGui.SameLine();
            if (ImGui.Button($"-"))
            {
                AssetToolGui.FontSizeDown();
            }

            ImGui.SameLine();
            ImGui.Text("|");
            ImGui.SameLine();
            if (ImGui.Button("Select Files..."))
            {
                if (AssetToolGui.OpenAssetFilePicker(out var files))
                {
                    List<(string abs, string relative)> paths  = files.Select(f => (f, Path.GetRelativePath(Manifest.BaseDirectory, f).Replace("\\", "/"))).ToList();
                    var outsideContent = paths.FindAll(p => p.relative.StartsWith("../"));
                    externalAssets = outsideContent.Select(p => new ExternalAsset { Path = p.abs }).ToList();

                    if (externalAssets.Count == 0)
                    {
                        paths.ForEach(p => FileTools.AddFile(p.relative, false));
                        Manifest.Save();
                        UpdateDirectory();
                    }
                    else
                    {
                        ImGui.OpenPopup("outside-content");
                    }
                }

            }
            

            if (ImGui.BeginPopup("outside-content"))
            {
                ImGui.Text("The following items are outside the content folder.");
                ImGui.Text("Where do we copy them?");

                foreach (var asset in externalAssets)
                {
                    ImGui.Text(asset.Path);
                    ImGui.SameLine();

                    var dirStr = asset.DirectorySet ?
                        (asset.DirectoryValid ?
                            $"{asset.DirectorySelected} " :
                            "invalid directory. must be withing content.")
                        : $"Select directory...##{asset.Path}";

                    if (ImGui.Button(dirStr))
                    {
                        if (FileTools.FolderPicker(out var dir))
                        {
                            var dirToDir = Path.GetRelativePath(Manifest.BaseDirectory, dir).Replace("\\", "/");
                            
                            asset.DirectorySelected = dir;
                            asset.DirectoryValid = !dirToDir.StartsWith("../");
                            asset.DirectorySet = true;

                        }
                    }
                }

                if (ImGui.Button("OK"))
                {
                    foreach(var e in externalAssets)
                    {
                        if(e.DirectorySet && e.DirectoryValid)
                        {   
                            var name = Path.GetFileName(e.Path);
                            var dstPath = Path.Combine(e.DirectorySelected, name);
                            var relative = Path.GetRelativePath(Manifest.BaseDirectory, dstPath).Replace("\\", "/");
                            File.Copy(e.Path, dstPath, overwrite: true);
                            FileTools.AddFile(relative, false);
                        }
                    }
                    Manifest.Save();
                    UpdateDirectory();

                    externalAssets.Clear();
                    ImGui.CloseCurrentPopup();

                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    externalAssets.Clear();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            ImGui.SameLine();
            if(ImGui.Button("All"))
            {
                var contentDir = Path.Combine(Manifest.BaseDirectory, "Content");
                var targetDir = Directory.Exists(contentDir) ? contentDir : Manifest.BaseDirectory;
                Log.Info($"Adding all files from {targetDir}...");
                FileTools.AddDirectory(targetDir);
                Log.Info("Done.");
                UpdateDirectory();
            }
            if(ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Add all files in Content folder");
                ImGui.EndTooltip();
            }

            ImGui.SameLine();

            ImGui.SameLine();
            ImGui.Text("|");
            ImGui.SameLine();
            col = AssetToolGui.DarkTheme ? FileTools.ColorGreen: FileTools.ColorGreenDark;
            ImGui.PushStyleColor(ImGuiCol.Text, col);
            if (ImGui.Button("Build All"))
            {
                _ = AssetBuildController.StartBuild(rebuild:false, UpdateDirectory);
            }
            ImGui.PopStyleColor();
            ImGui.SameLine();
            col = AssetToolGui.DarkTheme ? FileTools.ColorCyan : FileTools.ColorCyanDark;
            ImGui.PushStyleColor(ImGuiCol.Text, col);
            if (ImGui.Button("Rebuild All"))
            {
                _ = AssetBuildController.StartBuild(rebuild: true, UpdateDirectory);
            }
            ImGui.PopStyleColor();
            ImGui.NewLine();
            AssetBuildGui.DrawBuildWindow(dt);
            ImGui.NewLine();
            ImGui.Separator();
            var nameSpace = Manifest.Namespace;
            ImGui.Text("Generator: Shader helpers namespace");
            ImGui.InputText("##Shader namespace", ref nameSpace, 100);
            ImGui.Separator(); 
            ImGui.NewLine();

            if (Manifest.Namespace != nameSpace)
            {
                Manifest.Namespace = nameSpace;
                Manifest.Save();
            }

            if (ImGui.CollapsingHeader("Assets selected", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawBrowser(manifestOnly: true);
            }


            if (ImGui.CollapsingHeader("File selector"))
            {
                DrawBrowser(manifestOnly: false);
            }
        }


        private static DirectoryBrowserMeta ProcessDirectoryRec(string currentDir)
        {
            var name = Path.GetFileName(currentDir);
            //if (name.Equals("ContentBin", StringComparison.OrdinalIgnoreCase))
            //    return;

            var children = new List<DirectoryBrowserMeta>();
            foreach(var dir in Directory.GetDirectories(currentDir))
                children.Add(ProcessDirectoryRec(dir));

            var files = ProcessDirectoryFiles(Directory.GetFiles(currentDir));

            return new DirectoryBrowserMeta
            {
                Name = name,
                FilesMeta = files,
                Children = children,
                Path = currentDir
            };
            
        }
        private static void DrawBrowser(bool manifestOnly)
        {
            if (DrawBrowserRec(_directoryBrowserMeta, manifestOnly))
                UpdateDirectory();
            
        }
        private static bool DrawBrowserRec(DirectoryBrowserMeta meta, bool manifestOnly)
        {
            bool update = false;
            if (!manifestOnly)
            {
                ImGui.SameLine();
                if (ImGui.SmallButton("Toggle folder"))
                {
                    FileTools.ToggleDirectory(meta.Path);
                    update = true;
                }
            }
            else
            {
                //ImGui.SameLine();
                //if (ImGui.SmallButton("X"))
                //{
                //    FileTools.ToggleDirectory(meta.Path);
                //    FileTools.SaveManifest();
                //    update = true;
                //};
            }

            foreach (var child in meta.Children)
            {
                if (child.Name.Equals("ContentBin", StringComparison.OrdinalIgnoreCase))
                    continue;
                var flags = ImGuiTreeNodeFlags.None;
                if (manifestOnly)
                {
                    if (!child.ContainsTracked)
                        continue;
                    flags = ImGuiTreeNodeFlags.DefaultOpen;
                }

                
                if (ImGui.TreeNodeEx($"{child.Name}##{manifestOnly}", flags))
                {
                    update |= DrawBrowserRec(child, manifestOnly);
                    ImGui.TreePop();
                }


            }
            foreach(var file in meta.FilesMeta)
            {
                update |= DrawFile(file, manifestOnly);
            }
            return update;
        }

        static Vector4 GetColor(
             bool tracked, bool built)
        {
            if (!tracked)
                return AssetToolGui.DarkTheme ? FileTools.ColorWhite : FileTools.ColorBlack;
            
            var a = AssetToolGui.DarkTheme ? FileTools.ColorGreen : FileTools.ColorGreenDark;
            var b = AssetToolGui.DarkTheme ? FileTools.ColorYellow : FileTools.ColorYellowDark;
            return built ? a : b;
        }
        private static List<FileBrowserMeta> ProcessDirectoryFiles(string[] files)
        {
            var filesMeta = new List<FileBrowserMeta>();
            foreach(var file in files)
            {
                var name = Path.GetFileName(file);

                if (name.Equals(Manifest.Name, 
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                var type = FileTools.GuessType(file);
                if (type == AssetType.Unknown)
                    continue;

                var relative = Path
                    .GetRelativePath(Manifest.BaseDirectory, file)
                    .Replace('\\', '/');

                var asset = Manifest.Assets
                    .FirstOrDefault(a =>
                        a.RelativePath.Equals(relative, 
                        StringComparison.OrdinalIgnoreCase));

                (var tracked, var built) = FileTools.VerifyAsset(asset);
                var color = GetColor(tracked, built);

                
                filesMeta.Add(new FileBrowserMeta
                {
                    Name = name,
                    Asset = asset!,
                    Type = type,
                    RelativePath = relative,
                    Tracked = tracked,
                    Built = built,
                    Color = color,
                });
            }
            return filesMeta;
        }

        private static bool DrawFile(FileBrowserMeta meta, bool manifestOnly)
        {
            bool update = false;
            if (!meta.Tracked && manifestOnly)
                return update;

            //if (manifestOnly)
            //{
            //    ImGui.TextColored(meta.Color, meta.Name);
            //    return update;
            //}

            ImGui.PushStyleColor(ImGuiCol.Text, meta.Color);
            bool sel = false;

            var flags = manifestOnly ? ImGuiSelectableFlags.AllowOverlap : ImGuiSelectableFlags.AllowDoubleClick;
            if (ImGui.Selectable(meta.Name, ref sel, flags))
            {
                SelectedFileOptions.asset = meta.Asset;
                SelectedFileOptions.type = meta.Type;
                SelectedFileOptions.path = meta.RelativePath;
                ShowOptions = true;
                
                if(!manifestOnly)
                {
                    if (ImGui.IsMouseDoubleClicked(0))
                    {
                        FileTools.ToggleFile(meta.RelativePath, true);
                        update = true;
                    }
                }
            }
            if (manifestOnly)
            {
                ImGui.SameLine();
                if (ImGui.SmallButton($"X##{meta.Name}"))
                {
                    FileTools.RemoveFile(meta.RelativePath, true);
                    update = true;
                }
            }
            
            ImGui.PopStyleColor();

            return update;
        }

        
        public static void RefreshSelection()
        {
            SelectedFileOptions.asset = Manifest.Assets
                .FirstOrDefault(a =>
                    a.RelativePath.Equals(SelectedFileOptions.path, StringComparison.OrdinalIgnoreCase));
        }
    }
}
