using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SynergyManager : MonoBehaviour
{
    public static SynergyManager Instance { get; private set; }

    [Header("시너지 디스플레이 UI 캔버스 뷰")]
    [SerializeField] private GameObject synergyUIContainer;
    [SerializeField] private TMP_Text synergyInfoTextPrefab;

    private Dictionary<UnitGenre, int> genreCounts = new Dictionary<UnitGenre, int>();
    private Dictionary<UnitClass, int> classCounts = new Dictionary<UnitClass, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RefreshSynergies()
    {
        CalculateSynergies();
        ApplySynergies();
        UpdateSynergyUI();
    }

    private void CalculateSynergies()
    {
        genreCounts.Clear();
        classCounts.Clear();

        UnitInstance[] activeUnits = FindObjectsByType<UnitInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        //중복 유닛의 시너지 증감을 막음
        HashSet<string> uniqueUnitTypes = new HashSet<string>();

        foreach (var unit in activeUnits)
        {
            if (unit == null || unit.IsDead || !unit.IsPlayerSide || unit.CurrentCell == null) continue;

            if (!uniqueUnitTypes.Contains(unit.UnitName))
            {
                uniqueUnitTypes.Add(unit.UnitName);

                //유닛이 가진 List를 읽어 각각 카운트를 누적합니다.
                foreach (var genre in unit.GenresList)
                {
                    if (genre != UnitGenre.None)
                    {
                        genreCounts[genre] = genreCounts.GetValueOrDefault(genre, 0) + 1;
                    }
                }

                //유닛이 가진 List를 읽어 각각 카운트를 누적합니다.
                foreach (var uClass in unit.ClassesList)
                {
                    if (uClass != UnitClass.None)
                    {
                        classCounts[uClass] = classCounts.GetValueOrDefault(uClass, 0) + 1;
                    }
                }
            }

            // 개별 배치 위치 보너스 정산
            unit.ApplyPositionBonus();
        }
    }

    private void ApplySynergies()
    {
        float globalHpMod = 0.0f;
        float globalAtkMod = 0.0f;
        float globalSpdMod = 0.0f;

        // ---------------- [ 1. 종족 단계별 시너지 연산 ] ----------------
        if (genreCounts.GetValueOrDefault(UnitGenre.Electronic, 0) >= 4) globalSpdMod += 0.25f;
        else if (genreCounts.GetValueOrDefault(UnitGenre.Electronic, 0) >= 2) globalSpdMod += 0.10f;

        if (genreCounts.GetValueOrDefault(UnitGenre.Classic, 0) >= 4) globalHpMod += 0.35f;
        else if (genreCounts.GetValueOrDefault(UnitGenre.Classic, 0) >= 2) globalHpMod += 0.15f;

        if (genreCounts.GetValueOrDefault(UnitGenre.Metal, 0) >= 4) globalAtkMod += 0.30f;
        else if (genreCounts.GetValueOrDefault(UnitGenre.Metal, 0) >= 2) globalAtkMod += 0.15f;

        // ---------------- [ 2. 직업 단계별 시너지 연산 ] ----------------
        if (classCounts.GetValueOrDefault(UnitClass.String, 0) >= 2) globalSpdMod += 0.15f;
        if (classCounts.GetValueOrDefault(UnitClass.Percussion, 0) >= 2) globalHpMod += 0.20f;
        if (classCounts.GetValueOrDefault(UnitClass.Vocal, 0) >= 2) globalAtkMod += 0.20f;

        
        // 종합 버프 전파
        UnitInstance[] allUnits = FindObjectsByType<UnitInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var unit in allUnits)
        {
            if (unit == null || !unit.IsPlayerSide || unit.CurrentCell == null) continue;
            unit.ApplySynergyModifiers(globalHpMod, globalAtkMod, globalSpdMod);
        }
    }

    private void UpdateSynergyUI()
    {
        if (synergyUIContainer == null || synergyInfoTextPrefab == null) return;

        foreach (Transform child in synergyUIContainer.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (var kvp in genreCounts)
        {
            TMP_Text txt = Instantiate(synergyInfoTextPrefab, synergyUIContainer.transform);
            string highlightColor = kvp.Value >= 2 ? "#00FFFF" : "#FFFFFF";
            txt.text = $"Genre: {kvp.Key} ({kvp.Value} Active)";
            txt.color = ExtensionColorParse(highlightColor);
        }

        foreach (var kvp in classCounts)
        {
            TMP_Text txt = Instantiate(synergyInfoTextPrefab, synergyUIContainer.transform);
            string highlightColor = kvp.Value >= 2 ? "#00FF00" : "#FFFFFF";
            txt.text = $"Class: {kvp.Key} ({kvp.Value} Active)";
            txt.color = ExtensionColorParse(highlightColor);
        }
    }

    // 한글 폰트 깨짐 및 마크업 꼬임을 방지하기 위해 텍스트 색상을 안전하게 치환해 주는 헬퍼 함수
    private Color ExtensionColorParse(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color)) return color;
        return Color.white;
    }
}