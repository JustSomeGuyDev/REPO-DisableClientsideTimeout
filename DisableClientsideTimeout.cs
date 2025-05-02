using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System;

namespace DisableClientsideTimeout
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private Harmony harmony;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            harmony = new Harmony(PluginInfo.PLUGIN_GUID);

            try
            {
                // Manually patch the methods
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

            // harmony.PatchAll(typeof(TimeoutDisconnectPatch)); // We are patching manually now
        }
    }

    // Static class containing the patch logic
    internal static class TimeoutDisconnectPatch
    {
        // Use string signatures for AccessTools targeting internal type/members
        private static readonly Type PeerBaseType = AccessTools.TypeByName("ExitGames.Client.Photon.PeerBase");
        private static readonly MethodInfo debugOut = AccessTools.PropertyGetter(PeerBaseType, "debugOut");
        private static readonly MethodInfo EnqueueStatusCallback = AccessTools.Method(PeerBaseType, "EnqueueStatusCallback");

        // Patches are now applied manually in Plugin.Awake
        // [HarmonyTranspiler]
        // [HarmonyPatch("ExitGames.Client.Photon.EnetPeer", "SendOutgoingCommands")] // Specify full type name as string
        internal static IEnumerable<CodeInstruction> TranspilerEnetPeer(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> instrs = new List<CodeInstruction>(instructions);
            bool patched = false;

            // Find the reference to PeerBase.EnqueueStatusCallback
            var enqueueStatusCallbackRef = AccessTools.Method(AccessTools.TypeByName("ExitGames.Client.Photon.PeerBase"), "EnqueueStatusCallback");
            if (enqueueStatusCallbackRef == null)
            {
                 Plugin.Log?.LogError("TranspilerEnetPeer: Could not find PeerBase.EnqueueStatusCallback!");
                 return instructions; // Return original if we can't find the target
            }

            for (int i = 0; i < instrs.Count - 1; i++) // Iterate up to Count - 1 to check i and i+1
            {
                // Look for the call to EnqueueStatusCallback right after loading StatusCode.TimeoutDisconnect
                if (instrs[i].opcode == OpCodes.Ldc_I4 && instrs[i].operand is int val && val == (int)ExitGames.Client.Photon.StatusCode.TimeoutDisconnect &&
                    instrs[i + 1].opcode == OpCodes.Call && instrs[i + 1].operand is MethodInfo method && method == enqueueStatusCallbackRef) 
                {
                    Plugin.Log?.LogInfo("Patching EnetPeer.SendOutgoingCommands: Found TimeoutDisconnect call. Replacing with NOPs.");
                    // Replace the Ldc_I4 and Call instructions with Nop
                    instrs[i].opcode = OpCodes.Nop;
                    instrs[i].operand = null;
                    instrs[i + 1].opcode = OpCodes.Nop;
                    instrs[i + 1].operand = null;
                    patched = true;
                    break; // Assuming only one place to patch
                }
            }
            if (!patched) Plugin.Log?.LogWarning("Could not find patch location in EnetPeer.SendOutgoingCommands");
            return instrs;
        }

        // Patches are now applied manually in Plugin.Awake
        // [HarmonyTranspiler]
        // [HarmonyPatch("ExitGames.Client.Photon.TPeer", "DispatchIncomingCommands")] // Specify full type name as string
        internal static IEnumerable<CodeInstruction> TranspilerTPeer(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> instrs = new List<CodeInstruction>(instructions);
            bool patched = false;

            // Find the reference to PeerBase.EnqueueStatusCallback
            var enqueueStatusCallbackRef = AccessTools.Method(AccessTools.TypeByName("ExitGames.Client.Photon.PeerBase"), "EnqueueStatusCallback");
            if (enqueueStatusCallbackRef == null)
            {
                 Plugin.Log?.LogError("TranspilerTPeer: Could not find PeerBase.EnqueueStatusCallback!");
                 return instructions;
            }

            for (int i = 0; i < instrs.Count - 1; i++) // Iterate up to Count - 1 to check i and i+1
            {
                 // Look for the call to EnqueueStatusCallback right after loading StatusCode.TimeoutDisconnect
                if (instrs[i].opcode == OpCodes.Ldc_I4 && instrs[i].operand is int val && val == (int)ExitGames.Client.Photon.StatusCode.TimeoutDisconnect &&
                    instrs[i + 1].opcode == OpCodes.Call && instrs[i + 1].operand is MethodInfo method && method == enqueueStatusCallbackRef)
                {
                    Plugin.Log?.LogInfo("Patching TPeer.DispatchIncomingCommands: Found TimeoutDisconnect call. Replacing with NOPs.");
                    // Replace the Ldc_I4 and Call instructions with Nop
                    instrs[i].opcode = OpCodes.Nop;
                    instrs[i].operand = null;
                    instrs[i + 1].opcode = OpCodes.Nop;
                    instrs[i + 1].operand = null;
                    patched = true;
                    break; // Assuming only one place to patch
                }
            }
            if (!patched) Plugin.Log?.LogWarning("Could not find patch location in TPeer.DispatchIncomingCommands");
            return instrs;
        }
    }
}
