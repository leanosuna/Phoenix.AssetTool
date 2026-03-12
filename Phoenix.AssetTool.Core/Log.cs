using System.Numerics;
using System.Text;

namespace Phoenix.AssetTool.Core
{
    public static class Log
    {
        private static readonly object _lock = new object();
        private static readonly string _logFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LOG.txt");
        public static bool Verbose = false;
        public static bool ConsoleWrite = false;
        public static bool Date = false;
        public static bool Time = false;
        public static bool Enabled = false;
        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);
        public static void Error(string message) => Write("ERROR", message);
        public static void Debug(string message) => Write("DEBUG", message);
        public static void Exception(string message) => Write("EXCEPTION", message);

        private static void Write(string level, string message)
        {
            if (!Enabled)
                return;
            var now = DateTime.Now;
            var date = Date ? $"{now:dd-MM-yyyy} " : "";
            var time = Time ? $"{now:HH:mm:ss}" : "";
            var dateTime = Date || Time ? $"[{date}{time}] " : "";
            var line = Verbose ? $"{dateTime}[{level}] {message}" : message;

            lock (_lock)
            {
                if (ConsoleWrite)
                    Console.WriteLine(line);

                File.AppendAllText(_logFile, line + Environment.NewLine);
            }
        }

        public static void ClearLog()
        {
            lock (_lock)
            {
                if (File.Exists(_logFile))
                {
                    File.Delete(_logFile);
                }
            }
        }
        public static string ToStr(this Matrix4x4 m)
        {
            if (m.IsIdentity)
                return "Identity";

            StringBuilder sb = new StringBuilder();
            sb.Append(m.M11);
            sb.Append(", ");
            sb.Append(m.M12);
            sb.Append(", ");
            sb.Append(m.M13);
            sb.Append(", ");
            sb.Append(m.M14);
            sb.Append("\n");
            sb.Append(m.M21);
            sb.Append(", ");
            sb.Append(m.M22);
            sb.Append(", ");
            sb.Append(m.M23);
            sb.Append(", ");
            sb.Append(m.M24);
            sb.Append("\n");
            sb.Append(m.M31);
            sb.Append(", ");
            sb.Append(m.M32);
            sb.Append(", ");
            sb.Append(m.M33);
            sb.Append(", ");
            sb.Append(m.M34);
            sb.Append("\n");
            sb.Append(m.M41);
            sb.Append(", ");
            sb.Append(m.M42);
            sb.Append(", ");
            sb.Append(m.M43);
            sb.Append(", ");
            sb.Append(m.M44);
            sb.Append("\n");

            return sb.ToString();
        }

        public static string ToStrF2(this Matrix4x4 m)
        {
            if (m.IsIdentity)
                return "Identity";

            StringBuilder sb = new StringBuilder();
            sb.Append(m.M11.ToString("F2"));
            sb.Append(", ");
            sb.Append(m.M12.ToString("F2"));
            sb.Append(", ");
            sb.Append(m.M13.ToString("F2"));
            sb.Append(", ");
            sb.Append(m.M14.ToString("F2"));
            sb.Append("\n");
            sb.Append(m.M21.ToString("F2"));
            sb.Append(", ");
            sb.Append(m.M22.ToString("F2"));
            sb.Append(", ");
            sb.Append(m.M23.ToString("F2"));
            sb.Append(", ");
            sb.Append(m.M24.ToString("F2"));
            sb.Append("\n");
            sb.Append(m.M31.ToString("F2"));
            sb.Append(", ");
            sb.Append(m.M32.ToString("F2"));
            sb.Append(", ");
            sb.Append(m.M33.ToString("F2"));
            sb.Append(", ");
            sb.Append(m.M34.ToString("F2"));
            sb.Append("\n");
            sb.Append(m.M41.ToString("F2"));
            sb.Append(", ");
            sb.Append(m.M42.ToString("F2"));
            sb.Append(", ");
            sb.Append(m.M43.ToString("F2"));
            sb.Append(", ");
            sb.Append(m.M44.ToString("F2"));
            sb.Append("\n");

            return sb.ToString();
        }

        public static string ToStrF2(this Vector4 v)
        {
            return $"({v.X.ToStrF2()}, {v.Y.ToStrF2()}, {v.Z.ToStrF2()}, {v.W.ToStrF2()})";
        }
        public static string ToStrInt(this Vector4 v)
        {
            return $"({v.X.ToStrInt()}, {v.Y.ToStrInt()}, {v.Z.ToStrInt()}, {v.W.ToStrInt()})";
        }

        public static string ToStrF2(this Vector3 v)
        {
            return $"({v.X.ToStrF2()}, {v.Y.ToStrF2()}, {v.Z.ToStrF2()})";
        }
        public static string ToStrInt(this Vector3 v)
        {
            return $"({v.X.ToStrInt()}, {v.Y.ToStrInt()}, {v.Z.ToStrInt()})";
        }

        public static string ToStr(this Vector3 v)
        {
            return $"({v.X}, {v.Y}, {v.Z})";
        }

        public static string ToStrF2(this float f)
        {
            return f.ToString("F2");
        }
        public static string ToStrInt(this float f)
        {
            return $"{(int)f}";
        }

        public static string ToStr(this Quaternion q)
        {
            return $"({q.X}, {q.Y}, {q.Z}, {q.W})";
        }

        public static string ToStrF2(this Quaternion q)
        {
            return $"({q.X.ToStrF2()}, {q.Y.ToStrF2()}, {q.Z.ToStrF2()}, {q.W.ToStrF2()})";
        }

        public static string TrimBoneName(this string name)
        {
            if (name.StartsWith("mixamo"))
                return name.Substring(11);
            return name;
        }
    }
}