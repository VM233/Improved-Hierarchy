# Changelog

All notable changes to this package are documented in this file.

## [1.0.4] - 2026-09-13

### Fixed

- Read Unity's active Editor window registry for retained Hierarchy discovery, removing the loaded-object scan from periodic and scene-change refreshes.
- Verify registry membership across EditorWindow creation and destruction without adding a second membership cache.

## [1.0.3] - 2026-09-13

### Fixed

- Retire the component-reordering test's Undo group before destroying its fixture, preventing Unity Test Runner's later Undo cleanup from recreating the test GameObject in the caller's scene.

## [1.0.2] - 2026-09-13

### Fixed

- Retain unchanged row icons across native hierarchy refreshes, and rebuild from actual object, component, active-state and settings changes.
- Release row caches and scheduled window refreshes when their UI detaches.
- Restore prefix-formatted names after Unity rebinds a row, including names with leading whitespace.
- Use the public Unity 6000.6 hierarchy object API, and report broken internal contracts on older supported versions instead of showing stale cached objects.
- Cover row recycling, component changes, settings invalidation, detach and unchanged-pass allocations with focused tests.

## [1.0.1] - 2026-09-13

### Fixed

- Use UI Toolkit queries for visible-row discovery and generated-icon cleanup, removing one recursive iterator allocation per visited visual node.
- Preserve exact icon classes, root matches, host tint restoration and existing row-cache invalidation.
- Add focused icon cleanup and allocation-growth regression tests.

## [1.0.0] - 2026-07-09

### Added

- Added hierarchy row color presets with configurable text color, background color,
  alignment, font style, automatic upper-case display, and enable switches.
- Added compact component icon rendering on the right side of Hierarchy rows.
- Added main GameObject icon replacement using useful component icons and Unity built-in icons.
- Added support for Unity 6 UI Toolkit Hierarchy windows and the older IMGUI Hierarchy callback path.
- Added Preferences integration for configuring Improved Hierarchy behavior.
