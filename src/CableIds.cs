using System;
using System.Collections.Generic;
using System.Linq;
using Il2Cpp;
using UnityEngine;

namespace FirewallFix
{
    // MaliciousAttackManager.ComputeAttacks builds its graph from the cable table (WaypointInitializationSystem),
    // naming each switch end by CableEndpoint.SwitchID, but recognises a firewall as a wall by its own switchId.
    // Those can differ: the device keeps the ID from the save while its cables carry a session-only one
    // (seen in a save: firewall "Firewall4xQSXP16xSFP_3088501392", its cables "..._-111464").
    // GetSwitchById finds nothing under the cable's name, so malware passes through the firewall as if it were
    // a plain cable joint, and route evaluation skips its rules for the same reason.
    // Vanilla repairs this in NetworkSwitch.PatchStaleSwitchId, but only when a device reconnects its cables.
    // This does the same for every device: a cable end at one of its ports that names a device which no longer
    // exists, with the same name prefix, is renamed to the device's current ID.
    // A cable end can also name the wrong device that does exist: vanilla's load repair renames stale IDs by name
    // prefix alone, so it can give them to any other switch of the same model (seen in a save: every cable in three
    // switches recorded on three others). The port's own record of its cable settles whose end it is.
    internal static class CableIds
    {
        const float MaxEndToPort = 0.3f; // metres; a cable end sits on its port

        public static void Repair(string reason)
        {
            try { RepairCore(reason); }
            catch (Exception e) { FirewallFixMod.Log.Error($"Cable ID repair ({reason}) failed: {e}"); }
        }

        static void RepairCore(string reason)
        {
            var map = NetworkMap.instance;
            var wis = WaypointInitializationSystem.Instance;
            if (map == null || wis == null) return;

            var devices = LiveDevices();
            if (devices.Count == 0) return;
            var live = new HashSet<string>(devices.Select(d => d.switchId));
            var byId = new Dictionary<string, NetworkSwitch>();
            foreach (var d in devices) byId[d.switchId] = d;

            // GetAllCables returns a copy; UpdateCableInfo writes back to the live table.
            var cables = new Dictionary<int, WaypointInitializationSystem.CableInfo>();
            var all = wis.GetAllCables();
            if (all == null) return;
            for (int i = 0; i < all.Count; i++) cables[all[i].CableID] = all[i];

            var remaps = new Dictionary<string, (string to, int ends)>();
            var moved = new Dictionary<(NetworkSwitch to, NetworkSwitch from), int>();
            int ports = 0;

            foreach (var d in devices)
            {
                var id = d.switchId;
                var links = d.cableLinkSwitchPorts;
                if (links == null) continue;
                for (int i = 0; i < links.Length; i++)
                {
                    var link = links[i];
                    if (link == null) continue;

                    // A new cable copies its port's ID, so a stale one here would bring the problem back.
                    if (IsStale(link.switchID, id, live))
                    {
                        link.switchID = id;
                        ports++;
                    }

                    if (!cables.TryGetValue(link.cableIDsOnLink, out var info)) continue;
                    var pos = link.transform.position;
                    float ds = Vector3.Distance(info.StartPoint.Position, pos), de = Vector3.Distance(info.EndPoint.Position, pos);
                    bool start = ds <= de;
                    if (Math.Min(ds, de) > MaxEndToPort) continue; // not this port's cable (empty port reporting an old ID)

                    var end = start ? info.StartPoint : info.EndPoint;
                    if (end.Type != CableLink.TypeOfLink.Switch) continue;
                    var old = end.SwitchID;
                    bool stale = IsStale(old, id, live);
                    // Names another device that exists, but this port holds the cable and that device doesn't hold
                    // this end. old still names a real device, so the map is fixed per cable, not with RemapDeviceId.
                    NetworkSwitch owner = null;
                    bool misplaced = !stale && old != id && Prefix(old ?? "") == Prefix(id) &&
                                     byId.TryGetValue(old ?? "", out owner) &&
                                     !Holds(owner, info.CableID, end.Position, Math.Min(ds, de));
                    if (!stale && !misplaced) continue;

                    if (misplaced) map.RemoveCableConnection(info.CableID, true); // as vanilla's DisconnectCables
                    end.SwitchID = id;
                    if (start) info.StartPoint = end;
                    else info.EndPoint = end;
                    wis.UpdateCableInfo(info.CableID, info);
                    cables[info.CableID] = info;

                    if (misplaced)
                    {
                        wis.RegisterCableInNetworkMap(info);
                        var group = map.GetLACPGroupForCable(info.CableID);
                        if (group != null)
                        {
                            if (group.deviceA == old) group.deviceA = id;
                            if (group.deviceB == old) group.deviceB = id;
                        }
                        moved[(d, owner)] = moved.TryGetValue((d, owner), out var n) ? n + 1 : 1;
                        continue;
                    }

                    var key = old ?? "";
                    remaps[key] = (id, remaps.TryGetValue(key, out var r) ? r.ends + 1 : 1);
                }
            }

            // Route evaluation reads NetworkMap's copy of the graph. Vanilla's load repairs it with the same call.
            foreach (var kv in remaps)
                if (kv.Key.Length > 0) map.RemapDeviceId(kv.Key, kv.Value.to);

            if (remaps.Count > 0 || moved.Count > 0) wis.RequestRouteEvaluation(); // routes are only re-evaluated on request
            if (moved.Count > 0)
                FirewallFixMod.Log.Msg($"Repaired cables recorded on the wrong device ({reason}): " +
                                       string.Join("; ", moved.Select(kv => $"{DeviceIds.Describe(kv.Key.to)}: {kv.Value} cable end(s) were recorded on {DeviceIds.Describe(kv.Key.from)}")) + ".");
            if (remaps.Count == 0 && ports == 0) return;
            var detail = string.Join("; ", remaps.Select(kv => $"{kv.Value.to}: {kv.Value.ends} cable end(s) said '{kv.Key}'"));
            FirewallFixMod.Log.Msg($"Repaired stale device IDs ({reason}): {detail}" +
                                   (ports > 0 ? $"{(remaps.Count > 0 ? "; " : "")}{ports} port ID(s)" : "") + ".");
        }

        // Whether one of d's ports holds this cable at least as close to the given end as the port that claims it.
        static bool Holds(NetworkSwitch d, int cableId, Vector3 end, float claimDistance)
        {
            var links = d.cableLinkSwitchPorts;
            if (links == null) return false;
            for (int i = 0; i < links.Length; i++)
                if (links[i] != null && links[i].cableIDsOnLink == cableId &&
                    Vector3.Distance(links[i].transform.position, end) <= claimDistance) return true;
            return false;
        }

        // Stale: not the device's ID, names no live device, and looks like an older ID of the same device type.
        static bool IsStale(string seen, string id, HashSet<string> live) =>
            seen != id && !live.Contains(seen ?? "") && (string.IsNullOrEmpty(seen) || Prefix(seen) == Prefix(id));

        static string Prefix(string id)
        {
            int i = id.LastIndexOf('_');
            return i > 0 ? id.Substring(0, i) : id;
        }

        // Switches, routers and firewalls placed in the scene (FindObjectsOfTypeAll also returns templates).
        internal static List<NetworkSwitch> LiveDevices()
        {
            var found = Resources.FindObjectsOfTypeAll<NetworkSwitch>();
            var list = new List<NetworkSwitch>();
            for (int i = 0; i < found.Length; i++)
            {
                var d = found[i];
                if (d == null || string.IsNullOrEmpty(d.gameObject.scene.name) || string.IsNullOrEmpty(d.switchId)) continue;
                list.Add(d);
            }
            return list;
        }
    }
}
