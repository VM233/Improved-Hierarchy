# Improved Hierarchy

Unity editor extension for improving the Hierarchy window with color presets, component icons,
and clearer main object icons.

## Features

- Color Hierarchy rows by name prefixes with configurable text color, background color,
  alignment, font style, automatic upper-case display, and an enable switch.
- Show a compact component icon list on the right side of each Hierarchy row.
- Replace the left main GameObject icon when a useful component icon is available.
- Use the Transform icon as the main icon for GameObjects without scripts.
- Use common Unity built-in icons for main object icons, including Camera, Light, Audio,
  Canvas, Event System, Standalone Input Module, and Input System UI Input Module.
- Avoid duplicating the component already used as the left main icon in the right component
  icon list.
- Support Unity 6 New Hierarchy UI Toolkit windows and the older IMGUI Hierarchy callback path.
- Configure all options from Unity's Preferences window.

## Installation

Add the package through Unity Package Manager at an immutable published Git revision:

```text
https://github.com/VM233/Improved-Hierarchy.git#<full-commit-sha>
```

Or add it to `Packages/manifest.json`:

```json
"com.vm233.improved-hierarchy": "https://github.com/VM233/Improved-Hierarchy.git#<full-commit-sha>"
```

## Settings

Open `Preferences > Improved Hierarchy` to configure color presets, component icon display,
and main icon rules.

Hierarchy discovery and icon cleanup use UI Toolkit queries with result snapshots before tree mutation. They retain the existing row-cache invalidation and rendering behavior. See `Documentation~/hierarchy-query-cost.md` for the measured allocation problem and validation scope.
