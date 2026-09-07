# Contributing

Thanks for taking a look. This is a small utility and the goal is to keep it that way: no dependencies, no network, no elevation.

## Building

```
dotnet build
```

Requires the .NET 10 SDK on Windows. There are four source files:

| File | Responsibility |
|------|----------------|
| `UsageMonitor.cs` | Reads the ConsentStore registry keys and decides what counts as in use |
| `Program.cs` | Overlay window, pin definitions, tray menu, refresh loop |
| `TrayPins.cs` | Per-resource notification area icons |
| `Settings.cs` | Preferences file |

## Testing a change

There are no automated tests — the app's whole job is reading live OS state, so verification is manual. Before opening a pull request, check that:

1. The app starts and the four pins appear in the corner, dim.
2. **Test mode** (tray menu) lights all four pins and shows all four taskbar icons.
3. Opening a real camera or microphone app lights the matching pin, and the tooltip names that app.
4. Closing the app dims the pin again within a second or two.
5. `%APPDATA%\ResourcePins\log.txt` has no new errors after a few minutes running.

Say in the pull request which Windows version you tested on. Windows 10 and Windows 11 populate these registry keys somewhat differently, and that's a common source of surprises.

## Scope

Some things are deliberate and unlikely to be merged:

- Network calls, telemetry, or crash reporting of any kind.
- Anything that requires running as administrator to work at all.
- Extra runtime dependencies or UI frameworks.
- Claiming detection the data source can't back up. If a resource can't be detected reliably, the honest answer is to document the gap rather than guess. See the detection section in the README.

Adding a new pin for a capability that Windows already tracks in the ConsentStore is straightforward and welcome: add the capability key to `UsageMonitor.Capabilities` and a `PinDef` entry in `Program.cs`.

## Style

- Code, comments and commit messages in English.
- Follow the surrounding style. Comments explain why, not what.
- Keep pull requests focused on one thing — it makes review much faster.
