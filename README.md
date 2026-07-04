# Improved Hierarchy

Unity editor extension for improving the Hierarchy window with color presets, component icons,
and clearer main object icons.

## Features

- Color Hierarchy rows by name prefixes with configurable text color, background color,
  alignment, font style, and automatic upper-case display.
- Show a compact component icon list on the right side of each Hierarchy row.
- Replace the left main GameObject icon when a useful component icon is available.
- Keep Unity's default GameObject icon when no useful custom or built-in component icon exists.
- Use common Unity built-in icons for main object icons, including Camera, Light, Audio,
  Canvas, Event System, Standalone Input Module, and Input System UI Input Module.
- Avoid duplicating the component already used as the left main icon in the right component
  icon list.
- Support Unity 6 New Hierarchy UI Toolkit windows and the older IMGUI Hierarchy callback path.
- Configure all options from Unity's Project Settings window.

## Installation

Add the package through Unity Package Manager with this Git URL:

```text
https://github.com/VM233/Hierarchy-Color.git
```

Or add it to `Packages/manifest.json`:

```json
"com.vm233.hierarchy-color": "https://github.com/VM233/Hierarchy-Color.git"
```

## Settings

Open `Project Settings > Improved Hierarchy` to configure color presets, component icon display,
and main icon rules.
