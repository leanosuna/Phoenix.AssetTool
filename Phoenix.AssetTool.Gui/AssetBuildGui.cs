using ImGuiNET;
using Phoenix.AssetTool.Core;
using Phoenix.AssetTool.Core.Build;
using System.Numerics;


namespace Phoenix.AssetTool.Gui
{
    public static class AssetBuildGui
    {
        static bool showPendingOnly = false;
        public static AssetBuildStatus? Selected;
        public static void DrawBuildWindow(float deltaTime)
        {
            var buildList = AssetBuildController.Status.BuildList;
            var total = buildList.Count;
            var done = buildList.Count(a =>
                a.State == AssetBuildState.Built ||
                a.State == AssetBuildState.Skipped ||
                a.State == AssetBuildState.Failed);

            if (Selected != null && !buildList.Any(a => ReferenceEquals(a, Selected)))
                Selected = null;

            var headerOpen = ImGui.CollapsingHeader("build info");
            
            
            if (total > 0 && done != total)
            {
                ImGui.SameLine();
                var barColor = AssetToolGui.DarkTheme ? FileTools.ColorGreen : FileTools.ColorGreenDark;
                ImGui.PushStyleColor(ImGuiCol.PlotHistogram, barColor);
                ImGui.ProgressBar(done / (float)total, new Vector2(-1, 0));
                ImGui.PopStyleColor();
            }

            if (headerOpen)
            {
                ImGui.Checkbox("Show pending only", ref showPendingOnly);

                foreach (var item in buildList)
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
            if (showPendingOnly && (item.State == AssetBuildState.Built || item.State == AssetBuildState.Skipped))
                return;

            var dark = AssetToolGui.DarkTheme;
            var color = item.State switch
            {
                AssetBuildState.Pending => dark ? FileTools.ColorWhite : FileTools.ColorBlack,
                AssetBuildState.Building => dark ? FileTools.ColorYellow : FileTools.ColorYellowDark,
                AssetBuildState.Encoding => dark ? FileTools.ColorYellow : FileTools.ColorYellowDark,
                AssetBuildState.Built => dark ? FileTools.ColorGreen : FileTools.ColorGreenDark,
                AssetBuildState.Failed => dark ? FileTools.ColorRed: FileTools.ColorRedDark,
                AssetBuildState.Skipped => dark ? FileTools.ColorWhite: FileTools.ColorBlack,
                _ => FileTools.ColorWhite
            };

            bool showProgress = false;
            
            var str = "";
            switch (item.State)
            {
                case AssetBuildState.Pending:
                    str = "Pending...";
                    break;
                case AssetBuildState.Building:
                    str = "Building...";
                    showProgress = true;
                    break;
                case AssetBuildState.Encoding:
                    str = "Encoding...";
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
            if (showProgress)
            {
                //ImGui.SameLine(ImGui.GetWindowWidth() - 300);
                DrawSpinner(item, color);
                ImGui.SameLine();
                str += $"{item.Step}/{item.MaxSteps}";
                ImGui.Text(str);
                ImGui.SameLine();
            }
               
            ImGui.PushStyleColor(ImGuiCol.Text, color);

            //ImGui.SameLine();
            var error = item.State == AssetBuildState.Failed && item.Error != null;
            ImGui.Text(item.Asset.RelativePath);
            

            if (error)
            {
                if (ImGui.IsItemClicked())
                {
                    //Console.WriteLine("asd");
                    AssetBrowserGui.ShowOptions = false;
                    Selected = item;
                }
                ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("(click to open on the right)\n"+item.Error);

                ImGui.PushStyleColor(ImGuiCol.Text, color);
                ImGui.SameLine();
                ImGui.Text(str);
            }
            ImGui.PopStyleColor();
        }

        static void DrawSpinner(AssetBuildStatus item, Vector4 color)
        {
            Spinners.SpinnerAng($"##ang_{item.Asset.RelativePath}", 8, 2f, color, 5);
        }

    }
}
