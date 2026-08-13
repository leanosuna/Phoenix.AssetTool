# Phoenix.AssetTool CLI

The CLI (`pat`) registers and builds assets for use with Phoenix.Framework.

## Usage

```bash
pat <manifest> <command> [args...]
```

- `<manifest>` is the path to an `asset-manifest.json`, passed as the first positional argument.
- Run from the content directory so relative paths resolve correctly.
- Exit code is `0` on success and `255` (`-1`) when any file, option, or build error occurs.

Commands: `init`, `add`, `upd`, `opt`, `rem`, `list`, `build`, `auto`, `clean`, `gui`.

---

## init - create a manifest

```bash
pat asset-manifest.json init
pat asset-manifest.json init -force          # replace existing manifest (also deletes ContentBin)
```

## add - add assets with options

### Targets

```bash
# one file
pat asset-manifest.json add models/char.fbx

# multiple files
pat asset-manifest.json add models/char.fbx textures/diffuse.png shaders/basic.vert

# a whole directory (recursive)
pat asset-manifest.json add models

# the entire content directory
pat asset-manifest.json add .
```

### Model options

| Option | Values |
| --- | --- |
| `-extract-textures` | `true` / `false` |
| `-flags` | comma-separated `PostProcessSteps` names; `+Name`/`-Name` toggles; `default`; `none` |
| `-animated` | `true` / `false` |
| `-animations` | one or more `.fbx` paths (absolute or relative to the content dir) |
| `-pre-transform` | `true` / `false` |
| `-scale` | float (applies when `-pre-transform` is enabled) |

`-flags` forms:

```bash
# exact set (replaces the whole set)
pat asset-manifest.json add models/char.fbx -flags Triangulate,GenerateNormals,FlipUVs

# toggle relative to the built-in defaults
pat asset-manifest.json add models/char.fbx -flags +JoinIdenticalVertices,-FlipUVs

# restore the built-in defaults
pat asset-manifest.json add models/char.fbx -flags default

# no flags
pat asset-manifest.json add models/char.fbx -flags none
```

Examples:

```bash
pat asset-manifest.json add models/char.fbx -extract-textures true
pat asset-manifest.json add models/char.fbx -animated true -animations models/run.fbx models/jump.fbx
pat asset-manifest.json add models/char.fbx -pre-transform true -scale 0.01
pat asset-manifest.json add models/char.fbx -flags Triangulate,GenerateSmoothNormals -extract-textures true -scale 2
```

### Texture options

| Option | Values |
| --- | --- |
| `-mipmaps` | `true` / `false` |
| `-format` | `RGBA`, `BC1`, `BC3`, `BC5` |
| `-wrap-s` / `-wrap-t` | `Repeat`, `MirroredRepeat`, `ClampToEdge`, `ClampToBorder` |
| `-min` | `Nearest`, `Linear`, `NearestMipmapNearest`, `LinearMipmapNearest`, `NearestMipmapLinear`, `LinearMipmapLinear` |
| `-mag` | `Nearest`, `Linear` |
| `-anisotropy` | float (`0` disables) |

Examples:

```bash
pat asset-manifest.json add textures/diffuse.png -format BC3 -mipmaps true
pat asset-manifest.json add textures/diffuse.png -format BC5 -mipmaps false -anisotropy 8
pat asset-manifest.json add textures/diffuse.png -wrap-s ClampToEdge -wrap-t Repeat -min LinearMipmapLinear -mag Linear
pat asset-manifest.json add textures -format BC1     # applies to every texture in the directory
```

### Re-adding existing files

Re-running `add` on a file that is already tracked **overrides** its stored
options when options are supplied, and reports the file as updated:

```bash
pat asset-manifest.json add models/char.fbx -scale 2     # Updated 'models/char.fbx'
pat asset-manifest.json add models/char.fbx               # Already tracked 'models/char.fbx'
```

Options passed to a directory add are applied to every matching file,
including ones already tracked.

### Errors

Exit code `255` when any of these occur:

```bash
pat asset-manifest.json add missing.file                     # file not found
pat asset-manifest.json add ../outside.fbx                   # outside the Content directory
pat asset-manifest.json add models/char.fbx -flags bogus     # unknown assimp flag
pat asset-manifest.json add textures/diffuse.png -format XYZ # invalid compression format
```

## upd - update options without replacing

`upd` takes the same option flags as `add`, but only changes the options that
are passed - every unspecified option keeps its current value. Only assets
that are already tracked can be updated.

```bash
# change only the scale, keep flags/animations/pre-transform as they are
pat asset-manifest.json upd models/char.fbx -scale 5

# change only the compression format, keep mipmaps/wrap/filters
pat asset-manifest.json upd textures/diffuse.png -format BC5

# toggle a flag relative to the defaults without touching anything else
pat asset-manifest.json upd models/char.fbx -flags +GenerateNormals

# set only the animation list
pat asset-manifest.json upd models/char.fbx -animations models/run.fbx
```

Errors (exit `255`): no option flags passed, file not found, file outside the
Content directory, or the asset is not tracked (use `add` first).

## opt - show current load options

`opt` prints the stored load options for one or more tracked assets as JSON:

```bash
pat asset-manifest.json opt models/char.fbx
pat asset-manifest.json opt models/char.fbx textures/diffuse.png
```

Assets without stored options report that defaults will be used. Errors
(exit `255`): file outside the Content directory, or the asset is not tracked.

## rem - remove assets from the manifest

```bash
pat asset-manifest.json rem models/char.fbx
pat asset-manifest.json rem textures/diffuse.png shaders/basic.vert
```

Errors (exit `255`): file not found, or file outside the Content directory.

## list - show tracked files

```bash
pat asset-manifest.json list              # tracked files only
pat asset-manifest.json list -all         # every file, tracked or not
pat asset-manifest.json list -e .fbx      # filter by extension
```

Tracked files are colored green; built files cyan.

## build - build all tracked assets

```bash
pat asset-manifest.json build              # skip assets that are already built
pat asset-manifest.json build -force       # rebuild everything
```

### OpenGL version

Shader compilation needs an OpenGL context. The version is configured in
`asset-tool-config.json` (created automatically next to the manifest on first
build, default `"default"`):

```json
{ "gl": { "version": "default" } }
```

- `"default"` - start at OpenGL 4.1, fall back to the highest supported if
  below 4.1.
- `"auto"` - use the highest OpenGL version the driver supports.
- `"4.6"` / `"4.1"` / `"3.3"` - use that exact version; falls back to
  `"default"` with a warning if the driver does not support it.

## auto - watch files and rebuild on change

```bash
pat asset-manifest.json auto
```

Runs until a key is pressed. Rebuilds tracked assets when they change; shader
pairs (matching name, different extension) rebuild together.

## clean - delete the ContentBin folder

```bash
pat asset-manifest.json clean
```

## gui - launch the AssetTool GUI

```bash
pat asset-manifest.json gui
```
