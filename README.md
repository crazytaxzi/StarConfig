# Starbind

Starbind is a native Windows control-profile editor for Star Citizen. It is built around physical controls rather than raw XML action names.

## What it does

- Opens exported `layout_*_exported.xml` profiles.
- Reads exact keyboard, gamepad, joystick, throttle, and pedal product names from the profile.
- Selects or listens for a physical key, button, hat, or axis.
- Shows every action assigned to that control across Flight, Vehicle, On Foot, EVA, Turret, Mining, Salvage, and General contexts.
- Suggests equivalent actions across states while keeping Axis, Toggle, Cycle, Hold, and Direct behaviors distinct.
- Preserves multiple rebinds and attributes such as `multiTap`.
- Detects duplicate or unrelated assignments.
- Reads and writes device deadzones.
- Creates a timestamped backup and validates the XML before replacing the profile.
- Installs as a self-contained Windows application with no separate .NET runtime required.

## Distribution

Public users download `Starbind-Setup.exe` from GitHub Releases. The source tree and build files are not required after installation.

## Safety

Starbind edits exported Star Citizen control profiles. Every write creates a copy in a `Starbind Backups` folder beside the profile before the original file is replaced.
