# StarConfig

StarConfig is a fresh, native Windows-only Star Citizen control mapper. The current repository is the project starting line. Earlier repository history is intentionally irrelevant.

## What the first running build does

- Native Windows WPF desktop application on .NET 8.
- Detects keyboard, mouse, and connected Windows joystick-class devices.
- Finds common Star Citizen LIVE, PTU, EPTU, and TECH-PREVIEW mapping folders.
- Loads exported Star Citizen XML control profiles.
- Lists and searches actions by context, internal action name, and input.
- Explains actions, including the important difference between Master Mode and Operator Mode controls.
- Edits the exact action inside the exact action map, avoiding accidental updates to same-named actions elsewhere.
- Recognizes several shared player intents such as forward/backward movement, pitch, yaw, roll, and primary fire.
- Prompts the user to choose which game states should share a control instead of silently spreading bindings.
- Preserves differences between Cycle, Toggle, Hold, and Direct Select actions.
- Creates one timestamped backup before each batch of XML changes.
- Uses temporary-file replacement when saving.
- Opens the RSI Launcher from common Windows locations.

## Product rule

A physical control maps to a player intent. StarConfig may suggest compatible actions across Flight, Vehicle, On Foot, EVA, or Turret contexts, but the user chooses exactly where the control applies.

## Build on Windows

Requirements:

- Windows 10 or Windows 11, 64-bit
- Visual Studio 2022 with the .NET desktop development workload, or the .NET 8 SDK

```powershell
dotnet restore
dotnet build StarConfig.sln -c Release
dotnet run --project .\src\StarConfig\StarConfig.csproj
```

## Publish a standalone Windows folder

```powershell
dotnet publish .\src\StarConfig\StarConfig.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output:

`src\StarConfig\bin\Release\net8.0-windows\win-x64\publish\`

## Current limitations

- Hardware detection currently uses Windows WinMM joystick enumeration. Raw Input/HID support is the next technical upgrade for richer device identity and live button listening.
- Action descriptions and semantic relationships are seeded locally and will grow into a versioned Star Citizen knowledge database.
- Device artwork is currently represented by the supplied design anchor. Exact interactive hardware diagrams come after stable live input identification.

## Design anchor

The supplied interface mockup is stored at `src/StarConfig/Assets/design-anchor.png` and defines the intended visual direction without forcing the first build to become a bloated cockpit simulator.
