using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Gui
{
    internal class DirectoryBrowserMeta
    {
        public string Name { get; set; } = default!;
        public string Path { get; set; } = default!;
        public List<FileBrowserMeta> FilesMeta { get; set; } = default!;
        public List<DirectoryBrowserMeta> Children { get; set; } = default!;
        public bool ContainsTracked => FilesMeta.Any(f => f.Tracked) || Children.Any(c => c.ContainsTracked);

    }
}
