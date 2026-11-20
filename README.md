# Scene System

## Installation

In Unity, open **Window > Package Manager**, click **+ > Add package from git URL...** and enter:

```
https://github.com/RadTadDev/scene-system.git?path=/Assets/SceneSystem
```

To pin a release, append a tag: `https://github.com/RadTadDev/scene-system.git?path=/Assets/SceneSystem#v0.1.0`

Or add it to `Packages/manifest.json` directly:

```json
{
  "dependencies": {
    "com.radtaddev.scenesystem": "https://github.com/RadTadDev/scene-system.git?path=/Assets/SceneSystem#v0.1.0"
  }
}
```

Requires Unity 6000.6 or newer.

## Development

This repository is a Unity project. The package itself lives in
[`Assets/SceneSystem`](Assets/SceneSystem) and that folder is the only part consumers install.

1. Open the repository root in Unity 6000.6.10f1 (Unity Hub > Add > Add project from disk).
2. Commit the `Packages/` and `ProjectSettings/` folders Unity generates on first open.
3. Run tests from **Window > General > Test Runner**.

To ship samples, put them in `Assets/SceneSystem/Samples~` (hidden from Unity) and list each one under
`samples` in `package.json`. Import them through the Package Manager to work on them.

## Code style

C# style rules come from [cs-style](https://github.com/RadTadDev/cs-style): `.editorconfig`,
`Directory.Build.props`, the `ci/` tools and `.github/workflows/style.yml`. Every push and pull
request runs a non-blocking style check that reports findings as warnings. To pull the latest
rules, run `upkg style` from the repository and commit the result. Edits to those files are
overwritten on the next update.

To check locally (needs the .NET SDK):

```
dotnet format whitespace ci/StyleCheck/StyleCheck.csproj --verify-no-changes
dotnet format style ci/StyleCheck/StyleCheck.csproj --verify-no-changes
dotnet run --project ci/DocCheck/DocCheck.csproj -- .
```

Drop `--verify-no-changes` to apply fixes (run `style` before `whitespace`). Naming and missing
docs are not auto-fixed.

## Releasing

1. Bump `version` in [`Assets/SceneSystem/package.json`](Assets/SceneSystem/package.json).
2. Move the `Unreleased` notes in [`CHANGELOG.md`](Assets/SceneSystem/CHANGELOG.md) under the new version.
3. Commit, then tag and push: `git tag v<version> && git push --tags`.
