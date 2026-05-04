using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;
using Lightbug.CharacterControllerPro.Core;

namespace BurglinCheat
{
    public class Loader
    {
        public static GameObject LoadObject;

        public static void Init()
        {
            try
            {
                if (LoadObject == null)
                {
                    LoadObject = new GameObject("BurglinCheat_Loader");
                    GameObject.DontDestroyOnLoad(LoadObject);
                    LoadObject.AddComponent<CheatMain>();
                }
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CheatInject_FailLog.txt"), ex.ToString());
            }
        }

        public static void Unload()
        {
            if (LoadObject != null) GameObject.Destroy(LoadObject);
        }
    }

    public class CheatMain : MonoBehaviour
    {
        // ============ UI 状态 ============
        private bool showMenu = true;
        private Rect menuRect = new Rect(20, 20, 420, 620);
        private Vector2 scrollPos;
        private int currentTab = 0;
        private string[] tabs = { "玩家", "传送", "物品", "机制", "网络", "队友", "视觉", "混沌" };

        // ============ 玩家增强 ============
        private bool enableGodMode = false;
        private bool enableInfiniteStamina = false;
        private bool enableFlightNoClip = false;
        private bool enableInvisible = false;
        private bool enableSpeedHack = false;
        private float speedMult = 3f;
        private float jumpForce = 35f;
        private float flySpeed = 15f;

        // ============ ESP ============
        private bool enableEnemyESP = false;
        private bool enableLootESP = false;
        private bool enableItemESP = false;
        private bool enablePlayerESP = false;
        private bool enableVehicleESP = false;
        private float espMaxDistance = 80f;

        // ============ 传送系统 ============
        private Vector3? savedPos1, savedPos2, savedPos3;
        private int teleportTargetIdx = 0;
        private string[] teleportTargets = { "最近赃物", "最近存箱", "最近敌人", "最近载具", "最近队友" };

        // ============ 时间控制 ============
        private float targetTime = 0.5f;
        private bool freezeTime = false;

        // ============ 物品生成 ============
        private string[] cachedItemNames = null;
        private Vector2 itemScrollPos;
        private string itemFilter = "";
        private int itemCount = 1;
        private bool spawnIntoInventory = true;

        // ============ 性能缓存 ============
        private float lastCacheTime = 0f;
        private const float cacheInterval = 1.5f;
        private List<EnemyHealth> cachedEnemies = new List<EnemyHealth>();
        private List<GameEntityAI> cachedEnemiesAI = new List<GameEntityAI>();
        private List<GnomiumDeposit> cachedDeposits = new List<GnomiumDeposit>();
        private List<StealableObject> cachedLoot = new List<StealableObject>();
        private List<VehicleController> cachedVehicles = new List<VehicleController>();
        private List<PlayerNetworking> cachedPlayers = new List<PlayerNetworking>();
        private List<Grenade> cachedGrenades = new List<Grenade>();
        private List<Toilet> cachedToilets = new List<Toilet>();

        // ============ 反射字段 ============
        private FieldInfo staminaField;
        private FieldInfo godModeField;
        private FieldInfo baseSpeedField;
        private FieldInfo boostSpeedField;
        private Dictionary<int, float> origBaseSpeed = new Dictionary<int, float>();
        private Dictionary<int, float> origBoostSpeed = new Dictionary<int, float>();

        private void Start()
        {
            try { staminaField = typeof(CharacterActor).GetField("currentStamina", BindingFlags.NonPublic | BindingFlags.Instance); } catch { }
            try { godModeField = typeof(PlayerNetworking).GetField("godMode", BindingFlags.NonPublic | BindingFlags.Instance); } catch { }
            try
            {
                var planarFi = typeof(NormalMovement).GetField("planarMovementParameters", BindingFlags.Public | BindingFlags.Instance);
                if (planarFi != null)
                {
                    baseSpeedField = planarFi.FieldType.GetField("baseSpeedLimit");
                    boostSpeedField = planarFi.FieldType.GetField("boostSpeedLimit");
                }
            } catch { }
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Insert)) showMenu = !showMenu;

            if (Time.time - lastCacheTime > cacheInterval)
            {
                CacheEntities();
                lastCacheTime = Time.time;
            }

            if (freezeTime)
            {
                var prog = GameProgressionManager.Instance;
                if (prog != null) try { prog.SetGameTimeNormalized(targetTime); } catch { }
            }

            foreach (var player in cachedPlayers)
            {
                if (player == null) continue;
                if (!(player.IsOwner || player.IsLocalPlayer)) continue;

                if (godModeField != null) try { godModeField.SetValue(player, enableGodMode); } catch { }
                if (enableGodMode && player.Health != null && player.Health.Health < 100f)
                    try { player.Health.SelfDamageRpc(-99f); } catch { }

                if (enableInfiniteStamina && player.Actor != null && staminaField != null)
                    try { staminaField.SetValue(player.Actor, 100f); } catch { }

                ApplySpeedHack(player);

                if (Input.GetKeyDown(KeyCode.F4))
                {
                    enableInvisible = !enableInvisible;
                    try { player.ToggleGraphics(!enableInvisible); } catch { }
                }

                if (Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.V) && player.Actor != null)
                    player.Actor.LocalVerticalVelocity = new Vector3(0, jumpForce, 0);

                HandleFlight(player);
                HandleTeleportHotkeys(player);
            }
        }

        private void ApplySpeedHack(PlayerNetworking player)
        {
            if (baseSpeedField == null || boostSpeedField == null) return;
            var nm = player.GetComponentInChildren<NormalMovement>();
            if (nm == null) return;
            var pm = nm.planarMovementParameters;
            if (pm == null) return;
            int id = nm.GetInstanceID();

            if (enableSpeedHack)
            {
                try
                {
                    if (!origBaseSpeed.ContainsKey(id))
                    {
                        origBaseSpeed[id] = (float)baseSpeedField.GetValue(pm);
                        origBoostSpeed[id] = (float)boostSpeedField.GetValue(pm);
                    }
                    baseSpeedField.SetValue(pm, origBaseSpeed[id] * speedMult);
                    boostSpeedField.SetValue(pm, origBoostSpeed[id] * speedMult);
                } catch { }
            }
            else if (origBaseSpeed.ContainsKey(id))
            {
                try
                {
                    baseSpeedField.SetValue(pm, origBaseSpeed[id]);
                    boostSpeedField.SetValue(pm, origBoostSpeed[id]);
                    origBaseSpeed.Remove(id);
                    origBoostSpeed.Remove(id);
                } catch { }
            }
        }

        private void HandleFlight(PlayerNetworking player)
        {
            if (enableFlightNoClip)
            {
                if (player.Actor != null && player.Actor.enabled) player.Actor.enabled = false;
                var rb = player.GetComponent<Rigidbody>();
                if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
                foreach (var c in player.GetComponentsInChildren<Collider>()) c.isTrigger = true;

                if (Camera.main != null)
                {
                    Vector3 dir = Vector3.zero;
                    if (Input.GetKey(KeyCode.I)) dir += Camera.main.transform.forward;
                    if (Input.GetKey(KeyCode.K)) dir -= Camera.main.transform.forward;
                    if (Input.GetKey(KeyCode.J)) dir -= Camera.main.transform.right;
                    if (Input.GetKey(KeyCode.L)) dir += Camera.main.transform.right;
                    if (Input.GetKey(KeyCode.U)) dir += Vector3.up;
                    if (Input.GetKey(KeyCode.O)) dir -= Vector3.up;
                    float fs = Input.GetKey(KeyCode.LeftShift) ? flySpeed * 3f : flySpeed;
                    player.transform.position += dir * fs * Time.deltaTime;
                }
            }
            else if (player.Actor != null && !player.Actor.enabled)
            {
                player.Actor.enabled = true;
                var rb = player.GetComponent<Rigidbody>();
                if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }
                foreach (var c in player.GetComponentsInChildren<Collider>())
                    if (c.name != "GroundTrigger") c.isTrigger = false;
            }
        }

        private void HandleTeleportHotkeys(PlayerNetworking player)
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (Input.GetKey(KeyCode.LeftShift) && savedPos1.HasValue) Teleport(player, savedPos1.Value);
                else savedPos1 = player.transform.position;
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (Input.GetKey(KeyCode.LeftShift) && savedPos2.HasValue) Teleport(player, savedPos2.Value);
                else savedPos2 = player.transform.position;
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                if (Input.GetKey(KeyCode.LeftShift) && savedPos3.HasValue) Teleport(player, savedPos3.Value);
                else savedPos3 = player.transform.position;
            }
        }

        private void Teleport(PlayerNetworking player, Vector3 pos)
        {
            try { player.TeleportRpc(pos, false, true); }
            catch { player.transform.position = pos; }
        }

        private Vector3 GetSelectedTeleportPos(PlayerNetworking player)
        {
            Vector3 p = player.transform.position;
            float bestDist = float.MaxValue;
            Vector3 best = p;
            switch (teleportTargetIdx)
            {
                case 0:
                    foreach (var l in cachedLoot)
                    {
                        if (l == null || l.carriedByBob) continue;
                        float d = Vector3.Distance(p, l.transform.position);
                        if (d < bestDist) { bestDist = d; best = l.transform.position; }
                    }
                    break;
                case 1:
                    foreach (var dp in cachedDeposits)
                    {
                        if (dp == null) continue;
                        float d = Vector3.Distance(p, dp.transform.position);
                        if (d < bestDist) { bestDist = d; best = dp.transform.position; }
                    }
                    break;
                case 2:
                    foreach (var e in cachedEnemies)
                    {
                        if (e == null || e.Dead) continue;
                        float d = Vector3.Distance(p, e.transform.position);
                        if (d < bestDist) { bestDist = d; best = e.transform.position; }
                    }
                    break;
                case 3:
                    foreach (var v in cachedVehicles)
                    {
                        if (v == null) continue;
                        float d = Vector3.Distance(p, v.transform.position);
                        if (d < bestDist) { bestDist = d; best = v.transform.position; }
                    }
                    break;
                case 4:
                    foreach (var pl in cachedPlayers)
                    {
                        if (pl == null || pl == player) continue;
                        float d = Vector3.Distance(p, pl.transform.position);
                        if (d < bestDist) { bestDist = d; best = pl.transform.position + Vector3.up * 1.5f; }
                    }
                    break;
            }
            return best;
        }

        private void CacheEntities()
        {
            cachedEnemies = FindObjectsOfType<EnemyHealth>().ToList();
            cachedEnemiesAI = FindObjectsOfType<GameEntityAI>().ToList();
            cachedDeposits = FindObjectsOfType<GnomiumDeposit>().ToList();
            cachedLoot = FindObjectsOfType<StealableObject>().ToList();
            cachedVehicles = FindObjectsOfType<VehicleController>().ToList();
            cachedPlayers = FindObjectsOfType<PlayerNetworking>().ToList();
            cachedGrenades = FindObjectsOfType<Grenade>().ToList();
            cachedToilets = FindObjectsOfType<Toilet>().ToList();
        }

        private PlayerNetworking GetLocalPlayer()
        {
            foreach (var p in cachedPlayers)
                if (p != null && (p.IsOwner || p.IsLocalPlayer)) return p;
            return null;
        }

        public void OnGUI()
        {
            if (enableEnemyESP || enableLootESP || enableItemESP || enablePlayerESP || enableVehicleESP) DrawESP();
            if (!showMenu) return;
            GUI.color = Color.white;
            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            menuRect = GUILayout.Window(8888, menuRect, DrawMenu, "Burglin' Gnomes 漏洞总线 v2 (Insert切换)");
            GUI.backgroundColor = Color.white;
        }

        private void DrawMenu(int windowID)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < tabs.Length; i++)
                if (GUILayout.Toggle(currentTab == i, tabs[i], "Button")) currentTab = i;
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            switch (currentTab)
            {
                case 0: DrawTabPlayer(); break;
                case 1: DrawTabTeleport(); break;
                case 2: DrawTabItems(); break;
                case 3: DrawTabMechanics(); break;
                case 4: DrawTabNetwork(); break;
                case 5: DrawTabTeammate(); break;
                case 6: DrawTabVisual(); break;
                case 7: DrawTabChaos(); break;
            }
            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Label("载荷已注入");
            if (GUILayout.Button("卸载", GUILayout.Width(80))) Loader.Unload();
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        private void DrawTabPlayer()
        {
            GUILayout.Label("◆ 核心增益");
            enableGodMode = GUILayout.Toggle(enableGodMode, "  上帝模式 (反射 godMode 字段+免费合成)");
            enableInfiniteStamina = GUILayout.Toggle(enableInfiniteStamina, "  无限体力 (反射 currentStamina)");
            enableFlightNoClip = GUILayout.Toggle(enableFlightNoClip, "  幽灵飞行+穿墙 (IJKL/U升 O降/Shift加速)");
            enableInvisible = GUILayout.Toggle(enableInvisible, "  隐身模式 (F4 切换)");
            if (GUILayout.Button("应用隐身设置", GUILayout.Height(22)))
            {
                var p = GetLocalPlayer();
                if (p != null) try { p.ToggleGraphics(!enableInvisible); } catch { }
            }

            GUILayout.Space(8);
            GUILayout.Label("◆ 速度与跳跃");
            enableSpeedHack = GUILayout.Toggle(enableSpeedHack, "  移动速度倍率 (反射 baseSpeedLimit)");
            GUILayout.Label($"  倍率: {speedMult:F1}x");
            speedMult = GUILayout.HorizontalSlider(speedMult, 1f, 8f);
            GUILayout.Label($"  超级跳跃 (V+Space): {jumpForce:F0}");
            jumpForce = GUILayout.HorizontalSlider(jumpForce, 10f, 80f);
            GUILayout.Label($"  飞行速度: {flySpeed:F0}");
            flySpeed = GUILayout.HorizontalSlider(flySpeed, 5f, 50f);

            GUILayout.Space(8);
            if (GUILayout.Button("立即满血 (RPC)", GUILayout.Height(28)))
            {
                var p = GetLocalPlayer();
                if (p != null) try { p.Health.SelfDamageRpc(-999f); } catch { }
            }
        }

        private void DrawTabTeleport()
        {
            GUILayout.Label("◆ 目标选择");
            teleportTargetIdx = GUILayout.SelectionGrid(teleportTargetIdx, teleportTargets, 2);

            GUILayout.Space(6);
            if (GUILayout.Button($"传送到「{teleportTargets[teleportTargetIdx]}」", GUILayout.Height(35)))
            {
                var p = GetLocalPlayer();
                if (p != null) Teleport(p, GetSelectedTeleportPos(p));
            }

            GUILayout.Space(10);
            GUILayout.Label("◆ 路径点 (F1/F2/F3 保存，Shift+F1/F2/F3 跳回)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"槽1 {(savedPos1.HasValue ? "✓" : "—")}")) { var p = GetLocalPlayer(); if (p != null) savedPos1 = p.transform.position; }
            if (GUILayout.Button("回跳1") && savedPos1.HasValue) { var p = GetLocalPlayer(); if (p != null) Teleport(p, savedPos1.Value); }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"槽2 {(savedPos2.HasValue ? "✓" : "—")}")) { var p = GetLocalPlayer(); if (p != null) savedPos2 = p.transform.position; }
            if (GUILayout.Button("回跳2") && savedPos2.HasValue) { var p = GetLocalPlayer(); if (p != null) Teleport(p, savedPos2.Value); }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"槽3 {(savedPos3.HasValue ? "✓" : "—")}")) { var p = GetLocalPlayer(); if (p != null) savedPos3 = p.transform.position; }
            if (GUILayout.Button("回跳3") && savedPos3.HasValue) { var p = GetLocalPlayer(); if (p != null) Teleport(p, savedPos3.Value); }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            if (GUILayout.Button("传送到摄像机指向位置 (10米外)", GUILayout.Height(28)))
            {
                var p = GetLocalPlayer();
                if (p != null && Camera.main != null)
                    Teleport(p, Camera.main.transform.position + Camera.main.transform.forward * 10f);
            }

            if (GUILayout.Button("万象天引：拉取所有赃物到面前", GUILayout.Height(28)))
            {
                if (Camera.main != null)
                {
                    Vector3 dest = Camera.main.transform.position + Camera.main.transform.forward * 2f;
                    foreach (var loot in cachedLoot)
                        if (loot != null && !loot.carriedByBob)
                            loot.transform.position = dest;
                }
            }
        }

        private string[] GetItemNames(InventoryBase inv)
        {
            if (cachedItemNames != null) return cachedItemNames;
            try
            {
                var allItems = inv.BoundItems;
                if (allItems == null || allItems.items == null) return new string[0];
                cachedItemNames = allItems.items.Where(it => it != null).Select(it => it.Name).ToArray();
            }
            catch { cachedItemNames = new string[0]; }
            return cachedItemNames;
        }

        private void SpawnItemForLocal(string itemName)
        {
            var p = GetLocalPlayer();
            if (p == null || p.Inventory == null) return;
            bool isHost = false;
            try { isHost = NetworkManager.Singleton != null && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost); } catch { }

            for (int i = 0; i < itemCount; i++)
            {
                Vector3 pos;
                try { pos = p.ItemDropPosition; }
                catch { pos = p.transform.position + Vector3.up * 0.8f; }

                if (isHost)
                {
                    // 主机直接调用：服务端权威生成 + NetworkObject.InstantiateAndSpawn
                    try
                    {
                        var inst = p.Inventory.SpawnItem(itemName, pos);
                        if (spawnIntoInventory && inst != null)
                            try { p.Inventory.TryAddItem(inst); } catch { }
                    }
                    catch { }
                }
                else
                {
                    // 客户端：通过 PlayerWantsToTakeItemRpc 在自己背包"凭空"添加（部分物品可能拒绝）
                    try
                    {
                        int idx;
                        var data = p.Inventory.GetItemData(itemName, out idx);
                        if (data != null && idx >= 0)
                            p.Inventory.PlayerWantsToTakeItemRpc(idx);
                    }
                    catch { }
                }
            }
        }

        private void DrawTabItems()
        {
            var p = GetLocalPlayer();
            if (p == null || p.Inventory == null)
            {
                GUILayout.Label("◇ 等待本地玩家与背包初始化...");
                return;
            }

            bool isHost = false;
            try { isHost = NetworkManager.Singleton != null && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost); } catch { }
            GUILayout.Label(isHost ? "● 主机模式：直接 SpawnItem (推荐)" : "○ 客户端模式：仅 PlayerWantsToTakeItemRpc 通道 (受限)");

            GUILayout.Space(4);
            spawnIntoInventory = GUILayout.Toggle(spawnIntoInventory, "  生成后自动收入背包 (仅主机)");
            GUILayout.Label($"  数量: {itemCount}");
            itemCount = (int)GUILayout.HorizontalSlider(itemCount, 1, 99);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("过滤:", GUILayout.Width(40));
            itemFilter = GUILayout.TextField(itemFilter ?? "", GUILayout.Width(180));
            if (GUILayout.Button("刷新清单", GUILayout.Width(80))) cachedItemNames = null;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            string[] names = GetItemNames(p.Inventory);
            GUILayout.Label($"◆ 物品库 ({names.Length} 项)");

            itemScrollPos = GUILayout.BeginScrollView(itemScrollPos, GUILayout.Height(280));
            string filt = (itemFilter ?? "").Trim().ToLower();
            int shown = 0;
            foreach (var name in names)
            {
                if (name == null) continue;
                if (filt.Length > 0 && !name.ToLower().Contains(filt)) continue;
                if (GUILayout.Button(name, GUILayout.Height(22)))
                    SpawnItemForLocal(name);
                shown++;
                if (shown > 200) { GUILayout.Label("...更多请用过滤"); break; }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            GUILayout.Label("◆ 快捷批量");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("全物品各 1 个"))
            {
                int saved = itemCount; itemCount = 1;
                foreach (var n in names) if (n != null) SpawnItemForLocal(n);
                itemCount = saved;
            }
            if (GUILayout.Button("清空背包"))
            {
                try
                {
                    for (byte i = 0; i < 30; i++) try { p.Inventory.DropItemRpc(i); } catch { }
                }
                catch { }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("◆ 资源储量直接修改 (主机有效)");
            if (GUILayout.Button("将所有 GnomiumDeposit 储量+9999", GUILayout.Height(26)))
            {
                foreach (var d in cachedDeposits)
                {
                    if (d == null) continue;
                    try
                    {
                        var fi = typeof(GnomiumDeposit).GetField("depositedGnomium", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (fi != null) fi.SetValue(d, ((int)fi.GetValue(d)) + 9999);
                    } catch { }
                }
            }
        }

        private void DrawTabMechanics()
        {
            GUILayout.Label("◆ 时间与世界");
            GUILayout.Label($"  目标时间: {targetTime:F2} (0=黎明 0.5=正午 1=午夜)");
            targetTime = GUILayout.HorizontalSlider(targetTime, 0f, 1f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("设置时间")) { var prog = GameProgressionManager.Instance; if (prog != null) try { prog.SetGameTimeNormalized(targetTime); } catch { } }
            freezeTime = GUILayout.Toggle(freezeTime, " 锁定时间");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("开局")) { var prog = GameProgressionManager.Instance; if (prog != null) try { prog.StartGameRpc(); } catch { } }
            if (GUILayout.Button("结束本局")) { var prog = GameProgressionManager.Instance; if (prog != null) try { prog.EndGameRpc(); } catch { } }
            if (GUILayout.Button("重置游戏")) { var prog = GameProgressionManager.Instance; if (prog != null) try { prog.ResetGame(); } catch { } }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("◆ 任务系统");
            if (GUILayout.Button("一键完成所有任务 (DebugCompleteTask)", GUILayout.Height(30)))
            {
                var taskMgr = FindObjectOfType<PlayerTaskManager>();
                if (taskMgr != null)
                {
                    try
                    {
                        var tasks = taskMgr.CurrentTasks;
                        for (int i = 0; i < tasks.Length; i++)
                            try { taskMgr.DebugCompleteTask(i); } catch { }
                    } catch { }
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("◆ AI 控制");
            if (GUILayout.Button("全图 AI 临时停机 (enabled=false)", GUILayout.Height(28)))
            {
                foreach (var ai in cachedEnemiesAI) if (ai != null) ai.enabled = false;
            }
            if (GUILayout.Button("永久 Despawn 所有敌人 (AiDirector)", GUILayout.Height(28)))
            {
                var dir = FindObjectOfType<AiDirector>();
                if (dir != null)
                {
                    foreach (var e in cachedEnemies)
                    {
                        if (e == null) continue;
                        var ge = e.GetComponent<GameEntityBase>();
                        if (ge != null) try { dir.DespawnEnemy(ge); } catch { }
                    }
                }
            }
            if (GUILayout.Button("捆绑全图敌人 (SetTiedRpc)", GUILayout.Height(28)))
            {
                foreach (var e in cachedEnemies)
                {
                    if (e == null) continue;
                    var seh = e.GetComponentInChildren<StatusEffectHandler>();
                    if (seh != null) try { seh.SetTiedRpc(true); } catch { }
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("◆ 载具改装");
            if (GUILayout.Button("将所有载具调到极速 (maxSpeed=9999)", GUILayout.Height(28)))
            {
                foreach (var car in cachedVehicles)
                    if (car != null) { car.maxSpeed = 9999f; car.maxMotorTorque = 25000f; }
            }
        }

        private void DrawTabNetwork()
        {
            GUILayout.Label("◆ RPC Everyone 漏洞利用");

            if (GUILayout.Button("秒杀全图怪物 (TakeDamageRpc)", GUILayout.Height(30)))
            {
                foreach (var e in cachedEnemies)
                    try { if (!e.Dead) e.TakeDamageRpc(99999f); } catch { }
            }

            if (GUILayout.Button("一键提交所有储量到大本营", GUILayout.Height(30)))
            {
                foreach (var d in cachedDeposits)
                    try { d.DepositResourcesToStockpileRpc(); } catch { }
            }

            if (GUILayout.Button("强制结束本局 (EndGameRpc)", GUILayout.Height(28)))
            {
                var prog = FindObjectOfType<GameProgressionManager>();
                if (prog != null) try { prog.EndGameRpc(); } catch { }
            }

            GUILayout.Space(8);
            GUILayout.Label("◆ 玩家间漏洞 (谨慎！)");
            if (GUILayout.Button("【偷】队友物品空手套白狼", GUILayout.Height(28)))
            {
                var me = GetLocalPlayer();
                if (me != null && me.Inventory != null)
                {
                    foreach (var p in cachedPlayers)
                    {
                        if (p == null || p == me || p.Inventory == null) continue;
                        for (byte src = 0; src < 15; src++)
                            for (byte dst = 0; dst < 15; dst++)
                            {
                                try
                                {
                                    var srcRef = new NetworkBehaviourReference(p.Inventory);
                                    me.Inventory.PerformItemTransactionRpc(dst, srcRef, src);
                                } catch { }
                            }
                    }
                }
            }

            if (GUILayout.Button("【恶搞】肢解所有非本机玩家", GUILayout.Height(28)))
            {
                foreach (var p in cachedPlayers)
                {
                    if (p == null || p.IsLocalPlayer || p.IsOwner) continue;
                    try
                    {
                        p.Dismemberment.DismemberRpc((DismembermentController.DismemberSection.DismemberPart)(-1), 8f, p.transform.position, 30f, 5f);
                        p.Health.TakeDamageRpc(99999f);
                    } catch { }
                }
            }

            if (GUILayout.Button("【恶搞】强制丢光所有玩家背包", GUILayout.Height(28)))
            {
                foreach (var p in cachedPlayers)
                {
                    if (p == null || p.IsLocalPlayer || p.IsOwner) continue;
                    for (byte i = 0; i < 15; i++) try { p.Inventory.DropItemRpc(i); } catch { }
                }
            }
        }

        private void DrawTabTeammate()
        {
            GUILayout.Label("◆ 救助队友 (RespawnRpc Everyone)");

            if (GUILayout.Button("一键复活所有死亡队友 (RespawnRpc)", GUILayout.Height(32)))
            {
                foreach (var p in cachedPlayers)
                {
                    if (p == null || p.Health == null) continue;
                    if (p.Health.Dead) try { p.Health.RespawnRpc(); } catch { }
                }
            }

            if (GUILayout.Button("一键满血所有队友", GUILayout.Height(28)))
            {
                foreach (var p in cachedPlayers)
                {
                    if (p == null || p.Health == null) continue;
                    try { p.Health.SelfDamageRpc(-999f); } catch { }
                }
            }

            if (GUILayout.Button("解除所有队友捆绑状态", GUILayout.Height(28)))
            {
                foreach (var p in cachedPlayers)
                {
                    if (p == null) continue;
                    var seh = p.StatusEffects;
                    if (seh != null) try { seh.SetTiedRpc(false); } catch { }
                }
            }

            if (GUILayout.Button("呼叫医疗终端复活全员 (RespawnPlayersRpc)", GUILayout.Height(28)))
            {
                var med = FindObjectOfType<MedicalTerminal>();
                if (med != null)
                {
                    var mi = typeof(MedicalTerminal).GetMethod("RespawnPlayersRpc", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (mi != null) try { mi.Invoke(med, null); } catch { }
                }
            }
        }

        private void DrawTabVisual()
        {
            GUILayout.Label("◆ ESP 透视");
            enableEnemyESP = GUILayout.Toggle(enableEnemyESP, "  敌人透视 (HP+距离)");
            enableLootESP = GUILayout.Toggle(enableLootESP, "  存箱透视");
            enableItemESP = GUILayout.Toggle(enableItemESP, "  赃物透视");
            enablePlayerESP = GUILayout.Toggle(enablePlayerESP, "  队友透视");
            enableVehicleESP = GUILayout.Toggle(enableVehicleESP, "  载具透视");
            GUILayout.Label($"  最大显示距离: {espMaxDistance:F0}m");
            espMaxDistance = GUILayout.HorizontalSlider(espMaxDistance, 20f, 300f);
        }

        private void DrawTabChaos()
        {
            GUILayout.Label("◆ 混沌引擎 (谨慎使用)");

            if (GUILayout.Button("引爆全图所有手雷 (InstantExplodeRpc)", GUILayout.Height(30)))
            {
                foreach (var g in cachedGrenades)
                    if (g != null) try { g.InstantExplodeRpc(); } catch { }
            }

            if (GUILayout.Button("点燃全图所有手雷引线 (StartFuseRpc)", GUILayout.Height(28)))
            {
                foreach (var g in cachedGrenades)
                    if (g != null) try { g.StartFuseRpc(); } catch { }
            }

            if (GUILayout.Button("洪水：触发所有马桶喷涌 (PlayFloodRpc)", GUILayout.Height(28)))
            {
                foreach (var t in cachedToilets)
                {
                    if (t == null) continue;
                    var mi = typeof(Toilet).GetMethod("PlayFloodRpc", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (mi != null) try { mi.Invoke(t, null); } catch { }
                }
            }

            if (GUILayout.Button("启动所有龙卷风传送门 (StartVortexRpc)", GUILayout.Height(28)))
            {
                foreach (var h in FindObjectsOfType<GnomeHouse>())
                {
                    if (h == null) continue;
                    try { h.StartVortexRpc(h.transform.position, 1); } catch { }
                }
            }

            if (GUILayout.Button("捆绑全图玩家 (真.团灭)", GUILayout.Height(28)))
            {
                foreach (var p in cachedPlayers)
                {
                    if (p == null || p.IsLocalPlayer) continue;
                    var seh = p.StatusEffects;
                    if (seh != null) try { seh.SetTiedRpc(true); } catch { }
                }
            }

            if (GUILayout.Button("毁灭所有花园侏儒 (DestroyGnomeRpc)", GUILayout.Height(28)))
            {
                foreach (var g in FindObjectsOfType<GardenGnome>())
                {
                    if (g == null) continue;
                    var mi = typeof(GardenGnome).GetMethod("DestroyGnomeRpc", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (mi != null) try { mi.Invoke(g, null); } catch { }
                }
            }
        }

        private void DrawESP()
        {
            if (Camera.main == null) return;
            Vector3 camPos = Camera.main.transform.position;

            if (enableEnemyESP)
            {
                foreach (var enemy in cachedEnemies)
                {
                    if (enemy == null || enemy.Dead) continue;
                    Vector3 pos = enemy.transform.position;
                    float dist = Vector3.Distance(camPos, pos);
                    if (dist > espMaxDistance) continue;
                    Vector3 w2s = Camera.main.WorldToScreenPoint(pos);
                    if (w2s.z <= 0f) continue;
                    GUI.color = Color.red;
                    GUI.Label(new Rect(w2s.x - 25, Screen.height - w2s.y, 110, 40), $"[敌] {dist:F0}m\nHP:{(int)enemy.Health}");
                }
            }

            if (enableLootESP)
            {
                foreach (var d in cachedDeposits)
                {
                    if (d == null) continue;
                    Vector3 pos = d.transform.position;
                    float dist = Vector3.Distance(camPos, pos);
                    if (dist > espMaxDistance) continue;
                    Vector3 w2s = Camera.main.WorldToScreenPoint(pos);
                    if (w2s.z <= 0f) continue;
                    GUI.color = Color.cyan;
                    GUI.Label(new Rect(w2s.x - 30, Screen.height - w2s.y, 110, 40), $"[存箱] {dist:F0}m\n储量:{d.DepositedGnomium}");
                }
            }

            if (enableItemESP)
            {
                foreach (var loot in cachedLoot)
                {
                    if (loot == null || loot.carriedByBob) continue;
                    Vector3 pos = loot.transform.position;
                    float dist = Vector3.Distance(camPos, pos);
                    if (dist > espMaxDistance) continue;
                    Vector3 w2s = Camera.main.WorldToScreenPoint(pos);
                    if (w2s.z <= 0f) continue;
                    GUI.color = Color.yellow;
                    GUI.Label(new Rect(w2s.x - 30, Screen.height - w2s.y, 110, 40), $"[赃] {dist:F0}m\n{loot.ItemType}");
                }
            }

            if (enablePlayerESP)
            {
                foreach (var p in cachedPlayers)
                {
                    if (p == null || p.IsLocalPlayer) continue;
                    Vector3 pos = p.transform.position;
                    float dist = Vector3.Distance(camPos, pos);
                    if (dist > espMaxDistance) continue;
                    Vector3 w2s = Camera.main.WorldToScreenPoint(pos);
                    if (w2s.z <= 0f) continue;
                    GUI.color = Color.green;
                    string hp = p.Health != null ? ((int)p.Health.Health).ToString() : "?";
                    GUI.Label(new Rect(w2s.x - 30, Screen.height - w2s.y, 110, 40), $"[队] {dist:F0}m\nHP:{hp}");
                }
            }

            if (enableVehicleESP)
            {
                foreach (var v in cachedVehicles)
                {
                    if (v == null) continue;
                    Vector3 pos = v.transform.position;
                    float dist = Vector3.Distance(camPos, pos);
                    if (dist > espMaxDistance) continue;
                    Vector3 w2s = Camera.main.WorldToScreenPoint(pos);
                    if (w2s.z <= 0f) continue;
                    GUI.color = new Color(1f, 0.5f, 0f);
                    GUI.Label(new Rect(w2s.x - 25, Screen.height - w2s.y, 100, 25), $"[车] {dist:F0}m");
                }
            }
        }
    }
}
