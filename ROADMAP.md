# Roadmap

## v1.1 (current)
- [x] Overlay pins in the top-right corner
- [x] One taskbar icon per resource, visible only during use
- [x] Camera, microphone, location and screen capture (WGC) via ConsentStore
- [x] Lit/dim states — a pin lights up only on real active use
- [x] Tooltip naming the app using the resource
- [x] Orphaned registry entries filtered out (requires a live process)
- [x] Tray icon, menu toggles, diagnostic log
- [x] Self-contained Windows installer with optional autostart

## v2 (planned)
- [ ] **Block a resource per app** from the pin. Open question: revoking in `HKLM` requires elevation, so the non-elevated path will likely be a shortcut into Windows privacy settings.
- [ ] **Usage history** — a timeline of when each app used each resource.
- [ ] **Notification** the first time an app uses the camera or microphone.
- [ ] **Settings UI** — which resources to watch, screen corner, pin size and opacity.
- [ ] **Trusted apps list** so an app you use all day stops lighting the pin.

## Worth investigating
- [ ] **Detecting DXGI Desktop Duplication capture** (OBS and similar), currently invisible to the ConsentStore. Possible routes: ETW, or a process heuristic. Only worth doing without elevation and without false positives — "OBS is open" is not "OBS is recording".
- [ ] Multi-monitor support (pin on the active screen, or one per screen).
- [ ] More pins: sensitive folder access (`broadFileSystemAccess`), USB, HID.
