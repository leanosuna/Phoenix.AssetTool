using ImGuiNET;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.Build;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Text;


namespace Phoenix.AssetTool.Gui
{
    public static class AssetBuildGui
    {
        static AssetManifest manifest;

        static float timeout = 5;
        static float timeoutTimer = float.MaxValue;

        public static void DrawBuildWindow(float deltaTime)
        {
            if (ImGui.CollapsingHeader("build info", ImGuiTreeNodeFlags.DefaultOpen))
            {


                //ImGui.Begin("Asset Build");
                if (timeoutTimer < timeout)
                    timeoutTimer += deltaTime;

                //if (!controller.IsBuilding && timeoutTimer > timeout)
                //{
                //    return;
                //    //if (ImGui.Button("Build"))
                //    //    controller.StartBuild(CurrentManifest, rebuild: false);

                //    //ImGui.SameLine();

                //    //if (ImGui.Button("Rebuild"))
                //    //    controller.StartBuild(CurrentManifest, rebuild: true);
                //}


                //ImGui.Begin("Asset Build");

                //if (ImGui.Button("Cancel"))
                //    controller.Cancel();


                var total = AssetBuildController.BuildList.Count;
                var done = AssetBuildController.BuildList.Count(a =>
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

                foreach (var item in AssetBuildController.BuildList)
                {
                    DrawBuildItem(item);
                }
            }
            //ImGui.End();
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
            switch (item.State)
            {
                case AssetBuildState.Pending:
                    ImGui.Text("Pending...");
                    break;
                case AssetBuildState.Building:
                    ImGui.Text("Building...");
                    showProgress = true;
                    break;
                case AssetBuildState.Encoding:
                    ImGui.Text("Encoding...");
                    showProgress = true; 
                    break;
                case AssetBuildState.Built:
                    ImGui.Text("OK");
                    break;
                case AssetBuildState.Failed:
                    ImGui.Text("FAIL");
                    break;
                case AssetBuildState.Skipped:
                    ImGui.Text("Skipped");
                    break;
            }
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
