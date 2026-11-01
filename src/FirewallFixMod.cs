using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(FirewallFix.FirewallFixMod), "Firewall Fix", "1.0.0", "checkersx")]
[assembly: MelonGame("Waseku", "Data Center")]

namespace FirewallFix
{
    // Malware gets past correctly configured firewalls because cables keep a stale ID for the device they
    // plug into (see CableIds). This repairs those IDs before every attack and every 30 s, and logs what
    // each attack hit. Optional: switch malware off entirely.
    public class FirewallFixMod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        static MelonPreferences_Entry<bool> _repair;
        static MelonPreferences_Entry<bool> _disableMalware;
        static float _nextRepair;

        const float RepairInterval = 30f;
        const float FirstRepairDelay = 60f; // let the save's network finish loading first

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            var prefs = MelonPreferences.CreateCategory("FirewallFix", "Firewall Fix");
            _repair = prefs.CreateEntry("RepairCableIds", true, "Repair stale cable device IDs",
                "Fixes cables that point at an old ID for their switch, router or firewall, so firewalls block malware and apply their rules.");
            _disableMalware = prefs.CreateEntry("DisableMalware", false, "Disable malware attacks",
                "Skips the game's malware attacks entirely.");

            Patch(typeof(MaliciousAttackManager), nameof(MaliciousAttackManager.ComputeAttacks), nameof(ComputeAttacksPrefix), nameof(ComputeAttacksPostfix));
            Patch(typeof(MaliciousAttackManager), nameof(MaliciousAttackManager.LaunchAttack), nameof(LaunchAttackPrefix), null);
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName) =>
            _nextRepair = Time.realtimeSinceStartup + FirstRepairDelay;

        public override void OnUpdate()
        {
            if (!_repair.Value || Time.realtimeSinceStartup < _nextRepair) return;
            _nextRepair = Time.realtimeSinceStartup + RepairInterval;
            CableIds.Repair("periodic check");
        }

        public static bool LaunchAttackPrefix()
        {
            if (!_disableMalware.Value) return true;
            Log.Msg("Malware attack skipped (DisableMalware is on).");
            return false;
        }

        public static void ComputeAttacksPrefix()
        {
            if (_repair.Value) CableIds.Repair("before malware attack");
        }

        public static void ComputeAttacksPostfix(Il2CppSystem.Collections.Generic.List<MaliciousAttackManager.AttackPath> __result) =>
            AttackLog.Report(__result);

        void Patch(Type type, string method, string prefix, string postfix)
        {
            try
            {
                var target = AccessTools.Method(type, method);
                if (target == null)
                {
                    Log.Error($"Patch target {type.Name}.{method} not found; the game may have changed.");
                    return;
                }
                HarmonyInstance.Patch(target,
                    prefix: prefix == null ? null : new HarmonyMethod(AccessTools.Method(typeof(FirewallFixMod), prefix)),
                    postfix: postfix == null ? null : new HarmonyMethod(AccessTools.Method(typeof(FirewallFixMod), postfix)));
            }
            catch (Exception e)
            {
                Log.Error($"Patching {type.Name}.{method} failed: {e.Message}");
            }
        }
    }
}
