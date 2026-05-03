# Gnomium Internal Menu

An internal mod/cheat menu for **Burglin' Gnomes** (Process: `Gnomium`), written in C# and designed to be injected into the Mono backend via [SharpMonoInjector](https://github.com/wh0am15533/SharpMonoInjector).

## 🚀 Overview

This project is a comprehensive internal menu that exploits structural vulnerabilities in the game's Unity Netcode for GameObjects (NGO) implementation. By decompiling the game, we identified and utilized over 120 `RpcInvokePermission.Everyone` RPCs, allowing for extensive manipulation of the game state from any client.

## 🛠️ Features (v2)

The menu features an 8-tab GUI (`Insert` to toggle):

* **玩家 (Player)**:
  * God Mode (via private field reflection & self-heal RPC) & Free Crafting.
  * Infinite Stamina.
  * Ghost Flight & NoClip (IJKL/U/O + Shift).
  * Multiplier Speed Hack & Super Jump (V + Space).
  * Invisibility (Local Graphics Toggle - F4).
* **传送 (Teleport)**:
  * Teleport to nearest: Loot, Deposits, Enemies, Vehicles, Teammates.
  * 3 Custom Save Slots (F1/F2/F3 to save, Shift+F1/F2/F3 to warp).
  * Camera-directional blinking.
  * Loot Magnet (Pull all uncarried loot to camera).
* **物品 (Items)**:
  * Instantly spawn any item in the database.
  * Host Mode: Direct spawning via `NetworkObject.InstantiateAndSpawn` with auto-inventory addition.
  * Client Mode: Inventory request via `PlayerWantsToTakeItemRpc`.
* **机制 (Mechanics)**:
  * Time manipulation (Freeze time, modify time of day).
  * Task Auto-Completer (Instantly finish all active objectives).
  * Match Control: Force start game, end game, or reset.
  * AI Control: Despawn all enemies, disable map AI, global tie-up.
  * Vehicle Hacks: Override max speed and torque.
* **网络 (Network)**:
  * Map-wide kill (`TakeDamageRpc`).
  * Auto-deposit all resources (`DepositResourcesToStockpileRpc`).
  * Item Theft: Steal from teammates' inventories.
  * Troll features: Mass dismemberment, forcing players to drop all items.
* **队友 (Teammates)**:
  * Global teammate resurrection & healing (`RespawnRpc` / `SelfDamageRpc`).
  * Untie all teammates.
  * Remote Medical Terminal activation.
* **视觉 (Visuals)**:
  * Comprehensive ESP (Enemies, Deposits, Loot, Teammates, Vehicles).
  * Adjustable max distance slider.
* **混沌 (Chaos)**:
  * Insta-explode map grenades.
  * Trigger map-wide toilet floods & gnome house vortexes.
  * Destroy all garden gnomes.

## 📦 Build Instructions

Requirements:
- .NET SDK (target is `netstandard2.1`)
- Extracted Unity dependencies from `Gnomium_Data/Managed/` (e.g., `UnityEngine.dll`, `Assembly-CSharp.dll`, `Unity.Netcode.Runtime.dll`, etc.)

To build:
```powershell
dotnet build CheatPayload.csproj -c Release
```

## 💉 Injection

Ensure the game is running, then inject the built `.dll` using `smi.exe` (SharpMonoInjector command line):

```powershell
smi.exe inject -p "Gnomium" -a "bin\Release\netstandard2.1\CheatPayload.dll" -n BurglinCheat -c Loader -m Init
```

## ⚠️ Disclaimer

This project is for educational and reverse-engineering research purposes only. The identification of improperly secured RPCs highlights the importance of server-authoritative checks in Unity multiplayer development. Use at your own risk.
