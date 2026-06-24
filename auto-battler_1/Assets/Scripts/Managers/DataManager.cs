using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    // 기획 데이터베이스 메모리 적재 컬렉션
    public List<UnitData> AllUnits { get; private set; } = new List<UnitData>();
    public List<RelicData> AllRelics { get; private set; } = new List<RelicData>();
    public List<SynergyData> AllSynergies { get; private set; } = new List<SynergyData>();
    public List<EventData> AllEvents { get; private set; } = new List<EventData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadAllStaticData();
    }

    private void LoadAllStaticData()
    {
        // Assets/Resources/ 하위 폴더 기반 고속 일괄 로드 장치 가동
        AllUnits.AddRange(Resources.LoadAll<UnitData>("Units"));
        AllRelics.AddRange(Resources.LoadAll<RelicData>("Relics"));
        AllSynergies.AddRange(Resources.LoadAll<SynergyData>("Synergies"));
        AllEvents.AddRange(Resources.LoadAll<EventData>("Events"));
    }

    public UnitData GetUnitByName(string name)
    {
        return AllUnits.Find(u => u.unitName == name);
    }

    public RelicData GetRandomRelic()
    {
        if (AllRelics.Count == 0) return null;

        int randomIndex = Random.Range(0, AllRelics.Count);
        return AllRelics[randomIndex];
    }
}
