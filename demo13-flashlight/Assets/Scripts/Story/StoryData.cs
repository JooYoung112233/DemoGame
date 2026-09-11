using UnityEngine;
using System;

/// <summary>
/// 스토리 스크립트 JSON 역직렬화용 데이터 클래스.
///
/// 구조:
///   Scripts/ch0.json  → StoryScript (씬 흐름, 언어 무관)
///   Locale/ko.json    → LocaleData  (텍스트, 언어별)
/// </summary>

// ═══════════════════════════════
//  스크립트 (씬 흐름)
// ═══════════════════════════════

[Serializable]
public class StoryScript
{
    public StoryScene[] scenes;
}

[Serializable]
public class StoryScene
{
    [Tooltip("씬 ID (예: S-000)")]
    public string id;

    [Tooltip("씬 제목 (디버그/에디터용)")]
    public string title;

    [Tooltip("이 씬의 자동 트리거 조건. 비어있으면 수동 호출.")]
    public string triggerFlag;

    [Tooltip("true면 1회만 실행")]
    public bool oneShot = true;

    public StoryNode[] nodes;
}

[Serializable]
public class StoryNode
{
    /// <summary>
    /// 노드 타입:
    /// narration  — NarrationUI로 독백 표시
    /// dialogue   — DialogueUI로 NPC 대사 표시
    /// choice     — 선택지 분기
    /// tutorial   — TutorialPrompt 표시
    /// effect     — 화면 이펙트 (fade, shake, chromatic 등)
    /// system     — 시스템 호출 (퀘스트, 플래그, 아이템 등)
    /// condition  — 조건 분기 (플래그 체크)
    /// </summary>
    public string type;

    // ── narration / dialogue 공통 ──
    /// <summary>로케일 키 배열. 한 줄씩 순서대로 표시.</summary>
    public string[] textKeys;

    // ── dialogue 전용 ──
    /// <summary>화자 식별자 (pawnshop, player, merchant, bandit 등)</summary>
    public string speaker;
    /// <summary>화자 이름 로케일 키</summary>
    public string speakerKey;

    // ── choice 전용 ──
    public string choiceId;
    public StoryChoice[] options;

    // ── tutorial 전용 ──
    public string textKey;
    public float duration;
    public string tutId;

    // ── effect 전용 ──
    /// <summary>이펙트 종류: fade_in, fade_out, shake, chromatic, freeze, flash, grayscale_on, grayscale_off</summary>
    public string effect;
    public float floatParam;
    public float floatParam2;

    // ── system 전용 ──
    /// <summary>
    /// 시스템 액션:
    /// set_flag, clear_flag, quest_accept, quest_complete,
    /// give_item, unlock_map_night, unlock_shop, unlock_daily
    /// </summary>
    public string action;
    public string param;
    public string param2;
    public int intParam;

    // ── condition 전용 ──
    public string flag;
    /// <summary>조건 충족 시 점프할 노드 인덱스 (0-based, 같은 씬 내)</summary>
    public int ifTrueGoto = -1;
    /// <summary>조건 미충족 시 점프할 노드 인덱스</summary>
    public int ifFalseGoto = -1;
    /// <summary>조건 미충족 시 이 노드를 스킵할지</summary>
    public bool skipIfFalse;
}

[Serializable]
public class StoryChoice
{
    /// <summary>선택지 텍스트 로케일 키</summary>
    public string textKey;

    /// <summary>선택 후 NPC 응답 로케일 키 배열</summary>
    public string[] responseKeys;

    /// <summary>선택 시 적용될 효과</summary>
    public StoryChoiceEffect[] effects;
}

[Serializable]
public class StoryChoiceEffect
{
    /// <summary>효과 타입: affinity, trust, fear, flag</summary>
    public string type;
    /// <summary>대상 NPC ID 또는 플래그 이름</summary>
    public string target;
    /// <summary>변경 값</summary>
    public int value;
}

// ═══════════════════════════════
//  로케일 (텍스트)
// ═══════════════════════════════

[Serializable]
public class LocaleData
{
    public LocaleEntry[] entries;
}

[Serializable]
public class LocaleEntry
{
    public string key;
    public string value;
}
