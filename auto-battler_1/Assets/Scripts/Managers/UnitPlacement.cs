using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 정비 페이즈 중 유닛 배치, 하단 벤치 슬롯 배정 및 드래그앤드롭을 전체 관할하는 핵심 매니저
/// </summary>
public class UnitPlacement : MonoBehaviour
{
    public static UnitPlacement Instance { get; private set; }

    public static bool IsBattleActive { get; private set; } = false;

    [Header("Interactive Setup")]
    [SerializeField] private LayerMask unitLayer; // 유닛 콜라이더 판별 레이어
    [SerializeField] private int maxBenchSlots = 6;
    [SerializeField] private float benchYOffset = -4.2f; // 하단 벤치 UI 영역 월드 스페이스 Y 좌표

    // 실시간 드래그 내부 캐시 데이터
    private UnitInstance draggingUnit;
    private Vector2 dragOffset;
    private GridCell originalCell;
    private int originalBenchIndex = -1;

    // 벤치 시스템 내부 버퍼
    private List<UnitInstance> benchSlots;
    private List<Vector2> benchWorldPositions;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeBenchSlots();
    }

    private void Start()
    {
        // 테스트용: 데이터 매니저로부터 기타/드럼 유닛 데이터를 로딩하여 벤치 슬롯에 테스트 자동 마운트
        SpawnBenchTestUnits();
    }

    private void InitializeBenchSlots()
    {
        benchSlots = new List<UnitInstance>(new UnitInstance[maxBenchSlots]);
        benchWorldPositions = new List<Vector2>();

        // 화면 아래에 6개의 벤치 슬롯 가상 위치 계산 (X축으로 정적 나열)
        float startX = -((maxBenchSlots - 1) * 1.3f) / 2f;
        for (int i = 0; i < maxBenchSlots; i++)
        {
            Vector2 pos = new Vector2(startX + (i * 1.3f), benchYOffset);
            benchWorldPositions.Add(pos);
        }
    }

    private void SpawnBenchTestUnits()
    {
        // 벤치 위치에 테스트용 유닛 2마리 스폰 (기타, 드럼 등 데이터 로드 연동 연계)
        UnitData guitarData = Resources.Load<UnitData>("Units/Unit_Guitar");
        UnitData drumData = Resources.Load<UnitData>("Units/Unit_Drum");

        if (guitarData != null) SpawnUnitOnBench(0, guitarData);
        if (drumData != null) SpawnUnitOnBench(1, drumData);
    }

    private void SpawnUnitOnBench(int slotIndex, UnitData data)
    {
        if (slotIndex < 0 || slotIndex >= maxBenchSlots) return;

        GameObject go = new GameObject($"BenchUnit_{data.unitName}", 
            typeof(SpriteRenderer), 
            typeof(BoxCollider2D), 
            typeof(UnitInstance),
            typeof(UnitCombat));

        go.layer = LayerMask.NameToLayer("Unit");

        // 2D 마우스 드래그를 위해 박스 콜라이더 및 콜라이더 사이즈 자동 정비
        BoxCollider2D collider = go.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(1f, 1f);

        UnitInstance unit = go.GetComponent<UnitInstance>();
        unit.SetAlliance(true); // 아군 플레이어 진영 명시

        // 리소스 데이터 주소 이식
        var serializedField = typeof(UnitInstance).GetField("unitData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (serializedField != null) serializedField.SetValue(unit, data);

        benchSlots[slotIndex] = unit;
        unit.transform.position = benchWorldPositions[slotIndex];
        unit.InitializeFromData();

        Debug.Log($"🎸 하단 벤치 UI 슬롯 [{slotIndex}]에 {unit.UnitName} 유닛이 마운트되어 표시되었습니다.");
    }

    private void Update()
    {
        // 정비 상태일 때의 마우스 드래그앤드롭 작동
        HandlePlacementDrag();
    }

    private void HandlePlacementDrag()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 마우스 다운: 유닛을 마우스 끝으로 포착
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 10f, unitLayer);
            if (hit.collider != null)
            {
                UnitInstance unit = hit.collider.GetComponent<UnitInstance>();
                if (unit != null)
                {
                    draggingUnit = unit;
                    originalCell = unit.CurrentCell;
                    originalBenchIndex = benchSlots.IndexOf(unit);

                    // 드래그 중인 임시 오프셋 캐싱
                    dragOffset = (Vector2)unit.transform.position - mousePos;
                    Debug.Log($"[{unit.UnitName}] 드래그 배정을 위한 이동을 가동합니다. 🖐️");
                }
            }
        }

        // 드래그 중: 마우스 좌표 동기화
        if (Input.GetMouseButton(0) && draggingUnit != null)
        {
            draggingUnit.transform.position = mousePos + dragOffset;
        }

        // 마우스 업: 배치 처리 판정 연산
        if (Input.GetMouseButtonUp(0) && draggingUnit != null)
        {
            GridCell targetCell = FindClosestPlayerCell(mousePos);
            int targetBenchIndex = FindClosestBenchSlotIndex(mousePos);

            if (targetCell != null)
            {
                // [1] 전장 그리드에 배치 시도
                if (targetCell.isOccupied)
                {
                    // 해당 자리에 이미 아군 유닛이 차 있다면 자리를 맞교환(Swap)
                    SwapUnits(targetCell);
                }
                else
                {
                    // 비어있다면 해당 그리드로 소속 스냅
                    if (originalBenchIndex != -1) benchSlots[originalBenchIndex] = null;
                    draggingUnit.AssignToCell(targetCell);
                    Debug.Log($"[{draggingUnit.UnitName}] 그리드 배치 성공! {targetCell.gridPosition} 챡!");
                }
            }
            else if (targetBenchIndex != -1)
            {
                // [2] 하단 벤치 UI 슬롯에 배치 시도
                UnitInstance opponentOnBench = benchSlots[targetBenchIndex];

                if (opponentOnBench == null)
                {
                    // 벤치가 완전히 비어있음 ➡️ 배치 이전 완료
                    if (originalCell != null) originalCell.isOccupied = false;
                    if (originalBenchIndex != -1) benchSlots[originalBenchIndex] = null;

                    benchSlots[targetBenchIndex] = draggingUnit;
                    draggingUnit.AssignToCell(null); // 전장 격자 해제
                    draggingUnit.transform.position = benchWorldPositions[targetBenchIndex];
                    Debug.Log($"[{draggingUnit.UnitName}] 하단 벤치 슬롯 [{targetBenchIndex}]로 이동 성공!");
                }
                else
                {
                    // 벤치에 다른 녀석이 차지하고 있음 ➡️ 드래그한 위치와 벤치 슬롯 크로스 교환
                    if (originalCell != null)
                    {
                        opponentOnBench.AssignToCell(originalCell);
                    }
                    else if (originalBenchIndex != -1)
                    {
                        benchSlots[originalBenchIndex] = opponentOnBench;
                        opponentOnBench.transform.position = benchWorldPositions[originalBenchIndex];
                    }

                    benchSlots[targetBenchIndex] = draggingUnit;
                    draggingUnit.AssignToCell(null);
                    draggingUnit.transform.position = benchWorldPositions[targetBenchIndex];
                    Debug.Log($"[{draggingUnit.UnitName}] 🔄 [{opponentOnBench.UnitName}] 배치 맞교환 완료!");
                }
            }
            else
            {
                // [3] 전장 바깥 영역에 버릴 시 제자리 원위치 롤백
                ReturnToOriginalState();
            }

            draggingUnit = null;
        }
        if (SynergyManager.Instance != null)
        {
            SynergyManager.Instance.RefreshSynergies(); // 배치가 바뀌었으니 시너지와 앞/뒷줄 보너스 전면 재정산!
        }
    }

    private GridCell FindClosestPlayerCell(Vector2 position)
    {
        GridCell closest = null;
        float minDistance = float.MaxValue;

        int currentWidth = GridManager.Instance.Width;
        int currentHeight = GridManager.Instance.Height;

        // Player 진영(isPlayerSide = true)의 격자 셀만 정밀 검색
        for (int x = 0; x < currentWidth; x++)
        {
            for (int y = 0; y < currentHeight; y++)
            {
                GridCell cell = GridManager.Instance.GetCell(x, y, true);
                if (cell != null)
                {
                    float dist = Vector2.Distance(position, cell.worldPosition);
                    if (dist < minDistance && dist < 0.9f) // 유효 범위 내 자석 보정
                    {
                        minDistance = dist;
                        closest = cell;
                    }
                }
            }
        }
        return closest;
    }

    private int FindClosestBenchSlotIndex(Vector2 position)
    {
        int closestIndex = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < benchWorldPositions.Count; i++)
        {
            float dist = Vector2.Distance(position, benchWorldPositions[i]);
            if (dist < minDistance && dist < 1.0f)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    private void SwapUnits(GridCell targetCell)
    {
        UnitInstance targetUnit = null;
        UnitInstance[] allUnits = FindObjectsOfType<UnitInstance>();
        foreach (var u in allUnits)
        {
            if (u.CurrentCell == targetCell)
            {
                targetUnit = u;
                break;
            }
        }

        if (targetUnit != null)
        {
            if (originalCell != null)
            {
                targetUnit.AssignToCell(originalCell);
                draggingUnit.AssignToCell(targetCell);
            }
            else if (originalBenchIndex != -1)
            {
                // 벤치 ➡️ 그리드 스와핑 처리
                benchSlots[originalBenchIndex] = targetUnit;
                targetUnit.AssignToCell(null);
                targetUnit.transform.position = benchWorldPositions[originalBenchIndex];

                draggingUnit.AssignToCell(targetCell);
                benchSlots[originalBenchIndex] = targetUnit;
            }
        }
        else
        {
            ReturnToOriginalState();
        }
    }

    private void ReturnToOriginalState()
    {
        if (draggingUnit == null) return;

        if (originalCell != null)
        {
            draggingUnit.AssignToCell(originalCell);
        }
        else if (originalBenchIndex != -1)
        {
            draggingUnit.transform.position = benchWorldPositions[originalBenchIndex];
            benchSlots[originalBenchIndex] = draggingUnit;
        }
    }

    public void StartSimulationDirect()
    {
        StartBattleSimulation();
    }

    public void StopSimulationAndResetDirect()
    {
        StopSimulationAndReset();
    }

    private void StartBattleSimulation()
    {
        if (IsBattleActive) return;
        IsBattleActive = true;

        UnitInstance[] allUnits = FindObjectsByType<UnitInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var unit in allUnits)
        {
            if (unit != null)
            {
                // ★ [추가] 전투 시작 전, 현재 밟고 있는 완벽한 타일 주소를 영구 박제시킵니다!
                unit.RecordBattleStartPosition();
            }
        }

        Debug.Log("<color=#FFD700>🔊 [전투 개시] 스페이스바가 입력되었습니다. 유닛들의 FSM 지능과 탐색기가 가동됩니다!</color>");
    }

    private void StopSimulationAndReset()
    {
        IsBattleActive = false;

        // 유니티 표준 API를 사용해 꺼져있는 유닛까지 씬에서 전수 조사합니다.
        // FindObjectsSortMode.None을 주어 성능 최적화와 비활성 오브젝트 수집을 동시에 달성합니다.
        UnitInstance[] allUnits = FindObjectsByType<UnitInstance>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Debug.Log($"[리셋 시동] 씬에서 발견된 총 유닛 수(사망 포함): {allUnits.Length}마리");

        foreach (var unit in allUnits)
        {
            // 프리팹 에셋 유실이나 찌꺼기 방어선
            if (unit == null) continue;

            // 1. 꺼져있던 사망 유닛들의 불을 먼저 켭니다.
            unit.gameObject.SetActive(true);

            // 2. 컴뱃 컴포넌트 타겟팅 정보 및 코루틴 완벽 클린업
            if (unit.TryGetComponent(out UnitCombat c))
            {
                c.ResetCombatTarget();
            }

            // 3. 체력 풀피 복구 및 FSM 상태 초기화 (ReviveAndReset)
            unit.ReviveAndReset();

            // 4. ★ [위치 복구 보장] 시작 전 기억해둔 원래 격자 타일 포지션으로 강제 텔레포트
            if (unit.CurrentCell != null)
            {
                unit.transform.position = unit.CurrentCell.worldPosition;
                unit.CurrentCell.isOccupied = true;
                Debug.Log($"🔄 {unit.UnitName} -> 원래 타일 위치({unit.CurrentCell.gridPosition.x}, {unit.CurrentCell.gridPosition.y})로 복구 완료.");
            }
            else
            {
                // 배틀 존 타일이 없는 벤치 유닛 포지션 복구
                unit.InitializeFromData();
            }
        }
        if (SynergyManager.Instance != null)
        {
            SynergyManager.Instance.RefreshSynergies();
        }
        Debug.Log("<color=yellow>🔄 [전투 리셋 완료] 모든 유닛이 부활하고 원본 배치 자리로 복귀했습니다.</color>");
    }

    private void OnDrawGizmos()
    {
        // 에디터 모드에서 하단 벤치 드롭존의 시각 가이드 드로잉 (노란 원)
        if (benchWorldPositions == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < benchWorldPositions.Count; i++)
        {
            Gizmos.DrawWireSphere(benchWorldPositions[i], 0.4f);
        }
    }
}