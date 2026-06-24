using UnityEngine;

public class DataValidator : MonoBehaviour
{
    private void Start()
    {
        // DataManager가 Awake에서 로드를 끝낸 후 호출되도록 보장
        VerifyStaticData();
    }

    private void VerifyStaticData()
    {
        Debug.Log("<color=cyan><b>[DataValidator] Static Data Verification Start!</b></color>");

        // 1. 전체 유닛 로드 검증 및 특정 이름 탐색 테스트
        var allUnits = DataManager.Instance.AllUnits;
        Debug.Log($"[Unit Load] 로드된 총 유닛 수: {allUnits.Count}");
        foreach (var unit in allUnits)
        {
            Debug.Log($" -> 로드된 유닛 에셋 파일명: {unit.name} | 설정된 이름: {unit.unitName}");
        }

        UnitData searchedUnit = DataManager.Instance.GetUnitByName("기타");
        if (searchedUnit != null)
        {
            Debug.Log($"<color=green>[Success]</color> GetUnitByName('기타') 탐색 성공! 파일명: {searchedUnit.name}");
        }
        else
        {
            Debug.Log("<color=red>[Fail]</color> GetUnitByName('기타') 탐색 실패. 인스펙터의 Unit Name을 확인하세요.");
        }

        // 2. 전체 유물 로드 검증 및 랜덤 획득 테스트
        var allRelics = DataManager.Instance.AllRelics;
        Debug.Log($"[Relic Load] 로드된 총 유물 수: {allRelics.Count}");

        RelicData randomRelic = DataManager.Instance.GetRandomRelic();
        if (randomRelic != null)
        {
            Debug.Log($"<color=green>[Success]</color> GetRandomRelic() 가동 성공! 뽑힌 유물: {randomRelic.relicName}");
        }
        else
        {
            Debug.Log("<color=red>[Fail]</color> 유물 로드 실패. Relics 폴더에 에셋이 배치되었는지 확인하세요.");
        }
    }
}