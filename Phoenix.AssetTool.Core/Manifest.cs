using NativeFileDialogNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Phoenix.AssetTool.Core
{
    public static class Manifest
    {
        public static bool Loaded => AssetManifest != null;
        public static string Name { get; private set; } = default!;
        private static AssetManifest AssetManifest { get; set; } = default!;
        public static List<AssetEntry> Assets => AssetManifest.Assets;
        public static string BaseDirectory => AssetManifest.BaseDirectory;
        public static string AbsolutePath { get; private set; } = default!;

        public const string DefaultName = "asset-manifest.json";

        static List<Action> _onManifestChange = new List<Action>();

        public static void RegisterNotifyAction(Action action)
        {
            _onManifestChange.Add(action);
        }
        public static void Clear(string path)
        {
            if(Load(path))
            {
                Assets.Clear();
                Save();
            }
        }
        public static void Create(string path, string name = DefaultName)
        {
            //var relative = Path.GetRelativePath(Environment.CurrentDirectory, path).Replace("\\", "/");
            var relative = path;
            var am = new AssetManifest()
            {
                BaseDirectory = relative
            };
            AssetManifest = am;
            _onManifestChange.ForEach(a => a.Invoke());

            AbsolutePath = Path.Combine(path, name).Replace("\\","/");
            Name = Path.GetFileName(AbsolutePath);
            Save();
        }
        public static void Save()
        {
            if (!Loaded)
                return;
            File.WriteAllText(AbsolutePath, JsonConvert.SerializeObject(AssetManifest, Formatting.Indented));
        }
        

        public static bool Load(string path)
        {
            var relative = Path.GetRelativePath(Environment.CurrentDirectory, path).Replace("\\", "/");

            //var settings = new JsonSerializerSettings { Formatting = Formatting.Indented, };
            AssetManifest = JsonConvert.DeserializeObject<AssetManifest>(File.ReadAllText(path))!;
            if (AssetManifest == null)
                return false;
            _onManifestChange.ForEach(a => a.Invoke());

            AbsolutePath = path.Replace('\\', '/');
            Name = Path.GetFileName(AbsolutePath);

            return true;
        }
        public static bool FilePicker()
        {
            using var dlg = new NativeFileDialog()
            .SelectFile()
            .AddFilter("manifest files", "json");

            var result = dlg.Open(out string[]? files, defaultPath: Environment.CurrentDirectory);
            if (result == DialogResult.Okay && files != null && files.Length > 0)
            {
                var path = files[0];

                return Load(path);
            }
            return false;
        }
        
        public static (bool selected, bool existing, string path) PickFolderToCreate()
        {
            using var dlg = new NativeFileDialog()
            .SelectFolder();

            var result = dlg.Open(out string[]? folders, defaultPath: Environment.CurrentDirectory);
            if (result == DialogResult.Okay && folders != null && folders.Length > 0)
            {
                var dir = folders[0].Replace('\\', '/');
                var path = Path.Combine(dir, DefaultName).Replace('\\', '/');

                if(File.Exists(path))
                    return (true, true, path);

                Create(dir);

                return (true, false, "");

            }
            return (false, false, "");
        }
    }
}
