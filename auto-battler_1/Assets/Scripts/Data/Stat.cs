using System.Collections.Generic;
using UnityEngine;

public enum ModifierType
{
    Flat,       // 합연산 (예: 공격력 +15)
    Percent     // 곱연산 (예: 시너지로 공속 +20% -> +0.2f)
}

public enum ModifierSource
{
    Synergy,       // 종족/직업 합주 시너지
    PositionBonus, // 앞줄/뒷줄 포지션 버프
    Relic,         // 유물 (차후 확장용)
    SkillBuff      // 실시간 스킬 버프/디버프 (차후 확장용)
}

[System.Serializable]
public class StatModifier
{
    public float value;
    public ModifierType type;
    public ModifierSource source;

    public StatModifier(float value, ModifierType type, ModifierSource source)
    {
        this.value = value;
        this.type = type;
        this.source = source;
    }
}

[System.Serializable]
public class Stat
{
    [SerializeField] private float baseValue;
    private readonly List<StatModifier> modifiers = new List<StatModifier>();

    public float BaseValue { get => baseValue; set => baseValue = value; }

    public Stat(float baseValue)
    {
        this.baseValue = baseValue;
    }

    public void AddModifier(StatModifier mod)
    {
        modifiers.Add(mod);
    }

    public void RemoveModifiersFromSource(ModifierSource source)
    {
        modifiers.RemoveAll(mod => mod.source == source);
    }

    /// <summary>
    /// 기획 정산 순서에 맞춰 최종 스탯을 동적 연산하는 파이프라인
    /// </summary>
    public float GetFinalValue()
    {
        float finalValue = baseValue;
        float percentSum = 0f;

        // 1. 고정치 합연산 (Flat) 선처리
        foreach (var mod in modifiers)
        {
            if (mod.type == ModifierType.Flat)
                finalValue += mod.value;
        }

        // 2. 배율 곱연산 (Percent) 후처리
        foreach (var mod in modifiers)
        {
            if (mod.type == ModifierType.Percent)
                percentSum += mod.value;
        }

        finalValue *= (1.0f + percentSum);
        return finalValue;
    }
}