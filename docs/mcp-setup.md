# Phoenix.AssetTool.Mcp - Setup

An MCP (Model Context Protocol) server that exposes the Phoenix Asset Tool as
LLM-callable tools. It wraps `Phoenix.AssetTool.Core` directly, so tool
arguments map 1:1 to the CLI `add` options (see [cli.md](cli.md)).

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) (project targets `net10.0`)

## Install

Install the tool from NuGet (global):

```bash
dotnet tool install -g Phoenix.AssetTool.Mcp --version 1.0.3
```

Verify it starts (the server reads JSON-RPC from stdin, so it will keep
running until stopped):

```bash
pat-mcp    # press Ctrl+C to exit
```

To update:

```bash
dotnet tool update -g Phoenix.AssetTool.Mcp
```

## Register with opencode

Add to your opencode config (`~/.config/opencode/opencode.json` for all
projects, or a project-level `opencode.json`):

```json
{
  "mcp": {
    "assettool": {
      "type": "local",
      "command": ["pat-mcp"],
      "enabled": true
    }
  }
}
```

**Restart opencode** after saving the config - it is loaded once at startup and
is not hot-reloaded. Any OpenChamber session hosting opencode must be restarted
too.

For development without installing the tool, point the command at the project
instead:

```json
"command": ["dotnet", "run", "--project", "/path/to/Phoenix.AssetTool.Mcp", "--no-build"]
```

## Tools

Each tool takes a `manifestPath` argument (absolute path to
`asset-manifest.json`), so one server instance can serve any number of projects.

| Tool | Description |
| --- | --- |
| `assettool_add_assets` | Add files/directories with model & texture options. Re-adding a tracked file overrides its options. |
| `assettool_remove_assets` | Remove files from the manifest. |
| `assettool_list_assets` | List tracked assets with type and build status. |
| `assettool_build_assets` | Build all (or a subset) of the assets into `ContentBin`. |
| `assettool_clean` | Delete the `ContentBin` folder. |
| `assettool_get_asset_options` | Get stored load options (JSON) for an asset. |
| `assettool_set_asset_options` | Set/replace load options for an asset. |
| `assettool_update_asset_options` | Update load options for an asset, keeping any option that is not provided. |

### Example invocations

```
assettool_add_assets(manifestPath="/game/Content/asset-manifest.json",
                     paths=["models/char.fbx", "textures/diffuse.png"],
                     flags="default", scale=2.0, format="BC3")

assettool_list_assets(manifestPath="/game/Content/asset-manifest.json")

assettool_build_assets(manifestPath="/game/Content/asset-manifest.json",
                       rebuild=false)

assettool_get_asset_options(manifestPath="/game/Content/asset-manifest.json",
                            path="models/char.fbx")

assettool_set_asset_options(manifestPath="/game/Content/asset-manifest.json",
                            path="textures/diffuse.png", format="BC5", mipmaps=false)
```

Model options: `extractTextures`, `flags` (comma-separated `PostProcessSteps`
names; `+Name`/`-Name` toggles, `default`, `none`), `animated`, `preTransform`,
`scale`, `animations`.
Texture options: `mipmaps`, `format` (RGBA/BC1/BC3/BC5), `wrapS`, `wrapT`,
`min`, `mag`, `anisotropy`.

## Notes

- **Builds**: shader compilation requires an OpenGL context; the server creates
  a hidden window lazily. On headless machines shaders are reported failed and
  skipped, while model/texture builds still work.
- **OpenGL version**: the server reads `asset-tool-config.json` (auto-created
  next to the manifest with `"default"`) to pick the GL version. See
  [cli.md](cli.md) for the `"default"` / `"auto"` / explicit-version modes.
- The server writes its logs to stderr; the JSON-RPC protocol uses stdout.
- Operations are serialized internally because `Phoenix.AssetTool.Core` keeps
  manifest state as statics.

## Troubleshooting

- Tools not showing up: `opencode mcp list`, confirm `pat-mcp` is on `PATH`,
  then restart opencode.
- Reinstall after changes: `dotnet tool uninstall -g Phoenix.AssetTool.Mcp`, then
  `dotnet tool install -g Phoenix.AssetTool.Mcp --version 1.0.3`.
