# Changelog

## 3.0.1-ld.1

- Repackaged the official ChingMu Unity SDK v3.0.1 as an installable Unity Package Manager package.
- Moved runtime C# and native libraries into an explicit runtime assembly.
- Moved official examples and art into optional samples.
- Moved the original manual into package documentation.
- Fixed the unused `Mono.Cecil` import that prevented runtime compilation.
- Removed the unused Visual Scripting import from `VRPNSetup.cs`.
- Preserved existing official asset GUIDs during relocation.
