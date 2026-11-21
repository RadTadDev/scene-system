# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0]

### Added

- Uninstaller: `SceneSystem` takes an uninstaller that runs on each unloaded scene, in order, before unloads start.
- `ISceneManager.OnUninstalledAsync`, called after a scene is uninstalled and before it unloads.
- `SceneStep.LoadedScene`, the handle of a scene once it has loaded.

### Changed

- `SceneSystem` constructor now takes an uninstaller after the installer.

## [0.1.0] - 2026-11-20

### Added

- Initial package structure.
