# Package notes

This package keeps the runtime library separate from optional official content:

- Install the package for C# and native runtime support.
- Import **Official Content** only when the bundled scenes, prefabs, or art are needed.
- Import **Configuration Template** only when file-based configuration is required, then copy `Config.json` to `Assets/StreamingAssets/Config.json`.

The original vendor manual is available under `Documentation~/Official` in the package source.
