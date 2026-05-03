# Gnomium Internal Menu (Gnomium 内部作弊菜单)

针对 **Burglin' Gnomes**（进程名：Gnomium）开发的内部作弊辅助菜单。基于 C# 编写，专为通过 [SharpMonoInjector](https://github.com/wh0am15533/SharpMonoInjector) 注入到 Mono 运行时而设计。

## 🚀 概述

本项目是一个功能全面的内部菜单，主要利用了游戏在 Unity Netcode for GameObjects (NGO) 实现上的结构性安全漏洞。通过反编译游戏，我们发现并利用了超过 120 个标记为 RpcInvokePermission.Everyone 的 RPC 调用，使得任何客户端都能对全局游戏状态进行深度操作与破坏。

## 🛠️ 功能特性 (v2)

菜单包含 8 大功能标签页（使用 Insert 键显示/隐藏）：

* **玩家 (Player)**:
  * 上帝模式（反射私有字段，同时解锁免费制作）与无限体力。
  * 幽灵飞行与穿墙（IJKL/U/O 控制方向，Shift 加速）。
  * 移速倍率修改与超级跳跃（V + 空格）。
  * 隐身模式（本地图形渲染切换 - F4）。
* **传送 (Teleport)**:
  * 快速传送到最近的：赃物、存箱、敌人、载具或队友。
  * 3 个自定义坐标存档槽（F1/F2/F3 保存，Shift+F1/F2/F3 传送）。
  * 视线方向瞬移。
  * 万象天引（将全图未搬运的赃物拉取到面前）。
* **物品 (Items)**:
  * 任意生成游戏数据库中的物品。
  * 主机模式：通过 NetworkObject.InstantiateAndSpawn 直接权威生成并放入背包。
  * 客户机模式：通过 PlayerWantsToTakeItemRpc 尝试从全局物品池获取。
* **机制 (Mechanics)**:
  * 时间控制（锁定时间、任意修改昼夜）。
  * 任务秒做（一键完成所有当前任务）。
  * 进程控制：强制开局、强制结束本局或重置世界。
  * AI 控制：永久消除全图敌人、冻结设定、强制捆绑全图敌人。
  * 载具改装：解除全图载具的速度与扭矩限制。
* **网络 (Network) [RPC 漏洞利用]**:
  * 秒杀全图怪物（TakeDamageRpc）。
  * 一键上交全图掉落资源（DepositResourcesToStockpileRpc）。
  * 虚空神偷：跨空窃取队友背包物品。
  * 恶搞队友：强制肢解非本机玩家、强制丢弃队友背包所有物品。
* **队友 (Teammates)**:
  * 全局复活队友（RespawnRpc）与瞬间满血（SelfDamageRpc）。
  * 一键解除所有队友的捆绑状态。
  * 远程激活医疗终端立刻复活全员。
* **视觉 (Visuals)**:
  * 全面的 ESP 透视框与信息（涵盖敌人、存箱、赃物、队友、载具）。
  * 动态距离控制滑块。
* **混沌 (Chaos)**:
  * 瞬间引爆全图手雷或集体点燃引线。
  * 洪水淹没：触发全图马桶喷水效果。
  * 龙卷风：激活全图精灵房屋的传送吸入效果。
  * 毁灭世界上所有的花园侏儒。

## 📦 编译指南

环境要求：
- .NET SDK（目标框架：
etstandard2.1）
- 需要从游戏目录 Gnomium_Data/Managed/ 反编译/提取 Unity 和游戏的依赖库（如 UnityEngine.dll、Assembly-CSharp.dll、Unity.Netcode.Runtime.dll 等）作为项目引用。

编译命令：
`powershell
dotnet build CheatPayload.csproj -c Release
`

## 💉 注入方法

确保游戏正在运行，然后使用命令行工具 smi.exe (SharpMonoInjector) 将编译好的 .dll 注入进程：

`powershell
smi.exe inject -p "Gnomium" -a "bin\Release\netstandard2.1\CheatPayload.dll" -n BurglinCheat -c Loader -m Init
`

## ⚠️ 免责声明

本项目仅供教育和逆向工程研究使用。本项目通过展示跨端 RPC 权限滥用造成的全局影响，凸显了在 Unity 多人游戏开发中进行服务端权威校验的必要性。因使用本工具造成的任何毁坏体验或封号等后果由使用者自行承担。
