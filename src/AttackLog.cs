using System;
using System.Collections.Generic;
using Il2Cpp;

namespace FirewallFix
{
    // One line per malware attack: which servers it reaches and how many firewalls stop it.
    // ComputeAttacks returns one path per exposed server (target set) plus one per blocking firewall (target null).
    internal static class AttackLog
    {
        const int MaxNames = 8;

        public static void Report(Il2CppSystem.Collections.Generic.List<MaliciousAttackManager.AttackPath> attacks)
        {
            try
            {
                if (attacks == null) return;
                int exposed = 0, blocked = 0;
                var names = new List<string>();
                for (int i = 0; i < attacks.Count; i++)
                {
                    var a = attacks[i];
                    if (a == null) continue;
                    if (a.target == null) { blocked++; continue; }
                    exposed++;
                    if (names.Count < MaxNames) names.Add(a.target.ServerID);
                }

                var walls = blocked == 1 ? "1 firewall blocking" : $"{blocked} firewalls blocking";
                if (exposed == 0)
                    FirewallFixMod.Log.Msg($"Malware attack: no servers exposed; {walls}.");
                else
                    FirewallFixMod.Log.Warning($"Malware attack: {exposed} server(s) exposed ({string.Join(", ", names)}" +
                                               $"{(exposed > names.Count ? ", ..." : "")}); {walls}. " +
                                               "Their path from the Internet doesn't cross a powered firewall without an allow-all rule.");
            }
            catch (Exception e) { FirewallFixMod.Log.Error($"Attack report failed: {e.Message}"); }
        }
    }
}
