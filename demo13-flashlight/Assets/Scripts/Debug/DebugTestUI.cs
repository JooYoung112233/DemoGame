using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 디버그 테스트 UI.
/// F1키로 토글. 탭 구분으로 여러 시스템 테스트 가능.
/// </summary>
public class DebugTestUI : MonoBehaviour
{
    static DebugTestUI instance;

    [SerializeField] KeyCode toggleKey = KeyCode.F1;

    bool isOpen;
    bool inMapTool;
    int currentTab;
    string[] tabNames = { "플레이어", "경제/평판", "아이템", "하이드아웃", "씬" };

    // 레퍼런스
    Health health;
    PlayerInventory inventory;
    PlayerEquipment equipment;
    FlashlightController flashlight;

    // 드래그 이동
    Rect windowRect;
    bool windowRectInit;

    GUIStyle tabActiveStyle;
    GUIStyle tabInactiveStyle;
    GUIStyle headerStyle;
    GUIStyle btnStyle;
    GUIStyle labelStyle;
    GUIStyle smallBtnStyle;
    Texture2D bgTex;
    Texture2D tabActiveTex;
    Texture2D tabInactiveTex;
    bool stylesInit;

    Vector2 scrollPos;

    // 경제 탭
    int moneyInputAmount = 10000;
    int repInputAmount = 10;

    // 아이템 탭
    int itemSubTab;
    string[] itemSubTabNames = { "장비", "무기", "의료", "소비", "재료", "귀중품", "특수", "보관함" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        if (FindFirstObjectByType<DebugTestUI>(FindObjectsInactive.Include) != null) return;

        var go = new GameObject("[DebugTestUI]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DebugTestUI>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 전환 시 레퍼런스 리셋
        health = null; inventory = null; equipment = null; flashlight = null;
    }

    void FindPlayer()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            health = playerGO.GetComponent<Health>();
            inventory = playerGO.GetComponent<PlayerInventory>();
            equipment = playerGO.GetComponent<PlayerEquipment>();
            flashlight = playerGO.GetComponentInChildren<FlashlightController>();
        }
    }

    void Update()
    {
        if (inMapTool) return;
        if (GameInput.GetKeyDown(toggleKey))
            isOpen = !isOpen;
    }

    void OnGUI()
    {
        if (inMapTool || !isOpen) return;

        InitStyles();

        // 2026-07-29 사용자: "F1 창이 좀 작아서 글씨가 깨진다 — 30% 정도 키워줘".
        //   창만 키우면 글자는 그대로라 여전히 잘린다 → **글꼴도 같은 비율로** 키운다(아래 Styles).
        float panelW = 832f;    // 640 × 1.3
        float panelH = 988f;    // 760 × 1.3

        if (!windowRectInit)
        {
            windowRect = new Rect((Screen.width - panelW) * 0.5f, (Screen.height - panelH) * 0.5f, panelW, panelH);
            windowRectInit = true;
        }

        windowRect = GUI.Window(9999, windowRect, DrawWindow, "", GUIStyle.none);
        windowRect.x = Mathf.Clamp(windowRect.x, -panelW + 50, Screen.width - 50);
        windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - 50);
    }

    void DrawWindow(int windowID)
    {
        if (health == null) FindPlayer();

        float panelW = windowRect.width;
        float panelH = windowRect.height;

        GUI.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);
        GUI.DrawTexture(new Rect(0, 0, panelW, panelH), bgTex);
        GUI.color = Color.white;

        // 글꼴을 30% 키웠으므로 머리말·탭 높이도 같이 키운다 — 안 키우면 글자가 버튼 밖으로 삐져나온다.
        GUI.Label(new Rect(12, 6, panelW - 24, 32), "[ DEBUG ]  F1 닫기", headerStyle);
        GUI.DragWindow(new Rect(0, 0, panelW, 36));

        float tabY = 38;
        float tabH = 36;
        float tabW = panelW / tabNames.Length;
        for (int i = 0; i < tabNames.Length; i++)
        {
            GUIStyle style = (i == currentTab) ? tabActiveStyle : tabInactiveStyle;
            if (GUI.Button(new Rect(tabW * i, tabY, tabW, tabH), tabNames[i], style))
                currentTab = i;
        }

        float contentY = tabY + tabH + 8;
        float contentH = panelH - contentY - 12;
        Rect contentRect = new Rect(10, contentY, panelW - 20, contentH);

        GUILayout.BeginArea(contentRect);
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        switch (currentTab)
        {
            case 0: DrawPlayerTab(); break;
            case 1: DrawEconomyTab(); break;
            case 2: DrawItemTab(); break;
            case 3: DrawHideoutTab(); break;
            case 4: DrawSceneTab(); break;
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    #region 탭: 플레이어 (HP/스태미너/의료/생존)

    void DrawPlayerTab()
    {
        // ── HP ──
        GUILayout.Label("── HP ──", headerStyle);
        if (health != null)
        {
            GUILayout.Label($"HP: {health.CurrentHp:F0} / {health.MaxHp:F0}", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("데미지 20", btnStyle)) health.TakeDamage(20f);
            if (GUILayout.Button("데미지 50", btnStyle)) health.TakeDamage(50f);
            if (GUILayout.Button("풀힐", btnStyle)) health.Heal(health.MaxHp);
            GUILayout.EndHorizontal();
        }
        else GUILayout.Label("Player 없음", labelStyle);

        GUILayout.Space(8);

        // ── 스태미너/전투 ──
        var tdp = TopDownPlayer.Instance;
        if (tdp != null)
        {
            GUILayout.Label("── 전투 ──", headerStyle);
            GUILayout.Label($"스태미너: {tdp.StaminaCurrent:F0}/{tdp.StaminaMax:F0}  |  전투: {(tdp.CombatEnabled ? "ON" : "OFF")}", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("전투 ON", btnStyle)) tdp.CombatEnabled = true;
            if (GUILayout.Button("전투 OFF", btnStyle)) tdp.CombatEnabled = false;
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(8);

        // ── 생존 (허기/수분) ──
        GUILayout.Label("── 생존 스탯 ──", headerStyle);
        var survival = SurvivalStats.Get();
        if (survival != null)
        {
            GUILayout.Label($"포만감: {survival.Satiety:F0}/100  |  수분: {survival.Water:F0}/100", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("포만감 MAX", btnStyle)) survival.AddSatiety(100f);
            if (GUILayout.Button("수분 MAX", btnStyle)) survival.AddWater(100f);
            if (GUILayout.Button("전부 MAX", btnStyle)) { survival.AddSatiety(100f); survival.AddWater(100f); }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("포만감 0", btnStyle)) survival.AddSatiety(-200f);
            if (GUILayout.Button("수분 0", btnStyle)) survival.AddWater(-200f);
            GUILayout.EndHorizontal();
        }
        else GUILayout.Label("SurvivalStats 없음", labelStyle);

        GUILayout.Space(8);

        // ── 유인 테스트 ──
        GUILayout.Space(8);
        GUILayout.Label("── 유인 (던진 물건) ──", headerStyle);
        GUILayout.Label("소음 시스템은 폐기(2026-09-09). 적을 끄는 건 투척물 착탄뿐이다.", labelStyle);
        if (GUILayout.Button("발밑에 유인 발생", btnStyle))
        {
            var pp = TopDownPlayer.Instance;
            var gt = GameTuning.Instance;
            if (pp != null)
                Distraction.Report(Plan3D.ToPlan(pp.transform.position),
                                   gt != null ? gt.throwNoiseRadius : 9f,
                                   gt != null ? gt.noisePulseDuration : 0.6f);
        }

        // ── 투척물 테스트 (돌 지급) ──
        GUILayout.Space(8);
        GUILayout.Label("── 투척물 (돌) ──", headerStyle);
        var thInv = TopDownPlayer.Instance != null ? TopDownPlayer.Instance.GetComponent<PlayerInventory>() : null;
        if (thInv != null)
        {
            GUILayout.Label($"보유 돌: {thInv.CountItemAll(ThrowSystem.ThrowItemId)}개  (G키 조준 → 좌클릭 투척, 착탄 소음으로 유인)", labelStyle);
            if (GUILayout.Button("돌 5개 지급", btnStyle))
            {
                var stone = ItemDatabase.Get(ThrowSystem.ThrowItemId);
                if (stone != null) thInv.TryAutoPlaceAnywhere(new ItemInstance(stone, 5));
                else ToastManager.Show("Stone SO 없음 (Resources/Items/Misc/Stone)", ToastManager.ToastType.Warning);
            }
        }
        else GUILayout.Label("PlayerInventory 없음", labelStyle);

        // ── 타격 판정 진단 (2026-07-29 임시) ──
        GUILayout.Space(8);
        GUILayout.Label("── 진단 ──", headerStyle);
        bool atkLog = GUILayout.Toggle(AttackPerformer.DebugLog, " 타격 판정 로그");
        if (atkLog != AttackPerformer.DebugLog) AttackPerformer.DebugLog = atkLog;
        GUILayout.Label("스윙할 때마다 콘솔에: 스캔 몇 개 / 적중 / 허트박스없음·꺼짐·중복·벽막힘",
                        labelStyle);

        // ── 부위 표시 (2026-07-29 테스트 도구) ──
        GUILayout.Space(8);
        GUILayout.Label("── 부위 판정 ──", headerStyle);
        bool zonesOn = GUILayout.Toggle(BodyZoneOverlay.Enabled, " 몸 부위 표시(타르코프식)");
        if (zonesOn != BodyZoneOverlay.Enabled) BodyZoneOverlay.Enabled = zonesOn;
        GUILayout.Label("머리 ×2.0 · 몸통 ×1.0 · 팔 ×0.8 · 다리 ×0.75\n" +
                        "근접·총 모두 **조준한 곳**이 맞는다. 맞은 부위가 잠깐 밝아진다.", labelStyle);

        // ── 총기 (2026-07-29) ──
        //   기능엔 검증 수단이 같이 가야 한다 — 총·탄창·탄약을 손으로 찾아 주우려면 테스트가 안 된다.
        GUILayout.Space(8);
        GUILayout.Label("── 총기 (좌클릭 사격 / 우클릭 조준 / R 장전) ──", headerStyle);
        var gunInv = TopDownPlayer.Instance != null ? TopDownPlayer.Instance.GetComponent<PlayerInventory>() : null;
        if (gunInv != null)
        {
            var gunComp = TopDownPlayer.Instance.GetComponent<PlayerGun>();
            if (gunComp != null && TopDownPlayer.Instance.IsRangedEquipped)
                GUILayout.Label($"탄 {gunComp.Ammo}/{gunComp.Capacity}" +
                                (gunComp.IsReloading ? "  장전 중…" : "") +
                                $"   탄퍼짐 {gunComp.CurrentSpreadDeg:0.0}°", labelStyle);
            else
                GUILayout.Label("총 미장착 — 아래로 지급한 뒤 인벤에서 착용", labelStyle);

            if (GUILayout.Button("권총 + 탄창2 + 탄약60 지급", btnStyle))
            {
                var gun  = ItemDatabase.Get("pistol9");
                var mag  = ItemDatabase.Get("mag_9x19");
                var ammo = ItemDatabase.Get("ammo_9x19");
                if (gun == null || mag == null || ammo == null)
                    ToastManager.Show("총기 SO 없음 (Resources/Items의 pistol9/mag_9x19/ammo_9x19)",
                                      ToastManager.ToastType.Warning);
                else
                {
                    // 탄창 하나는 **가득 채워** 준다 — 안 그러면 지급하자마자 "맞는 탄창 없음"이라
                    //   총이 왜 안 나가는지 확인하는 데만 시간이 든다.
                    var full = new ItemInstance(mag);
                    full.ammoCount = mag.magCapacity;
                    full.ammoItemId = ammo.itemId;
                    gunInv.TryAutoPlaceAnywhere(new ItemInstance(gun));
                    gunInv.TryAutoPlaceAnywhere(full);
                    gunInv.TryAutoPlaceAnywhere(new ItemInstance(mag));     // 빈 탄창 — 채우기 테스트용
                    gunInv.TryAutoPlaceAnywhere(new ItemInstance(ammo, 60));
                    ToastManager.Show("권총·탄창2(하나는 가득)·탄약60 지급", ToastManager.ToastType.Info);
                }
            }
            if (GUILayout.Button("장착 총에 탄창 물리기(빠른 셋업)", btnStyle))
            {
                var eq = TopDownPlayer.Instance.GetComponent<PlayerEquipment>();
                var inst = eq != null ? eq.GetSlotInstance(EquipSlot.PrimaryWeapon) : null;
                var mag  = ItemDatabase.Get("mag_9x19");
                var ammo = ItemDatabase.Get("ammo_9x19");
                if (inst == null || mag == null || ammo == null)
                    ToastManager.Show("총을 먼저 착용하세요", ToastManager.ToastType.Warning);
                else
                {
                    inst.SetAttachment(WeaponPartType.Magazine, mag.itemId);
                    inst.ammoCount = mag.magCapacity;
                    inst.ammoItemId = ammo.itemId;
                    ToastManager.Show($"탄창 장착 — {inst.ammoCount}/{inst.AmmoCapacity}", ToastManager.ToastType.Info);
                }
            }
        }

        // ── 도감 (⑨) ──
        GUILayout.Space(6);
        GUILayout.Label("── 아이템 도감 (U키) ──", headerStyle);
        var cx = CodexManager.Instance;
        if (cx != null)
        {
            var allItems = ItemDatabase.GetAll();
            int totalItems = allItems != null ? allItems.Length : 0;
            GUILayout.Label($"발견: {cx.DiscoveredCount}/{totalItems}   (U키로 도감 열기 — 미발견=실루엣+???)", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("전체 발견 처리", btnStyle))
            {
                cx.DebugDiscoverAll();
                ToastManager.Show("도감 — 전체 발견", ToastManager.ToastType.Info);
            }
            if (GUILayout.Button("도감 초기화", btnStyle))
            {
                cx.ResetForNewGame();
                ToastManager.Show("도감 — 초기화", ToastManager.ToastType.Warning);
            }
            GUILayout.EndHorizontal();
        }
        else GUILayout.Label("CodexManager 없음", labelStyle);
    }

    #endregion

    #region 탭: 경제/평판

    void DrawEconomyTab()
    {
        // ── 스크랩(돈) ──
        GUILayout.Label("── 스크랩 (◈) ──", headerStyle);
        var currency = CurrencyManager.Instance;
        if (currency != null)
        {
            GUILayout.Label($"잔액: ◈ {currency.Balance:N0}", labelStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", smallBtnStyle, GUILayout.Width(30)))
                moneyInputAmount = Mathf.Max(100, moneyInputAmount / 2);
            GUILayout.Label($"{moneyInputAmount:N0}", labelStyle, GUILayout.Width(80));
            if (GUILayout.Button("+", smallBtnStyle, GUILayout.Width(30)))
                moneyInputAmount = Mathf.Min(1000000, moneyInputAmount * 2);
            if (GUILayout.Button($"지급 +{moneyInputAmount:N0}", btnStyle))
                currency.Add(moneyInputAmount, "디버그");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1천", smallBtnStyle)) currency.Add(1000, "디버그");
            if (GUILayout.Button("1만", smallBtnStyle)) currency.Add(10000, "디버그");
            if (GUILayout.Button("10만", smallBtnStyle)) currency.Add(100000, "디버그");
            if (GUILayout.Button("100만", smallBtnStyle)) currency.Add(1000000, "디버그");
            GUILayout.EndHorizontal();

            if (GUILayout.Button("잔액 초기화 (0)", btnStyle))
                currency.Lose(currency.Balance, "디버그 리셋");
        }
        else GUILayout.Label("CurrencyManager 없음", labelStyle);

        GUILayout.Space(10);

        // ── 평판 ──
        GUILayout.Label("── 평판 ──", headerStyle);
        var rep = ReputationManager.Instance;
        if (rep != null)
        {
            GUILayout.Label($"평판: {rep.Reputation}  [{rep.TierName} ({rep.Tier})]  다음까지: {rep.ToNextTier}", labelStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", smallBtnStyle, GUILayout.Width(30)))
                repInputAmount = Mathf.Max(1, repInputAmount / 2);
            GUILayout.Label($"{repInputAmount}", labelStyle, GUILayout.Width(50));
            if (GUILayout.Button("+", smallBtnStyle, GUILayout.Width(30)))
                repInputAmount = Mathf.Min(1000, repInputAmount * 2);
            if (GUILayout.Button($"+{repInputAmount}", btnStyle)) rep.Add(repInputAmount, "디버그");
            if (GUILayout.Button($"-{repInputAmount}", btnStyle)) rep.Add(-repInputAmount, "디버그");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("F (0)", smallBtnStyle)) SetReputation(0);
            if (GUILayout.Button("E (10)", smallBtnStyle)) SetReputation(10);
            if (GUILayout.Button("D (30)", smallBtnStyle)) SetReputation(30);
            if (GUILayout.Button("C (60)", smallBtnStyle)) SetReputation(60);
            if (GUILayout.Button("B (100)", smallBtnStyle)) SetReputation(100);
            if (GUILayout.Button("A (150)", smallBtnStyle)) SetReputation(150);
            GUILayout.EndHorizontal();
        }
        else GUILayout.Label("ReputationManager 없음", labelStyle);

        GUILayout.Space(10);

        // ── 상점 재고 회전 ──
        GUILayout.Label("── 상점 재고 회전 (1일 주기) ──", headerStyle);
        GUILayout.Label($"현재 게임일: {DailyQuestManager.Day()}  (이 날짜 경계로 고정+회전 슬롯 갱신)", labelStyle);
        if (GUILayout.Button("상점 재고 '다음날'로 강제 회전(+재입고)", btnStyle)) ShopUI.ForceRotate();

        GUILayout.Space(10);

        // ── NPC 호감도 ──
        GUILayout.Label("── NPC 관계도 ──", headerStyle);
        var npcRel = NPCRelationshipManager.Instance;
        if (npcRel != null)
        {
            var allRels = npcRel.GetAllRelationships();
            if (allRels != null && allRels.Count > 0)
            {
                foreach (var kv in allRels)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{kv.Key}: 호감{kv.Value.affinity} 신뢰{kv.Value.trust} 공포{kv.Value.fear}", labelStyle, GUILayout.Width(360));
                    if (GUILayout.Button("+호감", smallBtnStyle, GUILayout.Width(70)))
                        npcRel.ModifyRelationship(kv.Key, 10, 0, 0);
                    if (GUILayout.Button("+신뢰", smallBtnStyle, GUILayout.Width(70)))
                        npcRel.ModifyRelationship(kv.Key, 0, 10, 0);
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("NPC 만남 기록 없음 (NPC와 대화하세요)", labelStyle);
            }
        }
        else GUILayout.Label("NPCRelationshipManager 없음", labelStyle);
    }

    void SetReputation(int target)
    {
        var rep = ReputationManager.Instance;
        if (rep == null) return;
        int delta = target - rep.Reputation;
        if (delta != 0) rep.Add(delta, "디버그 등급 설정");
    }

    #endregion

    #region 탭: 아이템

    void DrawItemTab()
    {
        if (inventory == null)
        {
            GUILayout.Label("PlayerInventory 없음", labelStyle);
            return;
        }

        GUILayout.Label($"── 인벤토리 ({inventory.Grid.width}x{inventory.Grid.height}) ──", headerStyle);
        GUILayout.Label($"아이템: {inventory.Grid.ItemCount}개  |  무게: {inventory.CurrentWeight:F1}/{inventory.MaxWeight:F0} kg  |  가방: {(inventory.HasBackpack ? "O" : "X")}", labelStyle);

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("인벤 비우기", btnStyle)) { inventory.Grid.Clear(); SaveCheckpoints.Instance?.InventoryChanged(); }
        if (GUILayout.Button("창고 비우기", btnStyle))
        {
            var stash = MainStash.Ensure();
            if (stash != null) stash.GetGrid().Clear();
            SaveCheckpoints.Instance?.InventoryChanged();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("── 아이템 지급 ──", headerStyle);

        // 서브탭
        GUILayout.BeginHorizontal();
        for (int t = 0; t < itemSubTabNames.Length; t++)
        {
            var style = t == itemSubTab ? tabActiveStyle : tabInactiveStyle;
            if (GUILayout.Button(itemSubTabNames[t], style, GUILayout.Height(22)))
                itemSubTab = t;
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(4);

        var allItems = ItemDatabase.GetAll();
        if (allItems.Length == 0)
        {
            GUILayout.Label("ItemDatabase 비어있음", labelStyle);
        }
        else
        {
            for (int i = 0; i < allItems.Length; i++)
            {
                if (!MatchesItemSubTab(allItems[i], itemSubTab)) continue;

                GUILayout.BeginHorizontal();
                string equipInfo = allItems[i].equipSlot != EquipSlot.None ? $" [{allItems[i].equipSlot}]" : "";
                GUILayout.Label($"{allItems[i].displayName} ({allItems[i].weight:F2}kg){equipInfo}", labelStyle, GUILayout.Width(360));
                if (GUILayout.Button("+인벤", smallBtnStyle, GUILayout.Width(70)))
                {
                    var item = new ItemInstance(allItems[i], 1);
                    if (!inventory.TryPickup(item))
                        ToastManager.Show("인벤 공간 부족", ToastManager.ToastType.Warning);
                    else SaveCheckpoints.Instance?.InventoryChanged();
                }
                if (GUILayout.Button("+창고", smallBtnStyle, GUILayout.Width(70)))
                {
                    var stash = MainStash.Ensure();
                    if (stash != null)
                    {
                        var item = new ItemInstance(allItems[i], 1);
                        if (!stash.GetGrid().TryAutoPlace(item))
                            ToastManager.Show("창고 공간 부족", ToastManager.ToastType.Warning);
                        else SaveCheckpoints.Instance?.InventoryChanged();
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("── 월드 드롭 ──", headerStyle);
        var p = TopDownPlayer.Instance;
        if (allItems.Length > 0 && p != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("앞에 랜덤 드롭", btnStyle))
            {
                var data = allItems[Random.Range(0, allItems.Length)];
                var item = new ItemInstance(data, Random.Range(1, Mathf.Min(data.maxStack, 3) + 1));
                WorldItem.Drop(item, p.transform.position + (Vector3)(p.FacingDirection * 1.5f));
            }
            if (GUILayout.Button("주변 5개", btnStyle))
            {
                for (int j = 0; j < 5; j++)
                {
                    var data = allItems[Random.Range(0, allItems.Length)];
                    var item = new ItemInstance(data, Random.Range(1, Mathf.Min(data.maxStack, 3) + 1));
                    WorldItem.Drop(item, p.transform.position + new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0));
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            if (GUILayout.Button("시체 스폰 (앞에, 랜덤 아이템 — 뒤지기 테스트)", btnStyle))
                SpawnTestCorpse(p, allItems);
        }

        // ── 무게 테스트 (초과 페널티 검증) ──
        GUILayout.Space(10);
        GUILayout.Label("── 무게 테스트 (초과 페널티) ──", headerStyle);
        float ratio = inventory.MaxWeight > 0f ? inventory.CurrentWeight / inventory.MaxWeight : 0f;
        string tier = ratio >= 1.30f ? "하드컷" : ratio >= 1.15f ? "심각" : ratio >= 1.00f ? "과적" : "정상";
        GUILayout.Label($"무게 {inventory.CurrentWeight:F1} / {inventory.MaxWeight:F0} kg  ({ratio * 100f:F0}%) — <b>{tier}</b>", labelStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+납덩이 15kg", btnStyle)) GiveHeavyFiller(inventory, 0f);   // 1개만
        if (GUILayout.Button("과적 105%", btnStyle)) GiveHeavyFiller(inventory, 1.05f);
        if (GUILayout.Button("심각 120%", btnStyle)) GiveHeavyFiller(inventory, 1.20f);
        if (GUILayout.Button("하드컷 135%", btnStyle)) GiveHeavyFiller(inventory, 1.35f);
        GUILayout.EndHorizontal();
    }

    /// <summary>무게 테스트용 15kg 납덩이(1×1)를 인벤에 추가. targetRatio>0이면 그 비율에 도달할 때까지 반복(하드컷 무시 = 디버그).
    /// 런타임 ItemData라 세이브/DB에 안 남음.</summary>
    static void GiveHeavyFiller(PlayerInventory inventory, float targetRatio)
    {
        int added = 0, guard = 0;
        do
        {
            var lead = MakeHeavyItem();
            if (!inventory.TryAutoPlaceAnywhere(new ItemInstance(lead, 1)))   // 하드컷 미적용(기본 respectWeightCap=false)
            {
                ToastManager.Show("공간 부족 — 가방을 착용하거나 인벤을 비워라", ToastManager.ToastType.Warning);
                break;
            }
            added++;
            if (targetRatio <= 0f) break;   // 1개만
        }
        while (inventory.MaxWeight > 0f
               && inventory.CurrentWeight < inventory.MaxWeight * targetRatio
               && ++guard < 50);

        SaveCheckpoints.Instance?.InventoryChanged();
        if (added > 0) ToastManager.Show($"납덩이 {added}개 추가 ({inventory.CurrentWeight:F0}kg)", ToastManager.ToastType.Info);
    }

    static ItemData MakeHeavyItem()
    {
        var d = ScriptableObject.CreateInstance<ItemData>();
        d.itemId = "debug_lead";
        d.displayName = "납덩이(테스트)";
        d.gridWidth = 1; d.gridHeight = 1;
        d.category = ItemCategory.Material;
        d.rarity = ItemRarity.Common;
        d.maxStack = 1;
        d.weight = 15f;
        return d;
    }

    /// <summary>시체 루팅 테스트 — 플레이어 앞에 시체(LootContainer 3×3 + '시체 뒤지기' 상호작용)를 만들고
    /// 랜덤 아이템 2~4종을 채운다. 적 사망 경로(EnemyController.BecomeCorpse)와 동일 구성.</summary>
    static void SpawnTestCorpse(TopDownPlayer p, ItemData[] allItems)
    {
        var go = new GameObject("Debug_Corpse");
        go.transform.position = p.transform.position + (Vector3)(p.FacingDirection * 2f);

        // 비주얼: 누운 몸뚱이 느낌의 어두운 막대 스프라이트
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        sr.color = new Color(0.35f, 0.22f, 0.20f, 1f);
        sr.sortingOrder = 3;
        go.transform.localScale = new Vector3(1.1f, 0.5f, 1f);

        // 적 사망 경로와 같은 "시체" 라벨(그레이박스 식별).
        UnitLabel.Attach(go.transform, "시체", UnitLabel.CorpseColor, 4);

        // 내용물: 랜덤 아이템 2~4종 + 가방 1개(테스트라 항상 — 안에 랜덤 1~3개, 가방째 회수 가능)
        var items = new System.Collections.Generic.List<ItemInstance>();
        int count = Random.Range(2, 5);
        for (int j = 0; j < count; j++)
        {
            var data = allItems[Random.Range(0, allItems.Length)];
            items.Add(new ItemInstance(data, Random.Range(1, Mathf.Min(data.maxStack, 3) + 1)));
        }

        ItemData bagData = null;
        int seen = 0;
        for (int j = 0; j < allItems.Length; j++)
        {
            var d = allItems[j];
            if (d == null || d.equipSlot != EquipSlot.Backpack || !d.IsContainer) continue;
            seen++;
            if (Random.Range(0, seen) == 0) bagData = d;
        }
        if (bagData != null)
        {
            var bag = new ItemInstance(bagData, 1);
            var inner = bag.ContainerGrid;
            int innerCount = Random.Range(1, 4);
            for (int j = 0; j < innerCount && inner != null; j++)
            {
                var d = allItems[Random.Range(0, allItems.Length)];
                inner.TryAutoPlace(new ItemInstance(d, 1));
            }
            items.Add(bag);
        }

        var container = go.AddComponent<LootContainer>();
        var overflow = container.SetupAutoSize("시체 (테스트)", items);   // 격자 = 내용물 크기에 맞춤
        foreach (var it in overflow)
            WorldItem.Drop(it, go.transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0));

        var io = go.AddComponent<InteractableObject>();
        io.SetupAsContainer("시체 뒤지기");

        ToastManager.Show($"시체 스폰 — 아이템 {count}종{(bagData != null ? " + 가방" : "")}", ToastManager.ToastType.Info);
    }

    static bool MatchesItemSubTab(ItemData item, int tab)
    {
        switch (tab)
        {
            case 0: // 장비 (가방/방어구/헬멧 등 — EquipSlot이 있되 무기 제외)
                return item.equipSlot != EquipSlot.None
                    && item.equipSlot != EquipSlot.PrimaryWeapon
                    && item.equipSlot != EquipSlot.SecondaryWeapon
                    && item.equipSlot != EquipSlot.Melee
                    && item.category != ItemCategory.Weapon;
            case 1: // 무기
                return item.category == ItemCategory.Weapon
                    || item.equipSlot == EquipSlot.PrimaryWeapon
                    || item.equipSlot == EquipSlot.SecondaryWeapon
                    || item.equipSlot == EquipSlot.Melee;
            case 2: // 의료
                return item.category == ItemCategory.Medical;
            case 3: // 소비 (음식/음료/배터리/자극제 등)
                return item.category == ItemCategory.Consumable;
            case 4: // 재료
                return item.category == ItemCategory.Material;
            case 5: // 귀중품 (판매용 잡템 포함)
                return item.category == ItemCategory.Valuable
                    || item.category == ItemCategory.Misc;
            case 6: // 특수 (열쇠/스토리/지역)
                return item.category == ItemCategory.Key;
            case 7: // 보관함 (컨테이너 아이템)
                return item.IsContainer;
            default:
                return true;
        }
    }

    #endregion

    #region 탭: 하이드아웃

    void DrawHideoutTab()
    {
        GUILayout.Label("── 시설 모듈 레벨 ──", headerStyle);
        var hm = HideoutModuleManager.Instance;
        if (hm != null)
        {
            for (int i = 0; i < HideoutModuleManager.Modules.Length; i++)
            {
                string m = HideoutModuleManager.Modules[i];
                int lv = hm.GetLevel(m);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{HideoutModuleManager.DisplayName(m)}: Lv{lv}/{HideoutModuleManager.MaxLevel}", labelStyle, GUILayout.Width(140));
                if (lv < HideoutModuleManager.MaxLevel)
                {
                    if (GUILayout.Button("+1", smallBtnStyle, GUILayout.Width(35)))
                        ForceUpgradeModule(m);
                }
                if (lv > 0)
                {
                    if (GUILayout.Button("리셋", smallBtnStyle, GUILayout.Width(45)))
                        ForceSetModuleLevel(m, 0);
                }
                if (GUILayout.Button("MAX", smallBtnStyle, GUILayout.Width(40)))
                    ForceSetModuleLevel(m, HideoutModuleManager.MaxLevel);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("전부 MAX", btnStyle))
            {
                for (int i = 0; i < HideoutModuleManager.Modules.Length; i++)
                    ForceSetModuleLevel(HideoutModuleManager.Modules[i], HideoutModuleManager.MaxLevel);
            }
            if (GUILayout.Button("전부 리셋", btnStyle))
            {
                for (int i = 0; i < HideoutModuleManager.Modules.Length; i++)
                    ForceSetModuleLevel(HideoutModuleManager.Modules[i], 0);
            }
            GUILayout.EndHorizontal();
        }
        else GUILayout.Label("HideoutModuleManager 없음", labelStyle);

        GUILayout.Space(10);

        // ── 하이드아웃 UI 바로가기 ──
        GUILayout.Label("── 바로가기 ──", headerStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("시설 관리 UI", btnStyle)) HideoutUI.Show();
        if (GUILayout.Button("라디오 UI", btnStyle)) RadioUI.Show();
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        if (UIManager.Instance != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("작업대", btnStyle)) UIManager.Instance.ShowCrafting(CraftingStation.Workbench);
            if (GUILayout.Button("의료대", btnStyle)) UIManager.Instance.ShowCrafting(CraftingStation.MedicalBench);
            if (GUILayout.Button("조리대", btnStyle)) UIManager.Instance.ShowCrafting(CraftingStation.CookingBench);
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(12);

        // ── 예시 UI 미리보기 (그레이박스 전체화면 UI 5종) ──
        GUILayout.Label("── 예시 UI 미리보기 ──", headerStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("레이드 지도", btnStyle)) RaidMapUI.ShowPreview();
        if (GUILayout.Button("지역 출전", btnStyle))
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMapSelect();
        }
        if (GUILayout.Button("의뢰/통신", btnStyle)) QuestLogUI.ShowPreview();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("라디오", btnStyle)) RadioUI.ShowPreview();
        GUILayout.EndHorizontal();
    }

    void ForceUpgradeModule(string module)
    {
        var hm = HideoutModuleManager.Instance;
        if (hm == null) return;
        hm.ForceSetLevel(module, hm.GetLevel(module) + 1);
    }

    void ForceSetModuleLevel(string module, int level)
    {
        var hm = HideoutModuleManager.Instance;
        if (hm == null) return;
        hm.ForceSetLevel(module, level);
    }

    #endregion

    #region 탭: 씬

    void DrawSceneTab()
    {
        GUILayout.Label("── 씬 전환 ──", headerStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("→ Safehouse", btnStyle, GUILayout.Height(28)))
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionTo("Safehouse", "default");
        }
        if (GUILayout.Button("→ Hideout", btnStyle, GUILayout.Height(28)))
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionTo("Hideout", "default");
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("→ ScrapMarket (레이드)", btnStyle, GUILayout.Height(28)))
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionTo("ScrapMarket_GB", "Gate_Spawn");
        }

        // ── 지역1(Zone1) 직행/복귀 — 랜덤 스폰 + 매치 탈출 확인용 ──
        GUILayout.Space(4);
        GUILayout.Label($"지역1: 매 판 스폰 5곳 중 랜덤 1 + 탈출 고정1(중앙)+반대편 2  |  이번 판 스폰: {(string.IsNullOrEmpty(RaidSpawnDirector.ChosenSpawn) ? "-" : RaidSpawnDirector.ChosenSpawn)}", labelStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("→ 지역1 (레이드)", btnStyle, GUILayout.Height(28)))
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionTo("Zone1", "");   // 스폰은 RaidSpawnDirector가 랜덤 지정
        }
        if (GUILayout.Button("→ 마을 복귀", btnStyle, GUILayout.Height(28)))
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionTo("Safehouse", "default");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("── 약탈자 회수 (사망 루프) ──", headerStyle);
        GUILayout.Label($"회수 대기 물품: {ScavengerLoot.PendingCount}개  (다음 레이드 약탈자=보랏빛 적에게 분배)", labelStyle);
        if (GUILayout.Button("잃은 물건 시뮬(3개 뺏김) → 다음 레이드 회수", btnStyle, GUILayout.Height(28)))
        {
            var db = ItemDatabase.GetAll();
            var loot = new System.Collections.Generic.List<ItemInstance>();   // 이 파일엔 Generic using 없음 → 완전수식(파일 관례)
            for (int i = 0; i < db.Length && loot.Count < 3; i++)             // GetAll()은 ItemData[] → .Length
                if (db[i] != null && db[i].buyPrice > 0) loot.Add(new ItemInstance(db[i], 1));
            ScavengerLoot.Capture(loot);
            ToastManager.Show($"약탈자 대기열 +{loot.Count} — 레이드 가서 보랏빛 적을 잡아 회수", ToastManager.ToastType.Info);
        }

        GUILayout.Space(10);
        GUILayout.Label("── UI 팝업 테스트(프리팹 검증) ──", headerStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("쪽지(NoteUI)", btnStyle, GUILayout.Height(28)))
            NoteUI.Ensure().Show("테스트 쪽지 본문입니다.\n\n프리팹 인스턴스로 정상 렌더되는지 확인용.\n[E]/[Esc]로 닫기.", "테스트 쪽지");
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("── 시간 ──", headerStyle);
        GUILayout.Label($"Time.timeScale: {Time.timeScale:F2}", labelStyle);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("x0.5", btnStyle)) Time.timeScale = 0.5f;
        if (GUILayout.Button("x1", btnStyle)) Time.timeScale = 1f;
        if (GUILayout.Button("x2", btnStyle)) Time.timeScale = 2f;
        if (GUILayout.Button("x5", btnStyle)) Time.timeScale = 5f;
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("── 밤/낮 ──", headerStyle);
        var dnc = FindFirstObjectByType<DayNightCycle>();
        if (dnc != null)
        {
            GUILayout.Label($"현재: {(dnc.IsNight ? "밤" : "낮")}", labelStyle);
            if (GUILayout.Button("밤/낮 전환 (T)", btnStyle))
                dnc.ToggleDayNight();
        }

        GUILayout.Space(10);
        GUILayout.Label("── 세이브 ──", headerStyle);
        var sm = SaveManager.Instance;
        if (sm != null)
        {
            bool hasSave = sm.HasSave();
            GUILayout.Label($"세이브 파일: {(hasSave ? "있음" : "없음")}", labelStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("저장", btnStyle, GUILayout.Height(32)))
            {
                sm.Save();
                ToastManager.Show("저장 완료", ToastManager.ToastType.Success);
            }
            if (GUILayout.Button("불러오기", btnStyle, GUILayout.Height(32)))
            {
                if (sm.Load())
                    ToastManager.Show("로드 완료", ToastManager.ToastType.Success);
                else
                    ToastManager.Show("세이브 없음", ToastManager.ToastType.Warning);
            }
            GUILayout.EndHorizontal();

            GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
            if (GUILayout.Button("세이브 삭제 (초기화)", btnStyle, GUILayout.Height(32)))
            {
                sm.DeleteSave();
                ToastManager.Show("세이브 삭제됨", ToastManager.ToastType.Warning);
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            GUILayout.Label("SaveManager 없음", labelStyle);
        }
    }

    #endregion

    #region 스타일 초기화

    void InitStyles()
    {
        if (stylesInit) return;

        bgTex = MakeTex(Color.white);
        tabActiveTex = MakeTex(new Color(0.2f, 0.4f, 0.6f));
        tabInactiveTex = MakeTex(new Color(0.15f, 0.15f, 0.2f));

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 23;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 21;
        labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.95f);
        labelStyle.richText = true;

        btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 20;
        btnStyle.fontStyle = FontStyle.Bold;
        btnStyle.padding = new RectOffset(8, 8, 6, 6);

        smallBtnStyle = new GUIStyle(GUI.skin.button);
        smallBtnStyle.fontSize = 18;
        smallBtnStyle.padding = new RectOffset(6, 6, 5, 5);

        tabActiveStyle = new GUIStyle(GUI.skin.button);
        tabActiveStyle.fontSize = 20;
        tabActiveStyle.fontStyle = FontStyle.Bold;
        tabActiveStyle.normal.background = tabActiveTex;
        tabActiveStyle.normal.textColor = Color.white;

        tabInactiveStyle = new GUIStyle(GUI.skin.button);
        tabInactiveStyle.fontSize = 20;
        tabInactiveStyle.normal.background = tabInactiveTex;
        tabInactiveStyle.normal.textColor = new Color(0.6f, 0.6f, 0.7f);

        stylesInit = true;
    }

    Texture2D MakeTex(Color col)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }

    #endregion
}
