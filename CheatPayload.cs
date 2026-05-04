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
        private Rect menuRect = new Rect(30, 30, 720, 500); // 加宽，适合侧边栏
        private Vector2 scrollPos;
        private int currentTab = 0;
        private string[] tabs = { "玩家 (Player)", "传送 (Teleport)", "物品 (Items)", "机制 (Logic)", "网络 (Network)", "队友 (Team)", "视觉 (Visual)", "混沌 (Chaos)" };
        private bool _isDragging = false;
        private Vector2 _dragOffset;

        // ============ 样式系统 ============
        private bool _stylesReady = false;
        private GUIStyle sWin, sTabOn, sTabOff, sBtn, sBtnSm, sTogOn, sTogOff;
        private GUIStyle sLbl, sSec, sInput, sDimLbl;
        private Texture2D txBg, txSidebar, txTabOn, txTabOff, txTabHov;
        private Texture2D txBtn, txBtnHov, txBtnAct, txTogOn, txTogOff, txStatus;
        private Texture2D txAccent;

        private Texture2D Tex(int r, int g, int b, int a = 255)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, new Color(r / 255f, g / 255f, b / 255f, a / 255f));
            t.Apply();
            return t;
        }

        private GUIStyle CloneBtn(Texture2D norm, Texture2D hov, Texture2D act, Color textNorm, int fs = 12)
        {
            var s = new GUIStyle(GUI.skin.button)
            {
                fontSize = fs,
                padding = new RectOffset(10, 10, 6, 6),
                border = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleLeft // 左对齐更具现代感
            };
            s.normal.background = norm;   s.normal.textColor = textNorm;
            s.hover.background  = hov;    s.hover.textColor  = Color.white;
            s.active.background = act;    s.active.textColor = new Color(0.8f, 0.95f, 1f);
            s.onNormal.background = norm; s.onNormal.textColor = textNorm;
            return s;
        }

        private void BuildStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            // 极简玻璃拟态色盘 (Acrylic Dark Theme)
            txBg      = Tex(12, 12, 16, 235);    // 主背景，深邃半透明
            txSidebar = Tex(8, 8, 11, 245);      // 侧边栏更深
            txTabOn   = Tex(30, 35, 45, 200);    // 当前选中 Tab
            txTabOff  = Tex(0, 0, 0, 0);         // 未选中全透明
            txTabHov  = Tex(20, 25, 35, 150);    // 鼠标悬停

            txBtn     = Tex(22, 26, 38, 200);   // 常规按钮
            txBtnHov  = Tex(35, 45, 65, 220);   // 按钮悬停
            txBtnAct  = Tex(0, 140, 255, 220);   // 亮蓝按下状态

            txTogOn   = Tex(0, 175, 240, 200);   // 开关激活，赛博蓝
            txTogOff  = Tex(20, 22, 30, 180);    // 开关关闭，暗灰

            txStatus  = Tex(6, 6, 8, 250);       // 底部状态栏
            txAccent  = Tex(0, 180, 255, 255);   // 主题强调色(细线使用)

            // 文字颜色
            var cText   = new Color(0.85f, 0.9f, 0.95f);
            var cDim    = new Color(0.55f, 0.6f, 0.7f);
            var cAccent = new Color(0.0f, 0.85f, 1f);

            sWin = new GUIStyle(GUI.skin.box);
            sWin.normal.background = txBg;
            sWin.border = new RectOffset(0, 0, 0, 0);

            sTabOn  = CloneBtn(txTabOn,  txTabOn,  txTabOn,  Color.white, 13);
            sTabOn.fontStyle = FontStyle.Bold;
            sTabOff = CloneBtn(txTabOff, txTabHov, txTabOn,  cDim, 13);
            
            sBtn   = CloneBtn(txBtn, txBtnHov, txBtnAct, cText, 12);
            sBtn.alignment = TextAnchor.MiddleCenter; // 普通按钮居中
            
            sBtnSm = CloneBtn(txBtn, txBtnHov, txBtnAct, cText, 11);
            sBtnSm.alignment = TextAnchor.MiddleCenter;
            sBtnSm.padding = new RectOffset(5, 5, 4, 4);

            sTogOn  = CloneBtn(txTogOn,  txBtnHov, txBtnAct, new Color(0.05f, 0.1f, 0.15f), 12);
            sTogOn.fontStyle  = FontStyle.Bold;
            sTogOff = CloneBtn(txTogOff, txBtnHov, txTogOn,  cDim, 12);

            sSec = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            sSec.normal.textColor = cAccent;
            sSec.padding = new RectOffset(4, 0, 8, 4);

            sLbl = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            sLbl.normal.textColor = cText;
            sLbl.padding = new RectOffset(6, 0, 2, 2);

            sDimLbl = new GUIStyle(sLbl);
            sDimLbl.normal.textColor = cDim;
            sDimLbl.fontSize = 11;

            sInput = new GUIStyle(GUI.skin.textField) { fontSize = 12 };
            sInput.normal.background  = Tex(18, 20, 30, 200);
            sInput.focused.background = Tex(30, 35, 50, 220);
            sInput.normal.textColor  = cText;
            sInput.focused.textColor = Color.white;
            sInput.padding = new RectOffset(8, 8, 6, 6);
        }

        // Helper: 按钮式 Toggle，返回新状态
        private bool Tog(bool val, string label, params GUILayoutOption[] opts)
        {
            string prefix = val ? " ✔ " : " ◯ ";
            if (GUILayout.Button(prefix + label, val ? sTogOn : sTogOff, opts))
                return !val;
            return val;
        }

        // Helper: 区块标题 (下方带点缀边距)
        private void Sec(string text)
        {
            GUILayout.Space(8);
            GUILayout.Label(" " + text, sSec);
            GUILayout.Space(2);
        }

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
            BuildStyles();
            GUI.color = Color.white;

            if (enableEnemyESP || enableLootESP || enableItemESP || enablePlayerESP || enableVehicleESP)
                DrawESP();
            if (!showMenu) return;

            Event e = Event.current;

            // ── 拖拽处理 (保留顶部拖拽区域) ─────────────────────
            Rect titleBarRect = new Rect(menuRect.x, menuRect.y, menuRect.width - 60, 36);
            if (e.type == EventType.MouseDown && e.button == 0 && titleBarRect.Contains(e.mousePosition))
            {
                _isDragging  = true;
                _dragOffset  = e.mousePosition - new Vector2(menuRect.x, menuRect.y);
                e.Use();
            }
            if (e.type == EventType.MouseUp)   _isDragging = false;
            if (_isDragging && e.type == EventType.MouseDrag)
            {
                menuRect.x = e.mousePosition.x - _dragOffset.x;
                menuRect.y = e.mousePosition.y - _dragOffset.y;
                e.Use();
            }
            menuRect.x = Mathf.Clamp(menuRect.x, -menuRect.width + 100, Screen.width  - 50);
            menuRect.y = Mathf.Clamp(menuRect.y, -menuRect.height + 100, Screen.height - 50);

            // ── 主窗口与侧边栏背景 ────────────────────────────
            GUI.Box(menuRect, GUIContent.none, sWin);
            
            float sidebarWidth = 140f;
            Rect sidebarRect = new Rect(menuRect.x, menuRect.y, sidebarWidth, menuRect.height);
            GUI.DrawTexture(sidebarRect, txSidebar);
            
            // 霓虹侧边栏分割线
            GUI.DrawTexture(new Rect(sidebarRect.xMax, menuRect.y, 1, menuRect.height), txAccent);

            // ── 标题栏 (左侧) ─────────────────────────────────
            GUI.Label(new Rect(menuRect.x + 12, menuRect.y + 12, sidebarWidth - 20, 24), "⚡ Gnomium", sSec);

            if (GUI.Button(new Rect(menuRect.xMax - 54, menuRect.y + 8, 24, 24), "➖", sBtnSm))
                showMenu = false;
            if (GUI.Button(new Rect(menuRect.xMax - 28, menuRect.y + 8, 24, 24), "╳", sBtnSm))
                Loader.Unload();

            // ── 左侧 Tab 栏 ───────────────────────────────────
            GUILayout.BeginArea(new Rect(menuRect.x, menuRect.y + 46, sidebarWidth, menuRect.height - 50));
            for (int i = 0; i < tabs.Length; i++)
            {
                if (GUILayout.Button($"{(i == currentTab ? " ■ " : "   ")}{tabs[i]}", i == currentTab ? sTabOn : sTabOff, GUILayout.Height(38)))
                    currentTab = i;
            }
            GUILayout.EndArea();

            // ── 右侧内容区 (ScrollView) ───────────────────────
            Rect content = new Rect(menuRect.x + sidebarWidth + 8, menuRect.y + 36, menuRect.width - sidebarWidth - 16, menuRect.height - 66);
            GUILayout.BeginArea(content);
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, false,
                GUIStyle.none, GUI.skin.verticalScrollbar,
                GUILayout.Width(content.width), GUILayout.Height(content.height));
            switch (currentTab)
            {
                case 0: DrawTabPlayer();    break;
                case 1: DrawTabTeleport();  break;
                case 2: DrawTabItems();     break;
                case 3: DrawTabMechanics(); break;
                case 4: DrawTabNetwork();   break;
                case 5: DrawTabTeammate();  break;
                case 6: DrawTabVisual();    break;
                case 7: DrawTabChaos();     break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // ── 右侧状态栏 (底部) ─────────────────────────────
            Rect status = new Rect(menuRect.x + sidebarWidth + 1, menuRect.yMax - 26, menuRect.width - sidebarWidth - 1, 26);
            GUI.DrawTexture(status, txStatus);
            GUI.Label(new Rect(status.x + 10, status.y + 4, status.width - 20, 18),
                $"◈ 注入就绪  |  {cachedEnemies.Count} 敌  {cachedLoot.Count} 赃物  {cachedPlayers.Count} 玩家  |  [Insert] 隐藏",
                sDimLbl);
        }

        private void DrawTabPlayer()
        {
            Sec("◆ 核心增益");
            enableGodMode         = Tog(enableGodMode,         "上帝模式  (反射 godMode 字段)");
            enableInfiniteStamina = Tog(enableInfiniteStamina, "无限体力  (反射 currentStamina)");
            enableFlightNoClip    = Tog(enableFlightNoClip,    "幽灵飞行 + 穿墙  (IJKL / U↑ O↓ / Shift加速)");
            enableInvisible       = Tog(enableInvisible,       "隐身模式  (F4 切换)");
            if (GUILayout.Button("应用隐身设置", sBtn, GUILayout.Height(26)))
            {
                var p = GetLocalPlayer();
                if (p != null) try { p.ToggleGraphics(!enableInvisible); } catch { }
            }

            Sec("◆ 速度与跳跃");
            enableSpeedHack = Tog(enableSpeedHack, "移动速度倍率  (反射 baseSpeedLimit)");
            GUILayout.Label($"   倍率: {speedMult:F1}x", sLbl);
            speedMult = GUILayout.HorizontalSlider(speedMult, 1f, 8f);
            GUILayout.Label($"   超级跳跃力度 (V+Space): {jumpForce:F0}", sLbl);
            jumpForce = GUILayout.HorizontalSlider(jumpForce, 10f, 80f);
            GUILayout.Label($"   飞行速度: {flySpeed:F0}", sLbl);
            flySpeed = GUILayout.HorizontalSlider(flySpeed, 5f, 50f);

            GUILayout.Space(8);
            if (GUILayout.Button("  ♥  立即满血 (SelfDamageRpc)", sBtn, GUILayout.Height(30)))
            {
                var p = GetLocalPlayer();
                if (p != null) try { p.Health.SelfDamageRpc(-999f); } catch { }
            }
        }

        private void DrawTabTeleport()
        {
            Sec("◆ 目标选择");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < teleportTargets.Length; i++)
            {
                bool sel = i == teleportTargetIdx;
                if (GUILayout.Button(teleportTargets[i], sel ? sTabOn : sTabOff))
                    teleportTargetIdx = i;
                if (i == 1) { GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            if (GUILayout.Button($"  ➤  传送到「{teleportTargets[teleportTargetIdx]}」", sBtn, GUILayout.Height(34)))
            {
                var p = GetLocalPlayer();
                if (p != null) Teleport(p, GetSelectedTeleportPos(p));
            }

            Sec("◆ 路径点 (F1/F2/F3 保存，Shift+F1/F2/F3 跳回)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"  📍  槽1 {(savedPos1.HasValue ? "✓" : "—")}", sBtnSm)) { var p = GetLocalPlayer(); if (p != null) savedPos1 = p.transform.position; }
            if (GUILayout.Button("  ⤷  跳回槽1", sBtnSm) && savedPos1.HasValue) { var p = GetLocalPlayer(); if (p != null) Teleport(p, savedPos1.Value); }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"  📍  槽2 {(savedPos2.HasValue ? "✓" : "—")}", sBtnSm)) { var p = GetLocalPlayer(); if (p != null) savedPos2 = p.transform.position; }
            if (GUILayout.Button("  ⤷  跳回槽2", sBtnSm) && savedPos2.HasValue) { var p = GetLocalPlayer(); if (p != null) Teleport(p, savedPos2.Value); }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"  📍  槽3 {(savedPos3.HasValue ? "✓" : "—")}", sBtnSm)) { var p = GetLocalPlayer(); if (p != null) savedPos3 = p.transform.position; }
            if (GUILayout.Button("  ⤷  跳回槽3", sBtnSm) && savedPos3.HasValue) { var p = GetLocalPlayer(); if (p != null) Teleport(p, savedPos3.Value); }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            if (GUILayout.Button("  🎯  传送到摄像机指向位置 (10米外)", sBtn, GUILayout.Height(28)))
            {
                var p = GetLocalPlayer();
                if (p != null && Camera.main != null)
                    Teleport(p, Camera.main.transform.position + Camera.main.transform.forward * 10f);
            }

            if (GUILayout.Button("  ☄  万象天引：拉取所有赃物到面前", sBtn, GUILayout.Height(28)))
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
            GUILayout.Label(isHost ? "  ● 主机模式：直接 SpawnItem (推荐)" : "  ○ 客户端模式：仅 RPC 通道 (受限)", isHost ? sSec : sDimLbl);

            GUILayout.Space(4);
            spawnIntoInventory = Tog(spawnIntoInventory, "生成后自动收入背包 (仅主机)");
            GUILayout.Label($"   数量: {itemCount}", sLbl);
            itemCount = (int)GUILayout.HorizontalSlider(itemCount, 1, 99);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("  过滤:", sLbl, GUILayout.Width(50));
            itemFilter = GUILayout.TextField(itemFilter ?? "", sInput);
            if (GUILayout.Button("刷新", sBtnSm, GUILayout.Width(52))) cachedItemNames = null;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            string[] names = GetItemNames(p.Inventory);
            Sec($"◆ 物品库 ({names.Length} 项)");

            itemScrollPos = GUILayout.BeginScrollView(itemScrollPos, GUILayout.Height(260));
            string filt = (itemFilter ?? "").Trim().ToLower();
            int shown = 0;
            foreach (var name in names)
            {
                if (name == null) continue;
                if (filt.Length > 0 && !name.ToLower().Contains(filt)) continue;
                if (GUILayout.Button(name, sBtnSm, GUILayout.Height(22)))
                    SpawnItemForLocal(name);
                shown++;
                if (shown > 200) { GUILayout.Label("  ...更多请用过滤", sDimLbl); break; }
            }
            GUILayout.EndScrollView();

            Sec("◆ 快捷批量");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("全物品各 1 个", sBtn))
            {
                int saved = itemCount; itemCount = 1;
                foreach (var n in names) if (n != null) SpawnItemForLocal(n);
                itemCount = saved;
            }
            if (GUILayout.Button("清空背包", sBtn))
            {
                try
                {
                    for (byte i = 0; i < 30; i++) try { p.Inventory.DropItemRpc(i); } catch { }
                }
                catch { }
            }
            GUILayout.EndHorizontal();

            Sec("◆ 资源储量直接修改 (主机有效)");
            if (GUILayout.Button("  ⬆  将所有 GnomiumDeposit 储量 +9999", sBtn, GUILayout.Height(26)))
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
            Sec("◆ 时间与世界");
            GUILayout.Label($"   目标时间: {targetTime:F2}   (0=黎明  0.5=正午  1=午夜)", sLbl);
            targetTime = GUILayout.HorizontalSlider(targetTime, 0f, 1f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("设置时间", sBtn)) { var prog = GameProgressionManager.Instance; if (prog != null) try { prog.SetGameTimeNormalized(targetTime); } catch { } }
            freezeTime = Tog(freezeTime, "锁定时间");
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("▶ 开局",  sBtn)) { var prog = GameProgressionManager.Instance; if (prog != null) try { prog.StartGameRpc(); } catch { } }
            if (GUILayout.Button("■ 结束",  sBtn)) { var prog = GameProgressionManager.Instance; if (prog != null) try { prog.EndGameRpc(); } catch { } }
            if (GUILayout.Button("↺ 重置",  sBtn)) { var prog = GameProgressionManager.Instance; if (prog != null) try { prog.ResetGame(); } catch { } }
            GUILayout.EndHorizontal();

            Sec("◆ 任务系统");
            if (GUILayout.Button("  ✔  一键完成所有任务 (DebugCompleteTask)", sBtn, GUILayout.Height(30)))
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

            Sec("◆ AI 控制");
            if (GUILayout.Button("  ⏸  全图 AI 临时停机", sBtn, GUILayout.Height(28)))
            {
                foreach (var ai in cachedEnemiesAI) if (ai != null) ai.enabled = false;
            }
            if (GUILayout.Button("  ✕  永久 Despawn 所有敌人 (AiDirector)", sBtn, GUILayout.Height(28)))
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
            if (GUILayout.Button("  ⛓  捆绑全图敌人 (SetTiedRpc)", sBtn, GUILayout.Height(28)))
            {
                foreach (var e in cachedEnemies)
                {
                    if (e == null) continue;
                    var seh = e.GetComponentInChildren<StatusEffectHandler>();
                    if (seh != null) try { seh.SetTiedRpc(true); } catch { }
                }
            }

            Sec("◆ 载具改装");
            if (GUILayout.Button("  🚗  极速改装所有载具 (maxSpeed=9999)", sBtn, GUILayout.Height(28)))
            {
                foreach (var car in cachedVehicles)
                    if (car != null) { car.maxSpeed = 9999f; car.maxMotorTorque = 25000f; }
            }
        }

        private void DrawTabNetwork()
        {
            Sec("◆ RPC Everyone 漏洞利用");
            if (GUILayout.Button("  ⚔  秒杀全图怪物 (TakeDamageRpc)", sBtn, GUILayout.Height(30)))
            {
                foreach (var e in cachedEnemies)
                    try { if (!e.Dead) e.TakeDamageRpc(99999f); } catch { }
            }

            if (GUILayout.Button("  📦  一键提交所有储量到大本营", sBtn, GUILayout.Height(30)))
            {
                foreach (var d in cachedDeposits)
                    try { d.DepositResourcesToStockpileRpc(); } catch { }
            }
            if (GUILayout.Button("  ■  强制结束本局 (EndGameRpc)", sBtn, GUILayout.Height(28)))
            {
                var prog = FindObjectOfType<GameProgressionManager>();
                if (prog != null) try { prog.EndGameRpc(); } catch { }
            }

            Sec("◆ 玩家间漏洞 (谨慎！)");
            if (GUILayout.Button("  💰  【偷】队友物品空手套白狼", sBtn, GUILayout.Height(28)))
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

            if (GUILayout.Button("  💀  【恶搞】肢解所有非本机玩家", sBtn, GUILayout.Height(28)))
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

            if (GUILayout.Button("  🎒  【恶搞】强制丢光所有玩家背包", sBtn, GUILayout.Height(28)))
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
            Sec("◆ 救助队友 (RespawnRpc Everyone)");
            if (GUILayout.Button("  ↺  一键复活所有死亡队友 (RespawnRpc)", sBtn, GUILayout.Height(32)))
            {
                foreach (var p in cachedPlayers)
                {
                    if (p == null || p.Health == null) continue;
                    if (p.Health.Dead) try { p.Health.RespawnRpc(); } catch { }
                }
            }

            if (GUILayout.Button("  ♥  一键满血所有队友", sBtn, GUILayout.Height(28)))
            {
                foreach (var p in cachedPlayers)
                {
                    if (p == null || p.Health == null) continue;
                    try { p.Health.SelfDamageRpc(-999f); } catch { }
                }
            }
            if (GUILayout.Button("  🔓  解除所有队友捆绑状态", sBtn, GUILayout.Height(28)))
            {
                foreach (var p in cachedPlayers)
                {
                    if (p == null) continue;
                    var seh = p.StatusEffects;
                    if (seh != null) try { seh.SetTiedRpc(false); } catch { }
                }
            }

            if (GUILayout.Button("  🏥  呼叫医疗终端复活全员", sBtn, GUILayout.Height(28)))
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
            Sec("◆ ESP 透视");
            enableEnemyESP   = Tog(enableEnemyESP,   "敌人透视  (HP + 距离)");
            enableLootESP    = Tog(enableLootESP,    "存箱透视");
            enableItemESP    = Tog(enableItemESP,    "赃物透视");
            enablePlayerESP  = Tog(enablePlayerESP,  "队友透视");
            enableVehicleESP = Tog(enableVehicleESP, "载具透视");
            GUILayout.Space(4);
            GUILayout.Label($"   最大显示距离: {espMaxDistance:F0}m", sLbl);
            espMaxDistance = GUILayout.HorizontalSlider(espMaxDistance, 20f, 300f);
        }

        private void DrawTabChaos()
        {
            Sec("◆ 混沌引擎 (谨慎使用)");
            if (GUILayout.Button("  💥  引爆全图所有手雷 (InstantExplodeRpc)", sBtn, GUILayout.Height(30)))
            {
                foreach (var g in cachedGrenades)
                    if (g != null) try { g.InstantExplodeRpc(); } catch { }
            }

            if (GUILayout.Button("  🔥  点燃全图所有手雷引线 (StartFuseRpc)", sBtn, GUILayout.Height(28)))
            {
                foreach (var g in cachedGrenades)
                    if (g != null) try { g.StartFuseRpc(); } catch { }
            }
            if (GUILayout.Button("  🚽  洪水：触发所有马桶喷涌 (PlayFloodRpc)", sBtn, GUILayout.Height(28)))
            {
                foreach (var t in cachedToilets)
                {
                    if (t == null) continue;
                    var mi = typeof(Toilet).GetMethod("PlayFloodRpc", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (mi != null) try { mi.Invoke(t, null); } catch { }
                }
            }

            if (GUILayout.Button("  🌪  启动所有龙卷风传送门 (StartVortexRpc)", sBtn, GUILayout.Height(28)))
            {
                foreach (var h in FindObjectsOfType<GnomeHouse>())
                {
                    if (h == null) continue;
                    try { h.StartVortexRpc(h.transform.position, 1); } catch { }
                }
            }
            if (GUILayout.Button("  ⛓  捆绑全图玩家 (真.团灭)", sBtn, GUILayout.Height(28)))
            {
                foreach (var p in cachedPlayers)
                {
                    if (p == null || p.IsLocalPlayer) continue;
                    var seh = p.StatusEffects;
                    if (seh != null) try { seh.SetTiedRpc(true); } catch { }
                }
            }

            if (GUILayout.Button("  🗿  毁灭所有花园侏儒 (DestroyGnomeRpc)", sBtn, GUILayout.Height(28)))
            {
                foreach (var g in FindObjectsOfType<GardenGnome>())
                {
                    if (g == null) continue;
                    var mi = typeof(GardenGnome).GetMethod("DestroyGnomeRpc", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (mi != null) try { mi.Invoke(g, null); } catch { }
                }
            }
        }

        // ESP 标签绘制辅助
        private GUIStyle _espStyleRed, _espStyleCyan, _espStyleYellow, _espStyleGreen, _espStyleOrange;
        private void BuildESPStyles()
        {
            if (_espStyleRed != null) return;
            void MkESP(ref GUIStyle s, Color col)
            {
                s = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold };
                s.normal.textColor = col;
            }
            MkESP(ref _espStyleRed,    new Color(1f, 0.3f, 0.3f));
            MkESP(ref _espStyleCyan,   new Color(0f, 0.9f, 1f));
            MkESP(ref _espStyleYellow, new Color(1f, 0.9f, 0.2f));
            MkESP(ref _espStyleGreen,  new Color(0.2f, 1f, 0.4f));
            MkESP(ref _espStyleOrange, new Color(1f, 0.55f, 0.1f));
        }

        private void DrawESP()
        {
            BuildESPStyles();
            if (Camera.main == null) return;
            Vector3 camPos = Camera.main.transform.position;

            if (enableEnemyESP)
                foreach (var enemy in cachedEnemies)
                {
                    if (enemy == null || enemy.Dead) continue;
                    Vector3 pos = enemy.transform.position + Vector3.up * 1.5f;
                    float dist = Vector3.Distance(camPos, pos);
                    if (dist > espMaxDistance) continue;
                    Vector3 w2s = Camera.main.WorldToScreenPoint(pos);
                    if (w2s.z <= 0f) continue;
                    GUI.Label(new Rect(w2s.x - 30, Screen.height - w2s.y, 130, 36),
                              $"⚔ 敌  {dist:F0}m\nHP {(int)enemy.Health}", _espStyleRed);
                }

            if (enableLootESP)
                foreach (var d in cachedDeposits)
                {
                    if (d == null) continue;
                    Vector3 pos = d.transform.position + Vector3.up;
                    float dist = Vector3.Distance(camPos, pos);
                    if (dist > espMaxDistance) continue;
                    Vector3 w2s = Camera.main.WorldToScreenPoint(pos);
                    if (w2s.z <= 0f) continue;
                    GUI.Label(new Rect(w2s.x - 30, Screen.height - w2s.y, 130, 36),
                              $"📦 存箱  {dist:F0}m\n{d.DepositedGnomium}", _espStyleCyan);
                }

            if (enableItemESP)
                foreach (var loot in cachedLoot)
                {
                    if (loot == null || loot.carriedByBob) continue;
                    Vector3 pos = loot.transform.position + Vector3.up * 0.5f;
                    float dist = Vector3.Distance(camPos, pos);
                    if (dist > espMaxDistance) continue;
                    Vector3 w2s = Camera.main.WorldToScreenPoint(pos);
                    if (w2s.z <= 0f) continue;
                    GUI.Label(new Rect(w2s.x - 30, Screen.height - w2s.y, 130, 36),
                              $"✦ 赃物  {dist:F0}m\n{loot.ItemType}", _espStyleYellow);
                }

            if (enablePlayerESP)
                foreach (var p in cachedPlayers)
                {
                    if (p == null || p.IsLocalPlayer) continue;
                    Vector3 pos = p.transform.position + Vector3.up * 2f;
                    float dist = Vector3.Distance(camPos, pos);
                    if (dist > espMaxDistance) continue;
                    Vector3 w2s = Camera.main.WorldToScreenPoint(pos);
                    if (w2s.z <= 0f) continue;
                    string hp = p.Health != null ? ((int)p.Health.Health).ToString() : "?";
                    GUI.Label(new Rect(w2s.x - 30, Screen.height - w2s.y, 130, 36),
                              $"👤 队友  {dist:F0}m\nHP {hp}", _espStyleGreen);
                }

            if (enableVehicleESP)
                foreach (var v in cachedVehicles)
                {
                    if (v == null) continue;
                    Vector3 pos = v.transform.position + Vector3.up;
                    float dist = Vector3.Distance(camPos, pos);
                    if (dist > espMaxDistance) continue;
                    Vector3 w2s = Camera.main.WorldToScreenPoint(pos);
                    if (w2s.z <= 0f) continue;
                    GUI.Label(new Rect(w2s.x - 30, Screen.height - w2s.y, 110, 24),
                              $"🚗 载具  {dist:F0}m", _espStyleOrange);
                }
        }
    }
}
