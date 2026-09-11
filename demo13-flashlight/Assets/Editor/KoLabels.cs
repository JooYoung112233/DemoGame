using System.Collections.Generic;

/// <summary>
/// 에디터 툴 표시용 한글 라벨 사전.
/// CSV 헤더·SO 필드명(영어 변수명)은 파일/코드에 그대로 두고, 툴에서 보일 때만 한글로 매핑한다.
///
/// 사용: KoLabels.Get(fileName, header) → 한글 라벨(없으면 null → 호출부가 영문 원문 fallback).
///   · fileName : 현재 CSV 파일명(예: "items.csv"). null이면 전역 사전만 조회.
///   · header   : CSV 헤더/필드명(대소문자·BOM·공백 무시).
///
/// 우선순위: 파일별(PerFile) → 전역(Global). 미등록이면 null(영문 그대로 노출 — 누락돼도 안 깨짐).
/// 신규 컬럼/파일은 여기 항목만 추가하면 됨. NPC·Player·몬스터 탭도 같은 사전을 공유하도록 확장 예정.
/// </summary>
public static class KoLabels
{
    // 어느 파일에서도 의미가 같은 공통 헤더
    static readonly Dictionary<string, string> Global = new Dictionary<string, string>
    {
        { "note",     "메모" },
        { "region",   "지역" },
        { "value",    "값" },
        { "effect",   "효과" },
        { "materials","재료" },
        { "level",    "레벨" },
        { "min",      "최소" },
        { "max",      "최대" },
        { "unlock",   "해금 조건" },
        { "weight",   "가중치" },
        { "qty",      "수량" },
        { "name",     "이름" },
        { "id",       "ID" },
        { "cat",      "분류" },
        { "type",     "유형" },
        { "tier",     "구간" },
    };

    // 파일별 오버라이드(같은 헤더라도 파일마다 의미가 다른 것 + 그 파일 고유 컬럼)
    static readonly Dictionary<string, Dictionary<string, string>> PerFile =
        new Dictionary<string, Dictionary<string, string>>
    {
        ["items.csv"] = Items,
        ["region_items.csv"] = Items,   // region 컬럼만 추가, 나머지는 동일 → Global "region"로 처리
        ["quests.csv"] = new Dictionary<string, string>
        {
            { "id",            "퀘스트 ID" },
            { "cat",           "분류" },
            { "title",         "제목" },
            { "giver",         "의뢰 NPC" },
            { "type",          "목표 유형" },
            { "target",        "목표 대상" },
            { "qty",           "목표 수량" },
            { "reward_scrap",  "보상: 고철" },
            { "reward_items",  "보상: 아이템" },
            { "reward_rep",    "보상: 평판" },
            { "reward_trust",  "보상: 신뢰" },
            { "reward_unlock", "보상: 해금" },
            { "repeat",        "반복 가능" },
            { "flavor",        "플레이버 문구" },
        },
        ["region_loot.csv"] = RegionLoot,
        ["region_loot.txt"] = RegionLoot,
        ["loot_tables.txt"] = RegionLoot,   // 2026-09-11 지역 × 상자 종류 표(같은 컬럼 — tier 칸 = 상자 종류)
        ["barter.csv"] = new Dictionary<string, string>
        {
            { "npc",     "NPC" },
            { "in1",     "지불 1" },
            { "in1_qty", "지불 1 수량" },
            { "in2",     "지불 2" },
            { "in2_qty", "지불 2 수량" },
            { "out",     "획득" },
            { "out_qty", "획득 수량" },
        },
        ["dispatch.csv"] = new Dictionary<string, string>
        {
            { "type",          "파견 유형" },
            { "time_min",      "소요 시간(분)" },
            { "base_success",  "기본 성공률" },
            { "reward_main",   "주 보상" },
            { "reward_supply", "보급 보상" },
            { "fail",          "실패 시" },
        },
        ["dispatch_modifiers.csv"] = new Dictionary<string, string>
        {
            { "factor", "보정 요소" },
        },
        ["hideout_modules.csv"] = new Dictionary<string, string>
        {
            { "module",     "모듈" },
            { "scrap_cost", "고철 비용" },
        },
        ["quest_rewards.csv"] = new Dictionary<string, string>
        {
            { "grade",     "등급" },
            { "scrap_min", "고철 최소" },
            { "scrap_max", "고철 최대" },
        },
        ["reputation.csv"] = new Dictionary<string, string>
        {
            { "action", "행동(이벤트)" },
            { "rep",    "평판 증감" },
        },
        ["reputation_tiers.csv"] = new Dictionary<string, string>
        {
            { "tier",          "평판 등급" },
            { "name",          "명칭" },
            { "min",           "평판 최소" },
            { "max",           "평판 최대" },
            { "unlock",        "해금" },
            { "rent_discount", "임대료 할인" },
        },
        ["trust.csv"] = new Dictionary<string, string>
        {
            { "source", "행동(출처)" },
            { "trust",  "신뢰 증감" },
        },
        ["upkeep.csv"] = new Dictionary<string, string>
        {
            { "key", "항목" },
        },
    };

    // items.csv / region_items.csv 공용 컬럼
    static Dictionary<string, string> Items => new Dictionary<string, string>
    {
        { "id",      "아이템 ID" },
        { "disp",    "표시 이름" },
        { "desc",    "설명" },
        { "gw",      "격자 너비" },
        { "gh",      "격자 높이" },
        { "cat",     "카테고리" },
        { "rar",     "희귀도" },
        { "stack",   "최대 스택" },
        { "wt",      "무게(kg)" },
        { "use",     "사용 가능" },
        { "fx",      "사용 효과" },
        { "val",     "효과 수치" },
        { "med",     "의료 데이터" },
        { "dur",     "내구도 사용" },
        { "maxdur",  "최대 내구도" },
        { "costdur", "사용당 내구 소모" },
    };

    // region_loot.csv / region_loot.txt 공용 컬럼
    static Dictionary<string, string> RegionLoot => new Dictionary<string, string>
    {
        { "tier",       "루트 구간" },
        { "roll_count", "굴림 수" },
        { "item_id",    "아이템 ID" },
        { "weight",     "가중치(개별 확률)" },
        { "min",        "최소 수량" },
        { "max",        "최대 수량" },
    };

    // CSV/txt 파일 자체의 한글 제목(좌측 파일 목록 표시용)
    static readonly Dictionary<string, string> Files = new Dictionary<string, string>
    {
        { "items.csv",              "아이템" },
        { "quests.csv",             "퀘스트" },
        { "region_loot.csv",        "지역 드랍 테이블" },
        { "region_items.csv",       "지역 전용 아이템" },
        { "barter.csv",             "물물교환(바터)" },
        { "dispatch.csv",           "파견" },
        { "dispatch_modifiers.csv", "파견 보정치" },
        { "hideout_modules.csv",    "은신처 모듈" },
        { "quest_rewards.csv",      "퀘스트 보상 등급" },
        { "reputation.csv",         "평판 증감" },
        { "reputation_tiers.csv",   "평판 등급" },
        { "trust.csv",              "신뢰 증감" },
        { "upkeep.csv",             "유지비" },
        { "region_loot.txt",        "지역 드랍 (런타임 반영)" },
        { "region_items.txt",       "지역 아이템 (런타임 반영)" },
    };

    /// <summary>파일명의 한글 제목 조회(좌측 목록용). 없으면 null(호출부가 파일명 그대로 노출).</summary>
    public static string FileTitle(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        return Files.TryGetValue(fileName.Trim().ToLowerInvariant(), out var t) ? t : null;
    }

    // SO 필드명(C# 변수명) → 한글. 인스펙터형 탭(NPC·상점·Player·몬스터) 공용.
    //  대소문자 그대로 매칭(C# 필드명은 camelCase 고정). 미등록은 null → 호출부가 Unity 기본 라벨 fallback.
    static readonly Dictionary<string, string> Fields = new Dictionary<string, string>
    {
        // ── NPCData ──
        { "npcId",            "NPC ID" },
        { "displayName",      "표시 이름" },
        { "role",             "역할" },
        { "initialAffinity",  "초기 호감도" },
        { "initialTrust",     "초기 신뢰" },
        { "initialFear",      "초기 두려움" },
        { "defaultDialogues", "기본 대화" },
        { "eventDialogues",   "이벤트 대화" },
        { "shopData",         "상점 데이터" },
        { "availableQuests",  "제공 퀘스트" },
        { "shopInventory",    "상점 인벤토리" },
        // ── DialogueEntry / DialogueCondition ──
        { "id",            "ID" },
        { "lines",         "대사" },
        { "conditions",    "조건" },
        { "priority",      "우선순위" },
        { "minAffinity",   "최소 호감도" },
        { "minTrust",      "최소 신뢰" },
        { "minFear",       "최소 두려움" },
        { "requiredQuest", "필요 퀘스트" },
        { "requiredFlag",  "필요 플래그" },
        // ── DialogueChoice ──
        { "text",             "선택지 텍스트" },
        { "affinityChange",   "호감도 변화" },
        { "trustChange",      "신뢰 변화" },
        { "fearChange",       "두려움 변화" },
        { "reputationChange", "평판 변화" },
        { "resultLines",      "결과 대사" },
        { "triggerQuest",     "수주 퀘스트" },
        { "setFlag",          "플래그 설정" },
        { "openShop",         "상점 열기" },
        // ── EventDialogue ──
        { "triggerCondition", "발동 조건" },
        { "npcLines",         "NPC 대사" },
        { "choices",          "선택지" },
        { "oneShot",          "1회성" },
        // ── ShopData / WantedItem ──
        { "shopId",           "상점 ID" },
        { "shopName",         "상점 이름" },
        { "stock",            "판매 목록" },
        { "buyRate",          "구매가 배율" },
        { "sellRate",         "판매가 배율" },
        { "allowConsignment", "위탁 허용" },
        { "wanted",           "수배 목록" },
        { "item",             "아이템" },
        { "premium",          "매입가 배율" },
        { "remaining",        "남은 수량" },

        // ── StatDB (Player/몬스터) ── ※ id·displayName은 위에서 이미 정의(중복 금지)
        { "playerStat", "플레이어 스탯" },
        { "units",      "유닛 목록" },
        // 공용
        { "maxHp",     "최대 체력" },
        { "moveSpeed", "이동 속도" },
        // PlayerStatData — 이동
        { "sprintSpeedMultiplier", "달리기 속도 배율" },
        { "crouchSpeedMultiplier", "웅크리기 속도 배율" },
        { "moveAccel",             "가속도" },
        { "moveDecel",             "감속도" },
        { "sprintStaminaCost",     "달리기 스태미너 소모" },
        { "sprintMinStamina",      "달리기 최소 스태미너" },
        // 모션 스탯(MotionStat 리스트 — 플레이어/적 공용)
        { "motions",               "모션(애니 속도/거리)" },
        { "anim",                  "모션 키" },
        { "animSpeed",             "애니 속도" },
        { "distance",              "이동 거리(m)" },
        // PlayerStatData — 약공격
        { "lightDamage",           "약공격 데미지" },
        { "lightRange",            "약공격 사거리" },
        { "lightStaminaCost",      "약공격 스태미너" },
        { "lightGroggy",           "약공격 그로기" },
        { "lightCooldown",         "약공격 쿨다운" },
        { "lightComboMax",         "약공격 최대 콤보" },
        { "lightComboWindow",      "약공격 콤보 입력시간" },
        { "lightCombo2Damage",     "약공격 2타 데미지" },
        { "lightCombo2Groggy",     "약공격 2타 그로기" },
        { "lightCombo3Damage",     "약공격 3타 데미지" },
        { "lightCombo3Groggy",     "약공격 3타 그로기" },
        { "lightCombo3StaminaCost","약공격 3타 스태미너" },
        // PlayerStatData — 강공격
        { "heavyDamage",          "강공격 데미지" },
        { "heavyRange",           "강공격 사거리" },
        { "heavyStaminaCost",     "강공격 스태미너" },
        { "heavyGroggy",          "강공격 그로기" },
        { "heavyChargeTime",      "강공격 차지 시간" },
        { "heavyMaxCharge",       "강공격 최대 차지" },
        { "heavyFullDamage",      "강공격 풀차지 데미지" },
        { "heavyFullStaminaCost", "강공격 풀차지 스태미너" },
        { "heavyFullGroggy",      "강공격 풀차지 그로기" },
        { "heavyCooldown",        "강공격 쿨다운" },
        // PlayerStatData — 구르기/스태미너
        { "dodgeStaminaCost",        "구르기 스태미너" },
        { "dodgeDistance",           "구르기 거리" },
        { "dodgeDuration",           "구르기 시간" },
        { "dodgeInvincibleDuration", "구르기 무적 시간" },
        { "dodgeCooldown",           "구르기 쿨다운" },
        { "maxStamina",              "최대 스태미너" },
        { "staminaRegen",            "스태미너 회복" },
        { "staminaRegenDelay",       "스태미너 회복 지연" },
        { "exhaustionDuration",      "탈진 지속" },
        // UnitStatData — 비주얼/프리팹
        { "scale",            "크기" },
        { "tintColor",        "틴트 색상" },
        { "shadowColor",      "그림자 색상" },
        { "useGlow",          "발광 사용" },
        { "glowColor",        "발광 색상" },
        { "glowIntensity",    "발광 강도" },
        { "glowRange",        "발광 범위" },
        { "useTrailParticle", "잔상 파티클 사용" },
        { "trailColor",       "잔상 색상" },
        { "generatedPrefab",  "생성된 프리팹" },
        // UnitStatData — 전투/그로기
        { "attackDamage",       "공격 데미지" },
        { "attackRange",        "공격 사거리" },
        { "attackSpeed",        "공격 속도" },
        { "attackWindup",       "공격 선딜" },
        { "canBeCancelled",     "캔슬 가능" },
        { "maxGroggy",          "최대 그로기" },
        { "groggyDecay",        "그로기 감소" },
        { "groggyStunDuration", "그로기 스턴 시간" },
        // UnitStatData — 이동/감지/AI
        { "patrolSpeed",     "순찰 속도" },
        { "detectRange",     "감지 범위" },
        { "loseRange",       "추적 해제 범위" },
        { "patrolRadius",    "순찰 반경" },
        { "patrolWaitTime",  "순찰 대기 시간" },
        { "hitStunDuration", "피격 경직 시간" },
        // UnitStatData — 보상
        { "expReward",  "경험치 보상" },
        { "goldReward", "골드 보상" },
    };

    /// <summary>SO 필드명의 한글 라벨 조회. 없으면 null(호출부가 Unity 기본 라벨을 그대로 노출).</summary>
    public static string Field(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return Fields.TryGetValue(name, out var v) ? v : null;
    }

    // [Header(...)] 섹션 제목 한글화(대부분 소스가 이미 한글이라 영문만 보강).
    static readonly Dictionary<string, string> Headers = new Dictionary<string, string>
    {
        { "Player", "플레이어" },
        { "Units",  "유닛 (적/몬스터)" },
        { "ID",     "식별" },
    };

    /// <summary>[Header] 제목 한글화. 매핑 없으면 원문 그대로(이미 한글인 경우 포함).</summary>
    public static string Header(string h)
        => string.IsNullOrEmpty(h) ? h : (Headers.TryGetValue(h, out var v) ? v : h);

    /// <summary>파일명+헤더로 한글 라벨 조회. 없으면 null(호출부가 영문 원문을 그대로 노출).</summary>
    public static string Get(string fileName, string header)
    {
        if (string.IsNullOrEmpty(header)) return null;
        string h = Normalize(header);

        if (!string.IsNullOrEmpty(fileName))
        {
            string fn = fileName.Trim().ToLowerInvariant();
            if (PerFile.TryGetValue(fn, out var map) && map.TryGetValue(h, out var ko))
                return ko;
        }
        return Global.TryGetValue(h, out var g) ? g : null;
    }

    static string Normalize(string s)
        => s.Trim().TrimStart('﻿').ToLowerInvariant();
}
