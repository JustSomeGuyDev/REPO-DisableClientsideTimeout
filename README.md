# Disable Clientside Timeout

A simple mod that removes the client-side Photon `TimeoutDisconnect` checks, potentially preventing some disconnects in games using Photon Networking.

## Installation (Automatic via Mod Manager)

1.  Use a mod manager (like Gale or Thunderstore Mod Manager) to install this mod.
2.  Launch the game. The mod should load automatically.

## Installation (Manual)

1.  Ensure BepInEx is installed for your target game.
2.  Download the latest release of this mod.
3.  Extract the contents and copy `DisableClientsideTimeout.dll` into the game's `BepInEx\plugins` folder.
4.  Launch the game. Check the BepInEx console or log output file (`BepInEx/LogOutput.log`) for messages from "Disable Clientside Timeout" confirming it loaded and patched successfully.

## Important Notes

*   **Game Updates:** Game updates might break this mod if the underlying Photon code changes significantly.
*   **Server-Side Timeouts:** This only disables the *client-side* check. The server can still disconnect you for timeouts or other reasons.
*   **Effectiveness:** This mod attempts to prevent *some* types of disconnects related to the client-side timeout check. It may not solve all connection issues.
