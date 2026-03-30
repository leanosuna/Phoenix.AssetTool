using ImGuiNET;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.Build;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Text;


namespace Phoenix.AssetTool.Gui
{
    public static class AssetBuildGui
    {
        
        static float timeout = 5;
        static float timeoutTimer = float.MaxValue;

        public static void DrawBuildWindow(float deltaTime)
        {
            if (ImGui.CollapsingHeader("build info", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (timeoutTimer < timeout)
                    timeoutTimer += deltaTime;

                var total = AssetBuildController.Status.BuildList.Count;
                var done = AssetBuildController.Status.BuildList.Count(a =>
                    a.State == AssetBuildState.Built ||
                    a.State == AssetBuildState.Skipped ||
                    a.State == AssetBuildState.Failed);

                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Vector4.UnitY);
                if (total > 0 && done != total)
                    ImGui.ProgressBar(done / (float)total, new Vector2(-1, 0));
                ImGui.PopStyleColor();
                if (done == total)
                    timeoutTimer = 0;

                ImGui.Separator();

                foreach (var item in AssetBuildController.Status.BuildList)
                {
                    if (item.Asset.Type == AssetType.ExtTexture)
                        continue;
                    DrawBuildItem(item);
                }
            }
            ImGui.Separator();

        }
        private static void DrawBuildItem(AssetBuildStatus item)
        {
            var color = item.State switch
            {
                AssetBuildState.Pending => FileTools.ColorWhite,
                AssetBuildState.Building => FileTools.ColorYellow,
                AssetBuildState.Encoding => FileTools.ColorYellow,
                AssetBuildState.Built => FileTools.ColorGreen,
                AssetBuildState.Failed => FileTools.ColorRed,
                AssetBuildState.Skipped => FileTools.ColorWhite,
                _ => FileTools.ColorWhite
            };

            ImGui.PushStyleColor(ImGuiCol.Text, color);

            ImGui.TextUnformatted(item.Asset.RelativePath);
            
            ImGui.SameLine(ImGui.GetWindowWidth() - 150);

            bool showProgress = false;
            var str = "";
            switch (item.State)
            {
                case AssetBuildState.Pending:
                    str = "Pending...";
                    break;
                case AssetBuildState.Building:
                    str ="Building...";
                    showProgress = true;
                    break;
                case AssetBuildState.Encoding:
                    str ="Encoding...";
                    showProgress = true; 
                    break;
                case AssetBuildState.Built:
                    str = "OK";
                    break;
                case AssetBuildState.Failed:
                    str = "FAIL";
                    break;
                case AssetBuildState.Skipped:
                    str = "Skipped";
                    break;
            }
            if (item.State != AssetBuildState.Built &&
                item.State != AssetBuildState.Skipped &&
                item.State != AssetBuildState.Failed) 
            str += $"{item.Step}/{item.MaxSteps}";
            ImGui.Text(str);
            if (showProgress)
            {
                ImGui.SetNextItemWidth(140);
                ImGui.SameLine(ImGui.GetWindowWidth() - 300);
                ImGui.ProgressBar(-1.0f * (float)ImGui.GetTime(), new Vector2(0.0f, 0.0f));
            }



            ImGui.PopStyleColor();

            if (item.State == AssetBuildState.Failed && item.Error != null)
            {
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(item.Error);
            }
        }

    }
}
