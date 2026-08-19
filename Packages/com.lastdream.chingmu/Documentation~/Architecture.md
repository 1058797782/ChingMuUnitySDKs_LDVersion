# ChingMu Unity runtime architecture

This document describes the maintained runtime based on the official ChingMu Unity SDK v3.0.1 payload. It records the native boundary, data flow, component responsibilities, lifetime rules, coordinate conversion, and the compatibility decisions used by this package.

## Runtime layers

```text
Scene components
  BodyTracker / HumanTracker / retargeting / visualization
                         |
                  CMPluginCommonInterface
                    /                 \
               VrpnImpl          LiveStreamImpl
                  |                   |
             CMPluginAPI       ChingMuLiveFrameReader
                  \                   /
                P/Invoke native boundary
                  |                   |
              CMVrpn.dll        LiveStream.dll
                         |
                  ChingMu server tools
```

`CMPluginThreadManager` is the scene-level owner. It creates exactly one provider, starts the matching native client thread in `Awake`, connects in `Start`, exposes a read-compatible static provider reference for existing integrations, and stops only the thread it owns.

The static P/Invoke declarations in `CMPluginAPI` and `CMVrpn` are required by the native ABI. They do not themselves retain frame data. Mutable scene and character state belongs to manager or component instances.

## Provider modes

### VRPN

`VrpnImpl` is a pull provider backed by `CMVrpn.dll`.

- Tracker poses are requested by numeric channel or by a complete tracker address.
- Non-retargeted human data returns a root position plus local segment rotations.
- Retargeted human data returns local segment positions and rotations.
- Buffers are allocated once per provider and reused behind a lock.
- Server address and port are normalized whenever either property changes.

The lower-level public `CMVrpn` API remains available. Its human, retargeting, head-mounted-device, and controller fusion buffers are kept per calling thread and reused.

### LiveStream

`LiveStreamImpl` is backed by `LiveStream.dll`. The native library exposes a pointer to one large packed frame containing fixed-capacity arrays:

| Data | Fixed capacity |
|---|---:|
| Humans | 100 |
| Bodies | 1000 |
| Markers | 5000 |
| Buttons | 50 devices, 256 values each |
| Analogs | 50 devices, 256 values each |

Marshalling that complete structure constructs a large managed object graph because every nested fixed array becomes a managed array. The maintained reader instead:

1. obtains the native frame pointer once per rendered frame;
2. clamps every native count to the declared capacity;
3. scans only compact ID fields until the requested item is found;
4. reads only its position, rotation, detection, and required segment fields;
5. writes into caller-owned arrays.

This keeps steady-state frame reads independent of the unused frame capacity and also fixes the original assumption that a human ID was the same as its array position.

## Coordinates and units

Native positions are millimetres. Unity positions are metres.

```text
Unity position   = (native.x, native.z, native.y) / 1000
Unity quaternion = (native.x, native.z, native.y, -native.w)
```

This conversion is applied consistently to tracker, root, marker, and segment data. Do not add another axis swap in project wrappers unless the target model has an additional authored-space correction.

## Callback lifetime and threading

LiveStream creation/deletion callbacks and VRPN hierarchy callbacks can arrive from a native worker thread. Unity objects must not be created, destroyed, or transformed there.

The maintained path is:

```text
native callback
  -> static AOT callback entry
  -> safe numeric token lookup
  -> thread-safe action queue
  -> FixedUpdate on Unity main thread
  -> scene object mutation
```

The delegate is stored for the entire registration lifetime. LiveStream callbacks are unregistered during destruction. VRPN hierarchy exports in the shipped managed surface do not expose a confirmed unregister signature, so their callback target is a permanent static trampoline while the numeric token is removed; late calls therefore become no-ops instead of reaching a destroyed component.

## Human workflows

### Non-retargeted human

`HumanTracker` maps 63 documented body and finger indices to the serialized `ChingMU_HumanBones` structure. At startup it normalizes arms and legs toward a T-pose and stores world-space joint and parent bases. During sampling it applies:

```text
local rotation = inverse(T-pose parent world rotation)
               * tracked world/component rotation
               * T-pose joint world rotation
```

Missing bones and root parents are allowed. The original serialized bone field names and indices are unchanged.

### Retargeted human

`HumanRetargetForVrpn` receives hierarchy records whose sensor ranges begin at 100 for MCServer or 300 for MCAvatar, with 150 slots per object list index. Names map those slots to transforms in the character hierarchy.

`HumanRetargetForLiveStream` receives an `aHumanInfo` hierarchy description, maps segment names and indices on the main thread, keeps the callback-provided local offsets, and then applies streamed rotations and root data.

## Visualization and diagnostics

`SyncBodyForLiveStream` and `SyncHumanForLiveStream` are runtime visualizers, not required for ordinary tracking. They retain callbacks safely, remove generated colliders, reuse material state, and destroy generated objects. The human visualizer creates procedural joints when the official package's missing `Resources/Point` and `Resources/FingerPoint` prefabs are unavailable.

`LabelMarker`, `LabelMarkerTest`, and continuous `VRPNSetup` logging are disabled by default. They can issue hundreds or thousands of native calls and log allocations when enabled, so they should be used for bounded diagnostics only.

## Native binary boundary

The official v3.0.1 payload contains Windows PE x86_64 binaries only. The `CMVrpn.dll` located in the official `x86` folder has the same x86_64 hash as the copy in `x86_64`; it is not a 32-bit binary. Package importer settings therefore enable the x86_64 libraries only for the Windows Editor and Windows 64-bit Player.

`LiveStream.dll` imports the Visual C++ 2010 runtime (`MSVCP100.dll` and `MSVCR100.dll`). A target machine must provide those dependencies or the library load will fail before managed code can connect.

## Compatibility contract

The following are intentionally preserved:

- public component class names;
- existing `.meta` GUIDs;
- original serialized public field names;
- `CMPluginCommonInterface` method surface;
- read access through `CMPluginThreadManager.CMPlugin` and `IsConnected`;
- native entry-point names and structure layouts;
- T-pose and coordinate-conversion intent;
- optional official scene and prefab GUIDs.

The package does not modify the official DLL binaries.
