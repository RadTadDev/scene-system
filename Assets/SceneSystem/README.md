# Scene System

`com.radtaddev.scenesystem` &middot; see the repository README for installation and development notes.

Runs scene transitions as transactions. Each transaction uninstalls and unloads scenes first, then loads and installs new ones. Only one transaction runs at a time.

## Usage

```csharp
var system = new SceneSystem(installer, uninstaller, appClosingToken);

var transaction = system.Transition(
    unloads: new[] { currentScene },
    loads: new[] { nextScene },
    loadManager: manager);

transaction.OnProgressChanged += progress => UpdateBar(progress);
transaction.OnFinish += success => Debug.Log(success ? "Done" : "Failed");

bool ok = await transaction.Completion;
```

- `IsBusy` is true while a transaction runs or waits to start. `Transition` throws if the system is busy.
- `IsLoaded(scene)` tells whether a scene is loaded.
- Scenes load in parallel but install in declared order. Scenes uninstall in declared order before any unload starts.
- Each unloaded scene unloads with the manager that loaded it, unless `unloadManager` is given.
- A failed step does not roll back. Scenes that already loaded stay loaded.

## Transaction events

| Event | When it fires |
| --- | --- |
| `OnQueued` | Only when a delay is given. Once, with the delay as a `TimeSpan`, before the delay starts. |
| `OnBegin` | Once, when the transaction starts: after any delay, or right away without one. Before any scene operation. |
| `OnProgressChanged` | After each finished step, with progress from 0 to 1. |
| `OnFinish` | Once, after all steps finish, with `true` on success. |

A handler that throws is logged as an error. The remaining handlers and the transaction still run.

## Layout

| Folder | Contents |
| --- | --- |
| `Runtime/` | Runtime code (`RadTadDev.SceneSystem`) |
| `Editor/` | Editor-only code (`RadTadDev.SceneSystem.Editor`) |
| `Tests/Runtime`, `Tests/Editor` | Play mode and edit mode tests |
| `Documentation~/` | Package documentation (hidden from Unity) |
