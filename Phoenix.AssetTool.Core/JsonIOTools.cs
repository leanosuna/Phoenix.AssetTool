using Phoenix.AssetTool.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Phoenix.AssetTool.Core
{
    public static class JsonIOTools
    {
        public static bool Load<T>(string path, out T classFile)
        {
            if (!File.Exists(path))
            {
                classFile = default!;
                return false;
            }
            classFile = JsonSerializer.Deserialize<T>(
                File.ReadAllText(path)
            )!;
            return true;
        }

        public static void Save<T>(string path, T classFile)
        {
            File.WriteAllText(
                path,
                JsonSerializer.Serialize(classFile, new JsonSerializerOptions
                {
                    WriteIndented = true
                })
            );
        }

        
    }

}
