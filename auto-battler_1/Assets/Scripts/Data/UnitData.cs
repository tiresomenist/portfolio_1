using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "AutoBattler/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("기본 신상 정보")]
    public string unitName;
    public Sprite unitSprite; // 유닛의 외형 임시 이미지

    [Header("전투 원천 능력치 (정적 설계도)")]
    public int baseHP = 100;           // 관객 호응도(HP) 기본값
    public int baseAttackDamage = 15;   // 기본 공격력
    public float baseAttackSpeed = 1.0f; // 초당 공격 횟수 (공속)
    public int baseAttackRange = 1;     // 격자 칸 단위 사거리 (1 = 근거리)
}