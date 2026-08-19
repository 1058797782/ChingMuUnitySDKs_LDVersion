# Official content and demo guide

Import **Official Content** from Package Manager only when the examples or bundled art are needed. The runtime package works without importing this sample.

## Scene inventory

| Scene | Main component | Purpose |
|---|---|---|
| `Example/Tracker.unity` | `BodyTracker` | Receive a rigid body by numeric ID |
| `Example/IfUseGetTrackerByName/IfUseGetTrackerByName.unity` | `BodyTracker` with `isUsingTrackerName` | Receive a rigid body using its name-based address |
| `Example/FullBodyCapture.unity` | `HumanTracker` | Drive the official character with non-retargeted human data |
| `Example/Tracker&BodyCapture.unity` | `HumanRetargetForVrpn` plus `BodyTracker` | Show retargeted character and rigid-body tracking together |
| `Example/RetargetCharacter/ForVRPN.unity` | `HumanRetargetForVrpn` | Drive a hierarchy using VRPN retarget data |
| `Example/RetargetCharacter/ForLiveStream.unity` | `HumanRetargetForLiveStream` | Drive a hierarchy using LiveStream data |
| `Example/SixIK.unity` | `SixIKCaptureWithCMBody` | Drive humanoid body, head, hands, and feet from six rigid bodies |

Every functional scene also contains the `CmTrackThreadManager` prefab. It owns the native client lifetime and must initialize before consumers.

The two tracker scenes originally carried disabled components from an external UVRPN package that was not included in the official release. Those missing components have been removed and the package-native `BodyTracker` restored. Numeric and name-based behavior is selected through the serialized `BodyTracker` fields.

## Common setup

1. Import the sample.
2. Open the chosen scene.
3. Select `CmTrackThreadManager`.
4. Choose `Vrpn` or `LiveStream` according to the scene.
5. Set `ServerIP` to the actual server address and set the required port.
6. Start the matching ChingMu server application.
7. Enter Play Mode.

Example VRPN addresses:

```text
MCServer@127.0.0.1
MCAvatar@127.0.0.1
```

Do not treat the example port as universal. Use the port shown by the matching server tool and mode.

## Rigid body scenes

`BodyTracker.bodyId` selects a numeric channel. With `isUsingTrackerName` enabled, `bodyName` is combined with the host extracted from the manager address. The object is updated in `FixedUpdate`.

If configuration is enabled, `BodyIDIndex` selects an entry from `Bodies` in `StreamingAssets/Config.json`.

## Full-body scene

The character prefab serializes all `ChingMU_HumanBones` references. Keep the model in a valid humanoid T-pose before starting. `HumanID` selects the streamed human. Missing optional finger transforms are tolerated.

## Retarget scenes

For VRPN, `ObjectID_InCMTrackSence` is the object's order in the server retarget list, not a raw rigid-body ID. Hierarchy names must match Unity transform names.

For LiveStream, `humanID` selects the hierarchy description and streamed pose. The hierarchy callback must arrive before pose application begins.

## Six-point scene

The configuration requires six entries in this exact order:

| `Bodies` index | Target |
|---:|---|
| 0 | Head |
| 1 | Hip |
| 2 | Left hand |
| 3 | Right hand |
| 4 | Left foot |
| 5 | Right foot |

The hip height is used to scale the character after a plausible tracked value is available.

## LiveStream visualizers

`SyncBodyForLiveStream` builds simple body and marker geometry from creation callbacks. `SyncHumanForLiveStream` builds a joint hierarchy and markers. The official release did not include its referenced `Point` and `FingerPoint` resources, so the maintained visualizer falls back to procedural spheres.

These visualizers are diagnostic helpers and create scene geometry. They are not needed when a project already has its own body or avatar presentation.

## Validation without a tracking signal

The following checks do not require hardware or server data:

- runtime and test assemblies compile in Unity 2022.3.60f1;
- Edit Mode tests validate address normalization, ID lookup, axis conversion, and repeated raw-frame reads;
- all maintained scenes resolve package-native script GUIDs;
- native DLL architecture and importer platform settings match Windows x86_64;
- callbacks register through static AOT entry points and mutate scenes only through the main-thread queue;
- configuration missing-file, empty-list, and invalid-index paths do not throw.

Hardware validation is still required for connection establishment, native frame timing, actual sensor detection, skeleton naming, callback ordering from each server mode, and long-session native memory behavior.
