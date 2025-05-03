# Disable Clientside Timeout

A simple mod that removes the client-side Photon `TimeoutDisconnect` checks, potentially preventing some disconnects in games using Photon Networking.

## Installation (Automatic via Mod Manager)

1.  Use a mod manager (like Gale or Thunderstore Mod Manager) to install this mod.
2.  (Recommended) Install [REPOConfig](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/) for easier configuration.
3.  Launch the game. The mod should load automatically.

## Installation (Manual)

1.  Ensure BepInEx is installed for your target game.
2.  (Recommended) Download and install [REPOConfig](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/) by placing `REPOConfig.dll` in `BepInEx\plugins`.
3.  Download the latest release of this mod.
4.  Extract the contents and copy `DisableClientsideTimeout.dll` into the game's `BepInEx\plugins` folder.
5.  Launch the game. Check the BepInEx console or log output file (`BepInEx/LogOutput.log`) for messages from "Disable Clientside Timeout" confirming it loaded and patched successfully (unless disabled via config).

## Configuration

This mod includes a configuration option to enable or disable the patch:

*   **File Location:** `BepInEx/config/JustSomeGuyDev.DisableClientsideTimeout.cfg` (Generated after running the game once with the mod installed).
*   **Option:** `Enable Patch`
    *   **Type:** `Boolean` (true/false)
    *   **Default:** `true`
    *   **Description:** When set to `true`, the mod applies the patch to disable the client-side timeout disconnect. When set to `false`, the mod loads but does not apply the patch, leaving the default timeout behavior active. This is useful if you find the patch causes severe lag or desynchronization on very unstable internet connections... if your internet connection dips down below 3mbs..

You can edit this file directly with a text editor or use [REPOConfig](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/) if installed for in-game configuration.

**Important:** A game restart is required after changing this setting for it to take effect.

## Important Notes

*   **Game Updates:** Game updates might break this mod if the underlying Photon code changes significantly.
*   **Server-Side Timeouts:** This only disables the *client-side* check. The server can still disconnect you for timeouts or other reasons.
*   **Effectiveness:** This mod attempts to prevent *some* types of disconnects related to the client-side timeout check. It may not solve all connection issues. Disabling the patch via the config may be preferable on very poor connections.
