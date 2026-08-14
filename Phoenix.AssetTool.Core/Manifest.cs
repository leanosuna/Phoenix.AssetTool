using NativeFileDialogNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Xml.Linq;

namespace Phoenix.AssetTool.Core
{
    public static class Manifest
    {
        private static AssetManifest AssetManifest { get; set; } = default!;
        public static bool Loaded => AssetManifest != null;
        public static string Name { get; private set; } = default!;
        public static List<AssetEntry> Assets => AssetManifest.Assets;
        public static string BaseDirectory { get; private set; } = default!;
        public static string Namespace
        { 
            get => AssetManifest.Namespace; 
            set => AssetManifest.Namespace = value; 
        }

        public static bool DarkTheme
        {
            get => AssetManifest.DarkTheme;
            set => AssetManifest.DarkTheme = value;
        }

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
        public static bool CreateAbsolute(string absolute, bool replaceOnFound = false)
        {
            if (replaceOnFound && File.Exists(absolute))
                File.Delete(absolute);

            var am = new AssetManifest();
            var dir = Path.GetDirectoryName(absolute);
            var name = Path.GetFileName(absolute);
            if (dir is null)
            {
                Console.Error.WriteLine("dir cant be null");
                return false;
            }

            Directory.CreateDirectory(dir);

            AssetManifest = am;

            BaseDirectory = dir.Replace("\\", "/");
            AbsolutePath = absolute.Replace("\\", "/");
            Name = name;

            FileTools.ResetDirectoryToggles();
            _onManifestChange.ForEach(a => a.Invoke());
            Save();
            return true;
        }

        public static bool Create(string dir, string name = DefaultName)
        {
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(name))
                return false;

            var am = new AssetManifest();
            
            AssetManifest = am;

            BaseDirectory = dir.Replace("\\", "/");
            AbsolutePath = Path.Combine(dir, name).Replace("\\","/");
            Name = Path.GetFileName(AbsolutePath);

            FileTools.ResetDirectoryToggles();
            _onManifestChange.ForEach(a => a.Invoke());
            Save();

            return true;
        }
        public static void Save()
        {
            if (!Loaded)
                return;

            JsonIOTools.Save(AbsolutePath, AssetManifest);
        }
        

        public static bool Load(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (!JsonIOTools.Load(path, out AssetManifest manifest))
                return false;

            AssetManifest = manifest;
            BaseDirectory = Path.GetDirectoryName(path)!;
            AbsolutePath = path.Replace('\\', '/');
            Name = Path.GetFileName(AbsolutePath);

            FileTools.ResetDirectoryToggles();
            _onManifestChange.ForEach(a => a.Invoke());

            return true;
        }

        public static bool TryLoad(string path, out string? error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                error = "manifest path is empty.";
                return false;
            }

            if (!File.Exists(path))
            {
                error = $"manifest file not found at [{path}]";
                return false;
            }

            if (!Load(path))
            {
                error = $"manifest failed to load: {path}";
                return false;
            }

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
