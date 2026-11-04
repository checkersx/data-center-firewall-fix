using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(FirewallFix.FirewallFixMod), "Firewall Fix", "1.2.0", "checkersx")]
[assembly: MelonGame("Waseku", "Data Center")]

namespace FirewallFix
{
    // Malware gets past correctly configured firewalls because cables keep a stale ID for the device they
    // plug into (see CableIds). This repairs those IDs before every attack and every 30 s, and logs what
    // each attack hit. It also keeps device IDs unique, which the game needs to reload a network intact
    // (see DeviceIds). Optional: switch malware off entirely.
    public class FirewallFixMod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        static MelonPreferences_Entry<bool> _repair;
        static MelonPreferences_Entry<bool> _dedupe;
        static MelonPreferences_Entry<bool> _disableMalware;
        static float _nextRepair;
        static float _nextEarlyCheck, _earlyChecksUntil;

        const float RepairInterval = 30f;
        const float FirstRepairDelay = 60f; // let the save's network finish loading first
        const float EarlyCheckInterval = 5f; // until then, a quick map check: a save with duplicate IDs cuts a customer off on load

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            var prefs = MelonPreferences.CreateCategory("FirewallFix", "Firewall Fix");
            _repair = prefs.CreateEntry("RepairCableIds", true, "Repair stale cable device IDs",
                "Fixes cables that point at an old or wrong ID for their switch, router or firewall, so firewalls block malware and apply their rules, and every switch's cables count as its own.");
            _dedupe = prefs.CreateEntry("RepairDuplicateIds", true, "Repair duplicate device IDs",
                "Gives a new switch its own ID when it got the ID of one you already have, so a customer doesn't lose connectivity when the save is next loaded. Also repairs saves where that already happened.");
            _disableMalware = prefs.CreateEntry("DisableMalware", false, "Disable malware attacks",
                "Skips the game's malware attacks entirely.");

            Patch(typeof(MaliciousAttackManager), nameof(MaliciousAttackManager.ComputeAttacks), nameof(ComputeAttacksPrefix), nameof(ComputeAttacksPostfix));
            Patch(typeof(MaliciousAttackManager), nameof(MaliciousAttackManager.LaunchAttack), nameof(LaunchAttackPrefix), null);
            Patch(typeof(SaveSystem), nameof(SaveSystem.SaveGame), nameof(SaveGamePrefix), null);
            Patch(typeof(SaveSystem), nameof(SaveSystem.LoadGame), null, nameof(LoadGamePostfix));
            // Last, so it sees the ID other mods' postfixes leave behind.
            Patch(typeof(NetworkSwitch), nameof(NetworkSwitch.SwitchInsertedInRack), null, nameof(SwitchInsertedInRackPostfix), HarmonyLib.Priority.Last);
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            _nextRepair = Time.realtimeSinceStartup + FirstRepairDelay;
            StartEarlyChecks();
        }

        public static void LoadGamePostfix() => StartEarlyChecks();

        static void StartEarlyChecks()
        {
            _nextEarlyCheck = Time.realtimeSinceStartup + EarlyCheckInterval;
            _earlyChecksUntil = Time.realtimeSinceStartup + FirstRepairDelay;
        }

        public override void OnUpdate()
        {
            var now = Time.realtimeSinceStartup;
            if (_dedupe.Value && now < _earlyChecksUntil && now >= _nextEarlyCheck)
            {
                _nextEarlyCheck = now + EarlyCheckInterval;
                DeviceIds.Repair("after load", renameCopies: false);
            }

            if (now < _nextRepair) return;
            _nextRepair = now + RepairInterval;
            RepairAll("periodic check");
        }

        // Duplicates first: the cable repair only trusts IDs that name exactly one device.
        static void RepairAll(string reason)
        {
            if (_dedupe.Value) DeviceIds.Repair(reason);
            if (_repair.Value) CableIds.Repair(reason);
        }

        public static void SaveGamePrefix()
        {
            if (_dedupe.Value) DeviceIds.Repair("before save");
        }

        // A null save record means the player placed it; loads go through the game's own duplicate check.
        public static void SwitchInsertedInRackPostfix(NetworkSwitch __instance, SwitchSaveData __0)
        {
            if (_dedupe.Value && __0 == null) DeviceIds.Placed(__instance);
        }

        public static bool LaunchAttackPrefix()
        {
            if (!_disableMalware.Value) return true;
            Log.Msg("Malware attack skipped (DisableMalware is on).");
            return false;
        }

        public static void ComputeAttacksPrefix()
        {
            RepairAll("before malware attack");
        }

        public static void ComputeAttacksPostfix(Il2CppSystem.Collections.Generic.List<MaliciousAttackManager.AttackPath> __result) =>
            AttackLog.Report(__result);

        void Patch(Type type, string method, string prefix, string postfix, int priority = HarmonyLib.Priority.Normal)
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
                    postfix: postfix == null ? null : new HarmonyMethod(AccessTools.Method(typeof(FirewallFixMod), postfix), priority));
            }
            catch (Exception e)
            {
                Log.Error($"Patching {type.Name}.{method} failed: {e.Message}");
            }
        }
    }
}
