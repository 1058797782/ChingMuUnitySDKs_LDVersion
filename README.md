# ChingMu Unity SDK - LD Version

This repository keeps the original ChingMu repository history in archival branches and provides an installable Unity Package Manager layout on `main`.

## Install

In Unity Package Manager, select **Add package from git URL** and enter:

```text
https://github.com/1058797782/ChingMuUnitySDKs_LDVersion.git?path=/Packages/com.lastdream.chingmu#v3.0.1-ld.1
```

Or add this entry to the consuming project's `Packages/manifest.json`:

```json
"com.lastdream.chingmu": "https://github.com/1058797782/ChingMuUnitySDKs_LDVersion.git?path=/Packages/com.lastdream.chingmu#v3.0.1-ld.1"
```

The package is installed under `Packages/com.lastdream.chingmu` and does not copy files into `Assets` unless an optional sample is imported.

## Branches

- `main`: installable package and compatibility fixes.
- `legacy/unitypackage-import`: untouched Unity project immediately after importing the official v3.0.1 unitypackage.
- `legacy/original`: original repository history before the Unity project import.

See the package [README](Packages/com.lastdream.chingmu/README.md) for file provenance, configuration, and known limitations.
