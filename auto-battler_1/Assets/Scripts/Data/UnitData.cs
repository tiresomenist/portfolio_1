using System.Collections.Generic;
using UnityEngine;

// 스킬 성격을 규정하기 위한 enum
public enum SkillType
{
    SingleDamage,   // 단일 대상 강타
    AoEDamage,      // 범위(AOE) 피해
    TeamBuff,       // 주변 아군 버프
    EnemyDebuff,    // 맞춘 적군 디버프
    CrowdControl    // 군중 제어 (기절/행동불가)
}

//유닛 장르
public enum UnitGenre
{
    Electronic,
    Classic,
    Metal,
    None
}

//유닛 종류
public enum UnitClass
{
    String,
    Percussion,
    Vocal,
    None
}

// 다중 시너지 구조를 위한 List
[CreateAssetMenu(fileName = "NewUnitData", menuName = "AutoBattler/UnitData")]
public partial class UnitData : ScriptableObject
{
    [Header("기본 신상 정보")]
    public string unitName;
    public Sprite unitSprite;

    [Header("전투 원천 능력치")]
    public int baseHP = 100;
    public int baseAttackDamage = 15;
    public float baseAttackSpeed = 1.0f;
    public int baseAttackRange = 1;
    public int baseMaxMana = 100;       // ★ [추가] 최대 마나 기본값 설정
    public int baseDefense = 10;

    [Header("원거리 유닛 전용 설정")]
    public Projectile projectilePrefab;

    // ★ [오늘 자 추가 핵심 필드] 스킬 인프라 데이터 구축
    [Header("고유 스킬 구성 정보")]
    public SkillType skillType;         // 스킬 타입 분류
    public float skillValue;            // 스킬 수형 (데미지, 버프량, 기절 지속시간 등)
    public float skillRadius = 2.0f;    // 범위형 스킬인 경우 적용할 가상 반경
    public ParticleSystem skillEffectPrefab; // 스킬 시전 시 터트릴 유니티 기본 파티클 프리팹

    [Header("시너지 태그")]
    public List<UnitGenre> unitGenres = new List<UnitGenre>();
    public List<UnitClass> unitClasses = new List<UnitClass>();
}