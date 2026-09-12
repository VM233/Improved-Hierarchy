# Changelog

All notable changes to this package are documented in this file.

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
