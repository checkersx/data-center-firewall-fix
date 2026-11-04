# Firewall Fix

A mod for **Data Center** (by Waseku) that fixes a bug where **malware gets straight past a correctly set-up firewall** and shuts your servers down.

**[Get it on the Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3831754850)** · [Download from GitHub Releases](https://github.com/checkersx/data-center-firewall-fix/releases)

![Firewall Fix](workshop/preview.jpg)

**Contents:** [The bug](#the-bug) · [What it does](#what-it-does) · [A firewall that stops malware](#a-firewall-that-stops-malware) · [Before you install](#before-you-install) · [Install](#install) · [Settings](#settings) · [Still getting malware?](#still-getting-malware) · [Compatibility](#compatibility) · [Where the bugs come from](#where-the-bugs-come-from) · [How the fixes work](#how-the-fixes-work) · [Building from source](#building-from-source)

---

## The bug

You do the Internet Access ticket: a router with the customer's subnet, a firewall with an allow rule, everything cabled up. The ticket completes. But every 10 minutes, black malware packets still come in from the Internet, travel along your cables, and switch your servers off, as if the firewall weren't there.

It isn't your setup. The game gives every switch, router and firewall an ID, and each cable remembers the ID of the device at each end. Sometimes, after you load a save or place a new device, a device's cables keep an *old* ID. The malware looks for firewalls by their ID, can't find yours, and treats it as a plain piece of cable. Because of the same mix-up, the firewall's rules don't apply to normal traffic either.

In the saves examined so far, the mix-up started with another mod, gregCore. Details are in [Where the bugs come from](#where-the-bugs-come-from).

## What it does

- **Fixes the IDs.** Every 30 seconds, and right before each malware attack, it finds cables that name a device by an outdated ID and corrects them. Your firewall then stops malware the way it should, and its rules apply.
- **Fixes customers losing all connectivity after a load.** If a newly bought switch ends up with the same ID as one you already have, the game mixes the two up when you next load, and the customer behind your working switch goes offline. The mod gives the new switch its own ID, and repairs saves where that has already happened.
- **Fixes servers that won't connect although they're plugged into a working switch.** Sometimes the game records a switch's cables as belonging to a different switch, so it can't see the link, and the servers behind it stay offline (and their ticket never completes). The mod puts each cable back on the switch it's actually plugged into.
- **Tells you what each attack hit.** The MelonLoader console shows a line per attack, such as `Malware attack: no servers exposed; 1 firewall blocking.`, or which servers the malware could reach.
- **Optional: no malware at all.** One setting switches malware attacks off (see [Settings](#settings)).
- **Works on your existing saves.** Nothing extra is saved, so you can remove the mod at any time.

## A firewall that stops malware

Malware starts at the Internet and follows every cable. A firewall stops it when:

- the firewall is **switched on**,
- every cable route from the Internet to your servers **goes through it** (Internet → router → firewall → your switches, or Internet → firewall → router → …: both work),
- it has **no "allow everything" rule** (allow, any port, no source, no destination). Any other rules block malware.

For the Internet Access ticket, one rule is enough: **allow port 443 from the customer's subnet** (for example `192.168.1.0/24`).

**Do I need a "deny any any" rule?** No. A firewall already blocks anything that no rule allows. Keeping one does no harm, as long as it's below your allow rules (the first rule that matches wins).

---

## Before you install

You need these once, for any code mod in Data Center. If you already use other code mods, you most likely have them.

1. **MelonLoader 0.7.3** from the [MelonLoader releases page](https://github.com/LavaGang/MelonLoader/releases). Run the installer, point it at `Data Center.exe`, choose **0.7.3** and install. Start the game once, then quit.
2. **A fix-up plugin**, or no code mods load at all: [DS-InteropFix](https://steamcommunity.com/sharedfiles/filedetails/?id=3797166996) (Steam Workshop) or [Il2CppAssemblyFixer](https://github.com/mleem97/Il2CppAssemblyFixer) (GitHub). Follow its own instructions.

## Install

There's one file: **`FirewallFix.dll`**. It goes in the **`Mods`** folder inside your game folder. To open the game folder: in your Steam Library, right-click **Data Center** → **Manage** → **Browse local files**. Use the `Mods` folder next to `Data Center.exe`, not the one inside `StreamingAssets`.

- **From GitHub:** download `FirewallFix.dll` from the [Releases page](https://github.com/checkersx/data-center-firewall-fix/releases), put it in `Mods`, and start the game.
- **From the Steam Workshop:** subscribe on the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3831754850). If you have WorkshopModSync (see below), that's all: start the game. Without it, start the game once and quit, then open `Data Center_Data` → `StreamingAssets` → `Mods` in your game folder, type `FirewallFix.dll` into File Explorer's search box, and copy the file it finds into your game's `Mods` folder.

**Check it worked:** the MelonLoader console shows `Firewall Fix v1.2.0`, and within 10 minutes of play it shows a line starting `Malware attack:`.

**WorkshopModSync** is a free helper by Bloody that copies your subscribed Workshop code mods into the right folders every time the game starts. It comes with Bloody's [DHCP Mod](https://steamcommunity.com/sharedfiles/filedetails/?id=3773467133). You have it if your game folder's `Plugins` folder contains `WorkshopModSync.dll`.

**Updating:** with the game closed, replace `FirewallFix.dll` with the new one (WorkshopModSync does this for you).
**Uninstalling:** with the game closed, unsubscribe on the Workshop first (so WorkshopModSync doesn't put it back), then delete `FirewallFix.dll` from `Mods`.

## Settings

In `UserData\MelonPreferences.cfg` in your game folder (it appears after the first run with the mod), under `[FirewallFix]`. Edit it with the game closed.

| Setting | Default | What it does |
|---|---|---|
| `RepairCableIds` | `true` | The firewall fix itself, and the fix for cables recorded on the wrong switch. Leave it on. |
| `RepairDuplicateIds` | `true` | Gives switches that share an ID their own, so customers stay connected after a reload. Leave it on. |
| `DisableMalware` | `false` | Set to `true` to switch malware attacks off completely. |

## Still getting malware?

Look in the MelonLoader console (or `MelonLoader\Latest.log`) for `Malware attack: N server(s) exposed (…)`. It names the servers malware could reach. Each of them has a cable route from the Internet that doesn't go through a working firewall: a second cable to the Internet that skips the firewall, a firewall that's switched off, or one with an "allow everything" rule.

Otherwise, open an issue on [GitHub](https://github.com/checkersx/data-center-firewall-fix/issues) and include `Data Center\MelonLoader\Latest.log`.

## Compatibility

- Standalone: no gregCore or other framework needed. Works alongside other mods, including [Next-Gen Hardware](https://github.com/checkersx/data-centre-next-gen-hardware).
- **Using gregCore?** Firewall Fix repairs the device-ID problems gregCore 1.2.4-dev triggers (see [Where the bugs come from](#where-the-bugs-come-from)).
- **Co-op has never been tested.** Malware attacks are run by the host, so the host needs the mod. To be safe, install it for every player.
- Tested on Steam build 25869593 of Data Center, with MelonLoader 0.7.3.
- Not affiliated with Waseku.

---

## Where the bugs come from

This section is for modders, and for the developers of the game and of gregCore. It's based on the game's code (Steam build 25869593, read through Cpp2IL), the IL of gregCore 1.2.4-dev (the [gregCore Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3719510811) Workshop build of 28 October 2026), and real saves.
- **Confirmed** means it's in the code and reproduces from save data.
- **Suspected** means the evidence fits, but the exact sequence couldn't be replayed, because the logs from that session were gone.

| Symptom | Fixed in | Where it starts | What the game then does | Status |
|---|---|---|---|---|
| Malware gets past a correctly set-up firewall | 1.0 | gregCore changes a placed device's ID but not its ports' or cables' IDs | Malware and route checks find firewalls by the device's ID but follow the cables' IDs | gregCore part suspected, game part confirmed |
| A customer loses all connectivity after a load | 1.1 | gregCore gives newly bought switches IDs that can repeat | On load, its duplicate-ID handling leaves the wrong switch registered | Both confirmed |
| Servers plugged into a working switch won't connect, so their ticket never completes | 1.2 | The same device/cable ID mismatch as the first row | On load, its stale-ID repair can hand a cable to the wrong switch of the same model | gregCore part suspected; the game's behaviour is confirmed in its code, its part here suspected |

None of these has been seen without gregCore installed, but an unmodded game hasn't been tested for them either.

### gregCore

Two pieces of gregCore's WorldSync code (the `DataCenterModLoader` namespace) give devices IDs the game doesn't expect. Both run even though gregCore logs `[HwId] DISABLED … device IDs pass through untouched` on this game build. Both were reported on the [gregCore Workshop discussions](https://steamcommunity.com/sharedfiles/filedetails/discussions/3719510811) on 3 November 2026.

1. **IDs made from the spawn position (confirmed).**
   - `SpawnedObjectTracker.TryResolveNewSwitch` gives a newly spawned switch with an empty ID the ID `{name}_{hash}`. The hash comes from `Patch_UsableObject_InteractOnClick.GenerateDeterministicId(name, transform.position)`: the position rounded to centimetres, combined as `x*73856093 ^ y*19349663 ^ z*83492791`. `TryResolveNewPatchPanel` does the same for patch panels. Nothing checks that the ID is free.
   - The shop delivers to fixed spots, so the same model bought onto the same spot always gets the same ID. In one save, six newly bought switches each had the ID of a switch already racked and cabled for one customer. All six IDs reproduce exactly from the delivery spots, for example (5.36, 1.71, −34.08) gives `Switch16CU_1297367901`.
   - Different models bought onto one spot share the number too: a router, a 16+4-port switch and a 16-port switch all ended in `_1962418541`.
2. **IDs written back after placement (confirmed in the code; its effect suspected).**
   - A postfix on `Rack.MarkPositionAsUsed` records the ID of whatever goes into a rack slot (`Patch_Rack_MarkPositionAsUsed.PendingRestores` / `InstalledObjects`, keyed by rack position).
   - `Patch_NetworkSwitch_SwitchInsertedInRack.Postfix` then writes that ID back over the one the game has just assigned, so the spawn-position ID survives placement.
   - By then the game has already copied its own new ID onto the device's ports (`CableLink.switchID`) and registered it in `NetworkMap`, and neither is updated. Cables plugged in afterwards take the port's ID, so the device and its cables disagree.
   - This matches the firewall in the save that started this mod. The device was `Firewall4xQSXP16xSFP_3088501392`, one of the delivery-spot hashes, and its cables said `Firewall4xQSXP16xSFP_-111464`, an ID the game assigns.

Suggested fixes:

- Before assigning a generated or restored ID, check that no other `NetworkSwitch` or `PatchPanel` in the scene has it, and pick another if one does. Check every object, not only those in `NetworkMap`, because boxed devices aren't registered. The game's own `GenerateUniqueSwitchId()` is one option.
- Or leave devices in their boxes without an ID: the game gives them one when they're racked.
- When restoring an ID at placement, also update the device's ports and call `NetworkMap.RemapDeviceId(gameId, restoredId)`, so the ports, the cables and the map agree.

### The game

These are in the game's own code. They do no harm as long as each device has one ID that its ports and cables share, which seems to hold in the unmodded game. But they turn the mismatches above into lost connectivity, and the game could guard against all three.

1. **Malware and route checks rely on device and cable IDs agreeing (confirmed).**
   - `MaliciousAttackManager.ComputeAttacks` builds its graph from `WaypointInitializationSystem.GetAllCables()` and names each switch end after `CableEndpoint.SwitchID`. It runs a BFS from `"Internet"` that stops at walls, and attacks every powered server it reaches.
   - Walls are firewalls from `NetworkMap.GetAllFirewalls()` that are on and have no blank allow-all rule, matched by `NetworkSwitch.switchId`. When the device's ID and its cables' ID differ, the firewall is just another node and malware walks through it.
   - Route evaluation's `GetSwitchById(cableId)` returns null for such a device, so the firewall's rules are skipped too.
   - The game's own repair, `NetworkSwitch.PatchStaleSwitchId`, only runs from `ReconnectCables`, when a device powers on.
2. **Duplicate IDs on load leave the wrong device registered (confirmed).**
   - `NetworkSwitch.SwitchInsertedInRack(SwitchSaveData)` finds the ID taken (`ContainsDuplicateSwitchId`), gives this device a new ID and calls `NetworkMap.RemapDeviceId(old, new)`.
   - That moves the entry of the device that *already had* the ID, and `RegisterSwitch` only adds keys that are absent. So the renamed device is never registered, and its new ID points at the other device.
   - Path finding (`FindAllServerPathsFromCustomerPure`, `FindAllShortestPaths`) skips any node whose `switches` entry is switched off. When the other device is a switch still in its box, every route through the renamed one is cut.
   - Suggested fix: skip that `RemapDeviceId` call, since nothing of this device is registered yet, and register the renamed device.
3. **The load's stale-ID repair matches by name only (confirmed in the code; its part in the save suspected).**
   - "Restoring SwitchID logic" in the network load coroutine goes through every port with a cable. It takes whichever of the cable's endpoint IDs has the port's name prefix (for example `Switch16CU_`) and names no live switch, and calls `NetworkMap.RemapDeviceId(stale, portId)` across the whole map.
   - It never checks which end of the cable is in the port, so a stale ID can be given to another switch of the same model. After that, nothing treats it as stale.
   - In one save, every cable in the three switches that had gregCore's IDs was recorded on three other switches, one of them another customer's. That included both uplinks of two of them, which is why new servers there couldn't connect.
   - Suggested fix: use the port's own cable (`CableLink.cableIDsOnLink`) and the end nearest the port, and rename that end only.

If you change the network map at runtime, note that the game only re-evaluates routes on request. It calls `WaypointInitializationSystem.RequestRouteEvaluation()` itself after power, cable, LACP and route changes, so call it after your change too.

### Ruled out

- **Firewall Fix:** its repairs only ever rename a cable end to the device whose port holds it, or give a boxed duplicate a fresh ID along with its ports, so they can't create either kind of mismatch.
- **Next-Gen Hardware and Next-Gen Patch Panels** (same author): neither changes switch IDs.

## How the fixes work

### Stale cable IDs (1.0)

For every switch, router and firewall, the mod does what `PatchStaleSwitchId` does:
1. For each port, it corrects a stale `CableLink.switchID`.
2. It finds the port's cable end (≤ 0.3 m from the port) and corrects a stale `SwitchID` through `UpdateCableInfo`.
3. It calls `NetworkMap.RemapDeviceId(old, new)` so routing uses the new ID.

An ID counts as stale when it isn't the device's own ID, names no device that exists, and has the same name prefix. The repair runs every 30 seconds and in a Harmony prefix on `ComputeAttacks`.

### Duplicate device IDs (1.1)

- Of each set of devices sharing an ID, the one registered in `NetworkMap` keeps it. Each other copy gets a fresh `GenerateUniqueSwitchId()`, and its ports follow, as long as it has no cables.
- For any device whose ID is filed under a different device in `NetworkMap.switches`, the mod puts the device back and re-registers the other one under its own ID.
- This runs every 30 seconds, before each attack, and in a prefix on `SaveSystem.SaveGame`. For the first minute after a load, it also checks the map every 5 seconds, without renaming anything.
- A last-priority postfix on `SwitchInsertedInRack` catches a device placed with another device's ID. It returns the device to the ID the game just gave it, which its ports and `NetworkMap` already use.

### Cables recorded on the wrong switch (1.2)

- Each port records the cable plugged into it (`CableLink.cableIDsOnLink`).
- When a port holds a cable whose nearest end (≤ 0.3 m) names another switch of the same model, and that switch has no port holding that end at least as close, the mod renames the end to the port's switch.
- The map is fixed one cable at a time, the way the game does when a switch powers off and on: `RemoveCableConnection(id, preserveLACP: true)`, `UpdateCableInfo`, `RegisterCableInNetworkMap`. LACP groups follow, and routes are re-evaluated.
- This is part of the 30-second repair.

## Building from source

You need the .NET SDK (6 or later) and the game with MelonLoader installed (start the game once so MelonLoader generates its files).

```
dotnet build -c Release src
```

The build looks for the game at `G:\SteamLibrary\steamapps\common\Data Center`. If yours is somewhere else, add `-p:GameDir="<your game folder>"`. The built DLL is copied straight into the game's `Mods` folder (add `-p:Deploy=false` to skip that). Close the game first, because the game locks the DLL while it's running.

`workshop\build-package.ps1` builds the Steam Workshop upload into `publish\`.

## Licence

[GNU General Public License v3.0](LICENSE).
