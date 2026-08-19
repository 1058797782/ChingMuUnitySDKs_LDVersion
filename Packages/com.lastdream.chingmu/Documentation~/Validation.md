# Validation record

Date: 2026-08-20

Environment:

- Unity 2022.3.60f1;
- Windows 10 x86_64;
- package runtime assembly `LastDream.ChingMu`;
- no ChingMu tracking signal or server session available.

## Completed checks

| Check | Result |
|---|---|
| Standalone C# runtime assembly compilation | 0 errors, 0 warnings |
| Isolated Unity batch import and runtime assembly compilation | success, return code 0 |
| Address and configuration test cases | 7 passed |
| Raw LiveStream frame reader test cases | 5 passed |
| Callback registry test cases | 1 passed |
| `git diff --check` | clean |
| Missing UVRPN script references in maintained tracker scenes | none |
| Whole-frame `PtrToStructure` calls in runtime sampling | none |
| Windows native binary architecture inspection | all shipped DLL files are x86_64 |

`Marshal.SizeOf(CMPluginAPI.tFrame)` is 1,383,939 bytes for the shipped ABI. This is only the packed native frame size; whole-frame marshalling additionally constructs the nested managed arrays declared by its 100 humans, 1000 bodies, 5000 markers, buttons, and analog devices.

After warm-up, the isolated allocation harness measured 0 managed bytes for 100 repeated body reads through `ChingMuLiveFrameReader`. Thirteen package test cases passed. The harness and package tests use a synthetic unmanaged frame and do not load or call the native DLLs.

## Hardware checks still required

The following cannot be claimed without the matching server applications and an active tracking signal:

- VRPN and LiveStream connection establishment on the target network;
- numeric and name-based rigid-body detection;
- non-retargeted 63-bone pose correctness;
- MCServer and MCAvatar retarget hierarchy sensor ranges and transform-name matching;
- create/delete callback ordering under live scene changes;
- six-point body ordering against an actual performer;
- long-running native heap, socket, and worker-thread behavior;
- Windows Player Mono and IL2CPP builds on the deployment machine;
- required Visual C++ 2010 runtime availability for `LiveStream.dll`.

The package is therefore an optimization candidate until this list is completed. The stable import/package baselines remain available through the legacy branches and `v3.0.1-ld.1` tag.
