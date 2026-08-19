# Changelog

## 3.0.1-ld.2

- Replaced repeated whole-frame LiveStream marshalling with bounded direct reads of the requested body or human.
- Reused VRPN human, retargeting, and device-fusion buffers instead of allocating arrays on every sample.
- Fixed LiveStream human lookup to use the matched human ID rather than treating an ID as an array index.
- Made native callbacks AOT-safe, retained their delegates, routed scene changes to the Unity main thread, and added cleanup.
- Removed mutable per-character static retargeting state and guarded the single native tracking session.
- Fixed configuration load order, file handling, address normalization, and six-point body list validation.
- Added bounds checks and null handling across body, human, retargeting, hierarchy, and visualization components.
- Disabled high-volume marker diagnostics by default and removed their duplicate native reads and per-tick file flushes.
- Added procedural visualization fallbacks for the missing `Point` and `FingerPoint` resources.
- Corrected native plug-in import settings to Windows x86_64; the file shipped in the official `x86` folder is also x86_64 and is now disabled.
- Removed missing external UVRPN components from two optional scenes, restored their package-native `BodyTracker` components, and corrected the LiveStream retarget scene provider mode.
- Added Edit Mode coverage for address handling, ID-based frame lookup, coordinate conversion, and repeated frame reads.
- Added architecture, sample, and hardware-validation documentation.
- Declared the UGUI and TextMesh Pro dependencies used by the optional official UI prefabs.

## 3.0.1-ld.1

- Repackaged the official ChingMu Unity SDK v3.0.1 as an installable Unity Package Manager package.
- Moved runtime C# and native libraries into an explicit runtime assembly.
- Moved official examples and art into optional samples.
- Moved the original manual into package documentation.
- Fixed the unused `Mono.Cecil` import that prevented runtime compilation.
- Removed the unused Visual Scripting import from `VRPNSetup.cs`.
- Preserved existing official asset GUIDs during relocation.
