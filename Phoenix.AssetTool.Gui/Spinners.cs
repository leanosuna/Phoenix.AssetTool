using ImGuiNET;
using System.Numerics;

namespace Phoenix.AssetTool.Gui
{
    public static class Spinners
    {
        const float PI = MathF.PI;
        const float PI_2 = PI * 2f;
        const float PI_DIV_2 = PI / 2f;

        static uint Col(Vector4 c) => ImGui.GetColorU32(c);

        static int Segments(float radius)
        {
            int ri = (int)(radius + 0.999999f);
            if (ri < 50) return 16;
            if (ri < 100) return 32;
            if (ri < 200) return 48;
            if (ri < 400) return 64;
            return ri < 750 ? 96 : 128;
        }

        public static void SpinnerAng(string label, float radius, float thickness, Vector4 color, float speed, float angle = PI)
        {
            var pos = ImGui.GetCursorScreenPos();
            ImGui.Dummy(new Vector2(radius * 2, radius * 2));
            var centre = new Vector2(pos.X + radius, pos.Y + radius);
            var dl = ImGui.GetWindowDrawList();
            var ns = Segments(radius);
            float t = (float)ImGui.GetTime() * speed;

            dl.PathClear();
            for (int i = 0; i <= ns; i++)
            {
                float a = t + i * angle / ns;
                dl.PathLineTo(new Vector2(centre.X + MathF.Cos(a) * radius, centre.Y + MathF.Sin(a) * radius));
            }
            dl.PathStroke(Col(color), ImDrawFlags.None, thickness);
        }

        public static void SpinnerDots(string label, float radius, float thickness, Vector4 color, float speed, int dots = 12)
        {
            var pos = ImGui.GetCursorScreenPos();
            ImGui.Dummy(new Vector2(radius * 2, radius * 2));
            var centre = new Vector2(pos.X + radius, pos.Y + radius);
            var dl = ImGui.GetWindowDrawList();
            float t = (float)ImGui.GetTime() * speed;
            int mdots = dots / 2;
            float nextdot = dots;
            float angleOff = PI_2 / dots;

            dots = Math.Min(dots, 32);
            for (int i = 0; i <= dots; i++)
            {
                float a = t + i * angleOff;
                a %= PI_2;
                float th = thickness / 2f;
                if (nextdot + mdots < dots)
                {
                    if (i > nextdot && i < nextdot + mdots)
                        th = Math.Max(th, MathF.Sin(((i - nextdot) / mdots) * PI) * thickness);
                }
                else
                {
                    if ((i > nextdot && i < dots) || i < ((int)(nextdot + mdots)) % dots)
                        th = Math.Max(th, MathF.Sin(((i - nextdot) / mdots) * PI) * thickness);
                }
                dl.AddCircleFilled(new Vector2(centre.X + MathF.Cos(-a) * radius, centre.Y + MathF.Sin(-a) * radius), th, Col(color), 8);
            }
        }

        public static void SpinnerBounceDots(string label, float radius, float thickness, Vector4 color, float speed, int dots = 3)
        {
            var pos = ImGui.GetCursorScreenPos();
            float w = dots * (thickness * 2.5f);
            float h = thickness * 4f;
            ImGui.Dummy(new Vector2(w, h));
            var centre = new Vector2(pos.X + w / 2f, pos.Y + h / 2f);
            var dl = ImGui.GetWindowDrawList();
            float t = (float)ImGui.GetTime() * speed;
            float hsize = w / 2f - thickness * 1.25f;
            float offset = PI / dots;

            for (int i = 0; i < dots; i++)
            {
                float a = t + (PI - i * offset);
                float y = centre.Y + MathF.Sin(a * 0.8f) * thickness * 2f;
                dl.AddCircleFilled(new Vector2(centre.X - hsize + i * (thickness * 2.5f), MathF.Min(y, centre.Y)), thickness, Col(color), 8);
            }
        }

        public static void SpinnerFadeDots(string label, float thickness, Vector4 color, float speed, int dots = 6)
        {
            var pos = ImGui.GetCursorScreenPos();
            float spacing = thickness * 3f;
            float w = dots * spacing;
            float h = thickness * 3f;
            ImGui.Dummy(new Vector2(w, h));
            var centre = new Vector2(pos.X, pos.Y + h / 2f);
            var dl = ImGui.GetWindowDrawList();
            float t = (float)ImGui.GetTime() * speed;

            for (int i = 0; i < dots; i++)
            {
                float a = MathF.Sin(t + (PI - i * (PI / dots)) * 0.8f);
                var c = color;
                c.W *= MathF.Max(0.1f, a);
                dl.AddCircleFilled(new Vector2(centre.X + spacing / 2f + i * spacing, centre.Y), thickness, Col(c), 8);
            }
        }

        public static void SpinnerFadeBars(string label, float thickness, Vector4 color, float speed, int bars = 4)
        {
            var pos = ImGui.GetCursorScreenPos();
            float spacing = thickness * 2.5f;
            float barW = thickness * 1.2f;
            float barH = thickness * 5f;
            float w = bars * spacing + barW;
            float h = barH + thickness * 2f;
            ImGui.Dummy(new Vector2(w, h));
            var centre = new Vector2(pos.X, pos.Y + h / 2f);
            var dl = ImGui.GetWindowDrawList();
            float t = (float)ImGui.GetTime() * speed;

            for (int i = 0; i < bars; i++)
            {
                float a = MathF.Abs(MathF.Sin(t + i * (PI / bars)));
                float bh = MathF.Max(barH * a, barH * 0.15f);
                float x = centre.X + spacing / 2f + i * spacing;
                float y = centre.Y - bh / 2f;
                var c = color;
                c.W *= 0.3f + 0.7f * a;
                dl.AddRectFilled(new Vector2(x - barW / 2f, y), new Vector2(x + barW / 2f, y + bh), Col(c));
            }
        }

        public static void SpinnerBounceBall(string label, float radius, float thickness, Vector4 color, float speed)
        {
            var pos = ImGui.GetCursorScreenPos();
            float w = radius * 4f;
            float h = radius * 2.5f;
            ImGui.Dummy(new Vector2(w, h));
            var centre = new Vector2(pos.X + w / 2f, pos.Y + h / 2f);
            var dl = ImGui.GetWindowDrawList();
            float t = (float)ImGui.GetTime() * speed;

            float a = MathF.Abs(MathF.Sin(t));
            float y = centre.Y + (a - 0.5f) * radius * 1.2f;
            float r = thickness * (0.6f + 0.4f * a);
            dl.AddCircleFilled(new Vector2(centre.X, y), r, Col(color), 16);
        }

        public static void SpinnerBarChartSine(string label, float thickness, Vector4 color, float speed, int bars = 5)
        {
            var pos = ImGui.GetCursorScreenPos();
            float barW = thickness * 2f;
            float spacing = barW * 2f;
            float maxH = thickness * 6f;
            float w = bars * spacing + barW;
            float h = maxH + thickness * 2f;
            ImGui.Dummy(new Vector2(w, h));
            var centre = new Vector2(pos.X, pos.Y + h / 2f);
            var dl = ImGui.GetWindowDrawList();
            float t = (float)ImGui.GetTime() * speed;

            for (int i = 0; i < bars; i++)
            {
                float a = MathF.Sin(t + i * (PI / bars));
                float bh = MathF.Max(0.5f, (a + 1f) / 2f * maxH);
                float x = centre.X + spacing / 2f + i * spacing;
                float y = centre.Y - bh / 2f;
                dl.AddRectFilled(new Vector2(x - barW / 2f, y), new Vector2(x + barW / 2f, y + bh), Col(color));
            }
        }

        public static void SpinnerMoonLine(string label, float radius, float thickness, Vector4 color, Vector4 bg, float speed)
        {
            var pos = ImGui.GetCursorScreenPos();
            ImGui.Dummy(new Vector2(radius * 2, radius * 2));
            var centre = new Vector2(pos.X + radius, pos.Y + radius);
            var dl = ImGui.GetWindowDrawList();
            var ns = Segments(radius);
            float t = (float)ImGui.GetTime() * speed;
            float angleOff = PI_2 / (ns - 1);

            dl.PathClear();
            for (int i = 0; i < ns; i++)
                dl.PathLineTo(new Vector2(centre.X + MathF.Cos(i * angleOff) * radius, centre.Y + MathF.Sin(i * angleOff) * radius));
            dl.PathStroke(Col(bg), ImDrawFlags.None, thickness);

            dl.AddLine(centre, new Vector2(centre.X + MathF.Cos(t) * radius, centre.Y + MathF.Sin(t) * radius), Col(color), thickness * 2f);
        }

        static Vector4 Hsv(float h, float s, float v, float a = 1f)
        {
            ImGui.ColorConvertHSVtoRGB(h, s, v, out float r, out float g, out float b);
            return new Vector4(r, g, b, a);
        }
    }
}
