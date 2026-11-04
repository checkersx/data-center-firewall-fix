using System;
using System.Collections.Generic;
using System.Linq;
using Il2Cpp;

namespace FirewallFix
{
    // Two devices can end up with the same ID. A mod can name a newly spawned switch "{model}_{hash of its spawn
    // position}", and the shop drops purchases on the same few spots, so a new switch takes the ID of the last one of
    // its model bought onto that spot (seen in a save: six new switches in the delivery area, each sharing its ID with
    // a racked switch of one customer).
    // That's harmless until the save is loaded. SwitchInsertedInRack then gives the second copy a new ID and calls
    // NetworkMap.RemapDeviceId(old, new), which moves the *first* copy's map entry to the new ID; RegisterSwitch won't
    // overwrite it, so the renamed device is never registered. Route evaluation looks the renamed device up, finds the
    // other copy (switched off, in its box), skips it as powered down, and the customer behind it loses connectivity.
    // This gives every copy but one a fresh ID before the save is written, and puts devices the map has filed under
    // another device's ID back under their own (for saves that already have duplicates).
    internal static class DeviceIds
    {
        static readonly HashSet<string> Warned = new();

        // renameCopies is off while a save may still be loading: devices not yet given their saved ID can
        // look like copies. The game has already renamed any real copies by the time loading ends.
        public static void Repair(string reason, bool renameCopies = true)
        {
            try { RepairCore(reason, renameCopies); }
            catch (Exception e) { FirewallFixMod.Log.Error($"Duplicate ID repair ({reason}) failed: {e}"); }
        }

        static void RepairCore(string reason, bool renameCopies)
        {
            var map = NetworkMap.instance;
            if (map == null || map.switches == null) return;
            var devices = CableIds.LiveDevices();
            if (devices.Count == 0) return;
            var notes = new List<string>();

            foreach (var group in devices.GroupBy(d => d.switchId).Where(g => renameCopies && g.Count() > 1))
            {
                var copies = group.ToList();
                // The one the map knows keeps the ID: its cables and routes already use it.
                var keeper = copies.FirstOrDefault(d => IsRegistered(map, d))
                             ?? copies.FirstOrDefault(d => d.IsAnyCableConnected())
                             ?? copies[0];
                foreach (var d in copies)
                {
                    if (d == keeper) continue;
                    if (d.IsAnyCableConnected())
                    {
                        // Which cables belong to which copy is guesswork; leave it to the player.
                        if (Warned.Add(group.Key))
                            FirewallFixMod.Log.Warning($"{Describe(d)} and {Describe(keeper)} share an ID and both have cables. " +
                                                       "Unplug and re-place one of them to give it its own ID.");
                        continue;
                    }

                    var old = d.switchId;
                    d.switchId = d.GenerateUniqueSwitchId();
                    // A new cable copies its port's ID, so the ports must follow.
                    var ports = d.cableLinkSwitchPorts;
                    if (ports != null)
                        for (int i = 0; i < ports.Length; i++)
                            if (ports[i] != null && ports[i].switchID == old) ports[i].switchID = d.switchId;
                    notes.Add($"{old} -> {d.switchId} (a second device with {Describe(keeper)}'s ID)");
                }
            }

            // Acts only on the state the load bug leaves behind, so it does nothing once the cause or the game is fixed.
            foreach (var d in devices)
            {
                var id = d.switchId;
                NetworkSwitch other = null;
                if (!map.switches.TryGetValue(id, out other) || other == null || other == d || other.switchId == id) continue;
                // Ports still on this ID mean the other device's cables are too, so the entry is right for them
                // (a device renamed without its cables, which is CableIds' job, not this bug).
                if (PortsCarry(other, id)) continue;
                map.switches[id] = d;
                // Checked here rather than relying on RegisterSwitch never overwriting.
                if (!map.switches.ContainsKey(other.switchId)) map.RegisterSwitch(other);
                notes.Add($"{Describe(d)} re-registered (the map had {Describe(other)} under its ID)");
            }

            if (notes.Count == 0) return;
            FirewallFixMod.Log.Msg($"Repaired duplicate device IDs ({reason}): {string.Join("; ", notes)}.");
            // The game only re-evaluates routes when something on the network changes; without this the
            // customer stays offline until the player next touches a cable or power button.
            WaypointInitializationSystem.Instance?.RequestRouteEvaluation();
        }

        // A device just placed in a rack (not loaded from a save). Another mod's SwitchInsertedInRack postfix may put back
        // the ID the device had in its box, which can be another device's. The ID the game had just given it is still
        // on its ports and in NetworkMap, so going back to that keeps all three in step.
        public static void Placed(NetworkSwitch d)
        {
            try
            {
                if (d == null || string.IsNullOrEmpty(d.switchId)) return;
                var devices = CableIds.LiveDevices();
                var id = d.switchId;
                if (!devices.Any(o => o != d && o.switchId == id)) return;

                var ports = d.cableLinkSwitchPorts;
                var given = ports != null && ports.Length > 0 && ports[0] != null ? ports[0].switchID : null;
                var map = NetworkMap.instance;
                if (!string.IsNullOrEmpty(given) && given != id && !devices.Any(o => o != d && o.switchId == given) &&
                    map != null && map.switches != null && map.switches.TryGetValue(given, out var reg) && reg == d)
                    d.switchId = given;
                else
                {
                    d.switchId = d.GenerateUniqueSwitchId();
                    if (ports != null)
                        for (int i = 0; i < ports.Length; i++)
                            if (ports[i] != null) ports[i].switchID = d.switchId;
                }
                FirewallFixMod.Log.Msg($"Placed device took another device's ID ({id}); it is {d.switchId} instead.");
            }
            catch (Exception e) { FirewallFixMod.Log.Error($"Placement ID check failed: {e}"); }
        }

        static bool PortsCarry(NetworkSwitch d, string id)
        {
            var ports = d.cableLinkSwitchPorts;
            if (ports == null) return false;
            for (int i = 0; i < ports.Length; i++)
                if (ports[i] != null && ports[i].switchID == id) return true;
            return false;
        }

        static bool IsRegistered(NetworkMap map, NetworkSwitch d) =>
            map.switches.TryGetValue(d.switchId, out var v) && v == d;

        internal static string Describe(NetworkSwitch d) =>
            string.IsNullOrEmpty(d.labelText) ? d.switchId : $"{d.labelText} ({d.switchId})";
    }
}
