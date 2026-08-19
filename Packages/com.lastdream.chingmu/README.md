# ChingMu Unity SDK - LD Version

This package converts the official ChingMu Unity SDK v3.0.1 unitypackage into a Unity Package Manager layout. Installation keeps runtime files under `Packages/com.lastdream.chingmu` instead of copying the SDK into the consuming project's `Assets` directory.

## Requirements

- Unity 2022.3 or newer for this maintained package baseline.
- Windows for the bundled native libraries.
- A running compatible ChingMu server when entering Play Mode.

## Package layout and provenance

| Path | Provenance | Purpose |
|---|---|---|
| `Runtime/ChingMu/Core` | Official v3.0.1 release, with the compatibility changes listed below | Runtime C# bridge and components |
| `Runtime/ChingMu/Plugins` | Official v3.0.1 release | Native x86 and x86_64 libraries |
| `Samples~/OfficialContent` | Official v3.0.1 release | Optional scenes, prefabs, fonts, models, materials, and textures |
| `Samples~/Configuration/Config.json` | Official v3.0.1 release | Optional configuration template |
| `Documentation~/Official` | Official v3.0.1 release | Original vendor manual |
| `package.json`, assembly definition, and package documentation | LD package layer | Package Manager integration and maintenance notes |

The Unity template files outside `Packages/com.lastdream.chingmu` belong to the repository's development project and are not installed with this package.

## Compatibility changes from the official v3.0.1 source

- Removed the unused `Mono.Cecil` import from `HumanTracker.cs`, which prevented the runtime assembly from compiling.
- Removed the unused `Unity.VisualScripting` import from `VRPNSetup.cs`, so the runtime package does not require Visual Scripting.
- Added an assembly definition so the SDK has an explicit package boundary.

No public class names, serialized fields, native entry points, or existing asset GUIDs were changed.

## Configuration

The package can use Inspector values without a configuration file. If `isUsingConfig` is enabled, copy the configuration template to this exact location in the consuming project:

```text
Assets/StreamingAssets/Config.json
```

Unity Package Manager does not install StreamingAssets directly from a package, so this copy remains an explicit project-level choice.

## Optional content

Import **Official Content** from the package's Samples section only when the original examples or art assets are required. Some official XR prefabs reference XR Interaction Toolkit sample assets that are not included in the v3.0.1 unitypackage.

The official `SyncHumanForLiveStream` implementation also looks for `Resources/Point` and `Resources/FingerPoint`, but those prefabs are not present in the v3.0.1 release payload.

## Install from Git

```text
https://github.com/1058797782/ChingMuUnitySDKs_LDVersion.git?path=/Packages/com.lastdream.chingmu#v3.0.1-ld.1
```
