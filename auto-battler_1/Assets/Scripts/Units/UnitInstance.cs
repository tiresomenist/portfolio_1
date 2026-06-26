using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 유닛의 실시간 능력치(HP, 공격력, 사거리 등)와 데이터 동기화를 담당하는 컴포넌트
/// </summary>
[RequireComponent(typeof(UnitFSM))]
public class UnitInstance : MonoBehaviour
{
    [Header("Unit Profile Reference")]
    [SerializeField] private UnitData unitData; // ScriptableObject 설계도 원본

    [Header("Runtime Live Stats")]
    [SerializeField] private bool isPlayerSide = true;
    [SerializeField] private int maxHP;
    [SerializeField] private int currentHP;
    [SerializeField] private int currentAttackDamage;
    [SerializeField] private float currentAttackSpeed;
    [SerializeField] private int currentAttackRange;

    // 캐싱 컴포넌트 참조
    private UnitFSM fsm;
    private GridCell assignedCell;
    private SpriteRenderer spriteRenderer;

    // 동적 월드 스페이스 HP바 UI 슬라이더
    private Slider hpSlider;

    //시작 포지션 백업
    private Vector3 initialPosition;

    // 외부 연동용 안전한 게터 프로퍼티
    public string UnitName
    {
        get
        {
            // 유닛 설계도 원본 이름 (기타, 드럼 등)
            string rawName = unitData != null ? unitData.unitName : "Unknown";

            // 유니티 오브젝트마다 부여되는 고유 해시 ID값 추출 (예: 2541, 4812)
            int uniqueId = gameObject.GetInstanceID();
            // 가독성을 위해 양수 4자리 정도로 슬라이싱
            string shortId = Mathf.Abs(uniqueId % 10000).ToString("D4");

            if (isPlayerSide)
            {
                // 아군은 파란 계열(Cyan)로 [아군_기타#1254] 형태 출력
                return $"<color=#00FFFF>[아군_{rawName}#{shortId}]</color>";
            }
            else
            {
                // 적군은 붉은 계열(Light Red)로 [적군_드럼#9512] 형태 출력
                return $"<color=#FF6B6B>[적군_{rawName}#{shortId}]</color>";
            }
        }
    }
    public int AttackDamage => currentAttackDamage;
    public float AttackSpeed => currentAttackSpeed;
    public int AttackRange => currentAttackRange;
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public bool IsPlayerSide => isPlayerSide;
    public bool IsDead => currentHP <= 0;
    public GridCell CurrentCell => assignedCell;
    public Vector3 InitialPosition => initialPosition;

    private void Awake()
    {
        fsm = GetComponent<UnitFSM>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        initialPosition = transform.position;

        InitializeFromData();
        CreateWorldSpaceHPBar();
    }

    /// <summary>
    /// ScriptableObject 에셋 데이터를 읽어와 실시간 능력치 버퍼에 대입합니다.
    /// </summary>
    public void InitializeFromData()
    {
        if (unitData != null)
        {
            maxHP = unitData.baseHP;
            currentHP = maxHP;
            currentAttackDamage = unitData.baseAttackDamage;
            currentAttackSpeed = unitData.baseAttackSpeed;
            currentAttackRange = unitData.baseAttackRange;

            // 명세서에 있는 스프라이트 비주얼 자동 동기화
            if (unitData.unitSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = unitData.unitSprite;
            }
        }
        else
        {
            // 데이터 누락 시 오작동 방지용 안전 디폴트값 설정
            maxHP = 100;
            currentHP = maxHP;
            currentAttackDamage = 15;
            currentAttackSpeed = 1.0f;
            currentAttackRange = 1;
        }

        fsm.SetInitialState();
        UpdateHPBarUI();
    }

    /// <summary>
    /// 영토 배정 및 좌표 스냅핑을 제어합니다.
    /// </summary>
    public void AssignToCell(GridCell cell)
    {
        if (assignedCell != null)
        {
            assignedCell.isOccupied = false;
        }

        assignedCell = cell;

        if (assignedCell != null)
        {
            assignedCell.isOccupied = true;
            transform.position = assignedCell.worldPosition; // 전장의 셀 중심점으로 자석 정렬 스냅
        }
    }

    /// <summary>
    /// 실시간 대미지 연산기 및 UI 동기화
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        int previousHP = currentHP;

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        UpdateHPBarUI();

        Debug.Log($"[{UnitName}] 대미지 수신: HP {previousHP} -> {currentHP}");

        if (currentHP <= 0)
        {
            fsm.TransitionTo(UnitFSM.FsmState.Dead);
        }
    }

    public void SetAlliance(bool isPlayer)
    {
        isPlayerSide = isPlayer;
    }

    /// <summary>
    /// 에디터 수동 UI 작업을 원천 생략하고, 코드 가동 시 유닛 머리 위에 월드스페이스 HP 슬라이더를 런타임에 동적 생성합니다.
    /// </summary>
    private void CreateWorldSpaceHPBar()
    {
        // 1. World Space Canvas 게임 오브젝트 생성
        GameObject canvasGo = new GameObject("FloatingHPCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform);
        canvasGo.transform.localPosition = new Vector3(0, 0.9f, 0); // 머리 위 오프셋
        canvasGo.transform.localScale = Vector3.one * 0.01f; // 월드 스페이스 스케일 1/100 압축

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        // 2. 슬라이더 배경 플레이트 틀 생성
        GameObject bgGo = new GameObject("Background", typeof(Image));
        bgGo.transform.SetParent(canvasGo.transform, false);
        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(80, 12);
        bgGo.GetComponent<Image>().color = new Color(0.3f, 0.1f, 0.1f, 0.8f);

        // 3. 슬라이더 실시간 피 게이지 생성 (아군/적군 색상 분기)
        GameObject fillGo = new GameObject("Fill", typeof(Image));
        fillGo.transform.SetParent(canvasGo.transform, false);
        RectTransform fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(80, 12);
        fillGo.GetComponent<Image>().color = isPlayerSide ? Color.green : Color.red;

        // 4. 슬라이더 컴포넌트 구성 요소 빌드
        hpSlider = canvasGo.AddComponent<Slider>();
        hpSlider.transition = Slider.Transition.None;
        hpSlider.interactable = false;
        hpSlider.targetGraphic = fillGo.GetComponent<Image>();
        hpSlider.fillRect = fillRect;

        // 피붕 기준 정렬을 통해 체력이 왼쪽에서 오른쪽 방향으로 차오르고 감소하게 보정
        fillRect.anchorMin = new Vector2(0, 0.5f);
        fillRect.anchorMax = new Vector2(0, 0.5f);
        fillRect.pivot = new Vector2(0, 0.5f);

        UpdateHPBarUI();
    }

    private void UpdateHPBarUI()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }
    }
}