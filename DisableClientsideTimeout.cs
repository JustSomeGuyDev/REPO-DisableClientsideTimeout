using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System;
using BepInEx.Configuration;

namespace DisableClientsideTimeout
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private Harmony harmony;
        private static ConfigEntry<bool> enablePatch;

        private void Awake()
        {
            Log = Logger;

            // Bind the configuration setting with the restart note in its name
            enablePatch = Config.Bind(
                "General", // Section
                "Enable Patch (Restart Required)", // Key
                true, // Default value
                "Enable this to disable the Photon TimeoutDisconnect check. Disable if you experience severe lag/desync on very poor connections." // Description
            );

            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            if (enablePatch.Value)
            {
                Log.LogInfo("Patch is enabled via config. Applying Harmony patches...");
                harmony = new Harmony(PluginInfo.PLUGIN_GUID);

                try
                {
                    var enetPeerType = AccessTools.TypeByName("ExitGames.Client.Photon.EnetPeer");
                    var tPeerType = AccessTools.TypeByName("ExitGames.Client.Photon.TPeer");

                    if (enetPeerType == null)
                    {
                        Log.LogError("Failed to find EnetPeer type!");
                        return;
                    }
                    if (tPeerType == null)
                    {
                        Log.LogError("Failed to find TPeer type!");
                        return;
                    }

                    var sendOutgoingCommandsMethod = AccessTools.Method(enetPeerType, "SendOutgoingCommands");
                    var dispatchIncomingCommandsMethod = AccessTools.Method(tPeerType, "DispatchIncomingCommands");

                    var transpilerEnetPeerMethod = AccessTools.Method(typeof(TimeoutDisconnectPatch), nameof(TimeoutDisconnectPatch.TranspilerEnetPeer));
                    var transpilerTPeerMethod = AccessTools.Method(typeof(TimeoutDisconnectPatch), nameof(TimeoutDisconnectPatch.TranspilerTPeer));

                    if (sendOutgoingCommandsMethod == null)
                        Log.LogError("Failed to find EnetPeer.SendOutgoingCommands method!");
                    else
                        harmony.Patch(sendOutgoingCommandsMethod, transpiler: new HarmonyMethod(transpilerEnetPeerMethod));

                    if (dispatchIncomingCommandsMethod == null)
                        Log.LogError("Failed to find TPeer.DispatchIncomingCommands method!");
                    else
                        harmony.Patch(dispatchIncomingCommandsMethod, transpiler: new HarmonyMethod(transpilerTPeerMethod));

                    Log.LogInfo("Successfully applied timeout patches.");
                }
                catch (Exception ex)
                {
                    Log.LogError($"Error applying patches: {ex}");
                }
            }
            else
            {
                Log.LogInfo("Patch is disabled via config. Skipping Harmony patches.");
            }
        }
    }

    internal static class TimeoutDisconnectPatch
    {
        private static readonly Type PeerBaseType = AccessTools.TypeByName("ExitGames.Client.Photon.PeerBase");
        private static readonly MethodInfo debugOut = AccessTools.PropertyGetter(PeerBaseType, "debugOut");
        private static readonly MethodInfo EnqueueStatusCallback = AccessTools.Method(PeerBaseType, "EnqueueStatusCallback");

        internal static IEnumerable<CodeInstruction> TranspilerEnetPeer(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> instrs = new List<CodeInstruction>(instructions);
            bool patched = false;

            var enqueueStatusCallbackRef = AccessTools.Method(AccessTools.TypeByName("ExitGames.Client.Photon.PeerBase"), "EnqueueStatusCallback");
            if (enqueueStatusCallbackRef == null)
            {
                Plugin.Log?.LogError("TranspilerEnetPeer: Could not find PeerBase.EnqueueStatusCallback!");
                return instructions;
            }

            for (int i = 0; i < instrs.Count - 1; i++)
            {
                if (instrs[i].opcode == OpCodes.Ldc_I4 && instrs[i].operand is int val && val == (int)ExitGames.Client.Photon.StatusCode.TimeoutDisconnect &&
                    instrs[i + 1].opcode == OpCodes.Call && instrs[i + 1].operand is MethodInfo method && method == enqueueStatusCallbackRef)
                {
                    Plugin.Log?.LogInfo("Patching EnetPeer.SendOutgoingCommands: Found TimeoutDisconnect call. Replacing with NOPs.");
                    instrs[i].opcode = OpCodes.Nop;
                    instrs[i].operand = null;
                    instrs[i + 1].opcode = OpCodes.Nop;
                    instrs[i + 1].operand = null;
                    patched = true;
                    break;
                }
            }
            if (!patched) Plugin.Log?.LogWarning("Could not find patch location in EnetPeer.SendOutgoingCommands");
            return instrs;
        }

        internal static IEnumerable<CodeInstruction> TranspilerTPeer(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> instrs = new List<CodeInstruction>(instructions);
            bool patched = false;

            var enqueueStatusCallbackRef = AccessTools.Method(AccessTools.TypeByName("ExitGames.Client.Photon.PeerBase"), "EnqueueStatusCallback");
            if (enqueueStatusCallbackRef == null)
            {
                Plugin.Log?.LogError("TranspilerTPeer: Could not find PeerBase.EnqueueStatusCallback!");
                return instructions;
            }

            for (int i = 0; i < instrs.Count - 1; i++)
            {
                if (instrs[i].opcode == OpCodes.Ldc_I4 && instrs[i].operand is int val && val == (int)ExitGames.Client.Photon.StatusCode.TimeoutDisconnect &&
                    instrs[i + 1].opcode == OpCodes.Call && instrs[i + 1].operand is MethodInfo method && method == enqueueStatusCallbackRef)
                {
                    Plugin.Log?.LogInfo("Patching TPeer.DispatchIncomingCommands: Found TimeoutDisconnect call. Replacing with NOPs.");
                    instrs[i].opcode = OpCodes.Nop;
                    instrs[i].operand = null;
                    instrs[i + 1].opcode = OpCodes.Nop;
                    instrs[i + 1].operand = null;
                    patched = true;
                    break;
                }
            }
            if (!patched) Plugin.Log?.LogWarning("Could not find patch location in TPeer.DispatchIncomingCommands");
            return instrs;
        }
    }
}
