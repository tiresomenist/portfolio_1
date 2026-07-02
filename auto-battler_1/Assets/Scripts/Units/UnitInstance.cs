using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
    [SerializeField] private int currentHP;
    [SerializeField] private int currentAttackRange;
    [SerializeField] private int maxMana = 100;
    [SerializeField] private int currentMana = 0;

    [Header("독립형 Stat")]
    private Stat maxHpStat;                     // 최대 체력 관리 객체
    private Stat attackDamageStat;              // 공격력 관리 객체
    private Stat attackSpeedStat;               // 공격 속도 관리 객체
    private Stat defenseStat;                   // 방어력 관리 객체
    private Stat dmgReductionPercentStat;       // 받는 피해 감소 % 관리 객체
    private Stat dmgReductionFlatStat;          // 받는 피해 고정치 관리 객체

    [Header("Battle Reset Backup")]
    private GridCell battleStartCell; // ★ 전투 시작 시점의 타일을 기억할 영구 저장소

    // ★ [추가] 매프레임 중복 로그 및 스탯 가산을 막기 위한 락(Lock) 변수
    private int lastEvaluatedY = -1;


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

            // 유니티 오브젝트마다 부여되는 고유 해시 ID값 추출
            int uniqueId = gameObject.GetInstanceID();
            
            // 가독성을 위해 양수 4자리 정도로 슬라이싱
            string shortId = Mathf.Abs(uniqueId % 10000).ToString("D4");

            if (isPlayerSide)
            {
                return $"<color=#00FFFF>[아군_{rawName}#{shortId}]</color>";
            }
            else
            {
                return $"<color=#FF6B6B>[적군_{rawName}#{shortId}]</color>";
            }
        }
    }

    public void RecordBattleStartPosition()
    {
        battleStartCell = CurrentCell;
    }


    public int AttackDamage => attackDamageStat != null ? Mathf.RoundToInt(attackDamageStat.GetFinalValue()) : (unitData != null ? unitData.baseAttackDamage : 0);
    public float AttackSpeed => attackSpeedStat != null ? attackSpeedStat.GetFinalValue() : (unitData != null ? unitData.baseAttackSpeed : 1.0f);
    public int MaxHP => maxHpStat != null ? Mathf.RoundToInt(maxHpStat.GetFinalValue()) : (unitData != null ? unitData.baseHP : 100);
    public int Defense => defenseStat != null ? Mathf.RoundToInt(defenseStat.GetFinalValue()) : (unitData != null ? unitData.baseDefense : 10);
  
    public int AttackRange => currentAttackRange;
    public int CurrentHP => currentHP;
    public bool IsPlayerSide => isPlayerSide;
    public bool IsDead => currentHP <= 0;
    public GridCell CurrentCell => assignedCell;
    public Vector3 InitialPosition => initialPosition;
    public Projectile ProjectilePrefab => unitData != null ? unitData.projectilePrefab : null;
    public int CurrentMana => currentMana;
    public int MaxMana => maxMana;
    public SkillType UnitSkillType => unitData != null ? unitData.skillType : SkillType.SingleDamage;
    public float SkillValue => unitData != null ? unitData.skillValue : 0f;
    public float SkillRadius => unitData != null ? unitData.skillRadius : 2.0f;
    public ParticleSystem SkillEffectPrefab => unitData != null ? unitData.skillEffectPrefab : null;
    public List<UnitGenre> GenresList => unitData != null ? unitData.unitGenres : new List<UnitGenre>();
    public List<UnitClass> ClassesList => unitData != null ? unitData.unitClasses : new List<UnitClass>();
    
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
            maxHpStat = new Stat(unitData.baseHP);
            attackDamageStat = new Stat(unitData.baseAttackDamage);
            attackSpeedStat = new Stat(unitData.baseAttackSpeed);
            defenseStat = new Stat(unitData.baseDefense);

            dmgReductionPercentStat = new Stat(0f);
            dmgReductionFlatStat = new Stat(0f);

            lastEvaluatedY = -1;

            currentHP = MaxHP;
            currentAttackRange = unitData.baseAttackRange;
            maxMana = unitData.baseMaxMana;
            currentMana = 0;

            // 명세서에 있는 스프라이트 비주얼 자동 동기화
            if (unitData.unitSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = unitData.unitSprite;
            }
        }
        else
        {
            // 데이터 누락 시 오작동 방지용 안전 디폴트값 설정
            maxHpStat = new Stat(100);
            attackDamageStat = new Stat(15);
            attackSpeedStat = new Stat(1.0f);
            defenseStat = new Stat(10);
            dmgReductionPercentStat = new Stat(0f);
            dmgReductionFlatStat = new Stat(0f);
            currentHP = MaxHP;
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

        // [1단계: 방어력 공식 적용] (방어력 1당 유효 체력 1% 정비례 증가)
        float currentDef = Mathf.Max(0f, defenseStat.GetFinalValue());
        float armorMultiplier = 100f / (100f + currentDef);
        float step1Damage = damage * armorMultiplier;

        // [2단계: 받는 피해 감소 % 적용 (곱연산)]
        float dmgRedPercent = Mathf.Clamp(dmgReductionPercentStat.GetFinalValue(), 0f, 0.9f); // 최대 90% 감면 가드라인
        float step2Damage = step1Damage * (1.0f - dmgRedPercent);

        // [3단계: 받는 피해 고정치 차감 및 최소 1 대미지 보장]
        float dmgRedFlat = Mathf.Max(0f, dmgReductionFlatStat.GetFinalValue());
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(step2Damage - dmgRedFlat));

        // [4단계: 실시간 체력 인가]
        int previousHP = currentHP;
        currentHP = Mathf.Clamp(currentHP - finalDamage, 0, MaxHP);
        UpdateHPBarUI();

        Debug.Log($"[{UnitName}] 대미지 수신: HP {previousHP} -> {currentHP}");

        GainMana(5);

        if (currentHP <= 0)
        {
            fsm.TransitionTo(UnitFSM.FsmState.Dead);
        }
    }

    public void SetAlliance(bool isPlayer)
    {
        isPlayerSide = isPlayer;
    }

    public void ReviveAndReset()
    {
        if (unitData != null)
        {
            // ESC 리셋이나 정비창으로 돌아올 때 모든 동적 실시간 전투 버프(스킬 디버프 포함) 청소
            maxHpStat.RemoveModifiersFromSource(ModifierSource.SkillBuff);
            attackDamageStat.RemoveModifiersFromSource(ModifierSource.SkillBuff);
            attackSpeedStat.RemoveModifiersFromSource(ModifierSource.SkillBuff);
            defenseStat.RemoveModifiersFromSource(ModifierSource.SkillBuff);
        }

        currentHP = MaxHP;
        currentMana = 0;
        UpdateHPBarUI();

        // 시작할 때 박제해둔 내 원래 좌표로 즉시 복귀
        transform.position = initialPosition;

        // CurrentCell 변수 자체를 대입하는 것이 아니라,
        // 내가 원래 밟고 있던 타일의 점유 데이터 상태만 데이터상으로 true로 복구합니다.
        if (CurrentCell != null)
        {
            CurrentCell.isOccupied = true;
        }

        lastEvaluatedY = -1;

        // FSM 상태도 Idle로 안전하게 초기화
        if (fsm == null) fsm = GetComponent<UnitFSM>();
        if (fsm != null) fsm.SetInitialState();
    }

    // ★ 마나 가산 처리기 (게이지가 가득 차면 true 반환)
    public bool GainMana(int amount)
    {
        if (IsDead) return false;

        currentMana += amount;
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);

        // UI 연동 (차후 마나바 UI 추가 시 연계 가능)
        Debug.Log($"[{UnitName}] 마나 변동: {currentMana} / {maxMana} (+{amount})");

        if (currentMana >= maxMana)
        {
            return true; // 마나 가득 참 알림
        }
        return false;
    }
    // ★ [추가] 스킬 시전 후 마나 초기화 장치
    public void UseMana()
    {
        currentMana = 0;
    }

    public void ApplySynergyModifiers(float hpPercent, float atkPercent, float spdPercent)
    {
        if (maxHpStat == null || attackDamageStat == null || attackSpeedStat == null) return;

        maxHpStat.RemoveModifiersFromSource(ModifierSource.Synergy);
        attackDamageStat.RemoveModifiersFromSource(ModifierSource.Synergy);
        attackSpeedStat.RemoveModifiersFromSource(ModifierSource.Synergy);

        if (hpPercent > 0f) maxHpStat.AddModifier(new StatModifier(hpPercent, ModifierType.Percent, ModifierSource.Synergy));
        if (atkPercent > 0f) attackDamageStat.AddModifier(new StatModifier(atkPercent, ModifierType.Percent, ModifierSource.Synergy));
        if (spdPercent > 0f) attackSpeedStat.AddModifier(new StatModifier(spdPercent, ModifierType.Percent, ModifierSource.Synergy));
        
        if (UnitPlacement.Instance != null && !UnitPlacement.IsBattleActive)
        {
            currentHP = MaxHP;
        }
        UpdateHPBarUI();
    }

    public void ApplyPositionBonus()
    {
        if (CurrentCell == null || maxHpStat == null || attackSpeedStat == null) return;

        if (lastEvaluatedY == CurrentCell.gridPosition.y) return;

        lastEvaluatedY = CurrentCell.gridPosition.y;

        // 기존 배치 버프 처리
        maxHpStat.RemoveModifiersFromSource(ModifierSource.PositionBonus);
        attackSpeedStat.RemoveModifiersFromSource(ModifierSource.PositionBonus);

        int totalRows = 3; // GridManager 참조 실패를 대비한 안전 디폴트값 보장
        if (GridManager.Instance != null)
        {
            // ★ [팁] GridManager 내부에 아군 세로 크기를 담아둔 변수명(예: playerRows 또는 rows)에 맞게 매핑해줍니다.
            totalRows = GridManager.Instance.Height;
        }

        if (CurrentCell.gridPosition.y >= (totalRows / 2))
        {
            // [앞줄]: 최대 체력 +15% 수정자 장착
            maxHpStat.AddModifier(new StatModifier(0.15f, ModifierType.Percent, ModifierSource.PositionBonus));
            Debug.Log($"{UnitName} <color=yellow>🛡 [동적 앞줄 판정]</color> (Y:{CurrentCell.gridPosition.y} / 총 {totalRows}행): 체력 +15% 버프.");
        }
        else
        {
            // [뒷줄]: 공격 속도 +15% 수정자 장착
            attackSpeedStat.AddModifier(new StatModifier(0.15f, ModifierType.Percent, ModifierSource.PositionBonus));
            Debug.Log($"{UnitName} <color=cyan>⚡ [동적 뒷줄 판정]</color> (Y:{CurrentCell.gridPosition.y} / 총 {totalRows}행): 공속 +15% 버프.");
        }
        if (UnitPlacement.Instance != null && !UnitPlacement.IsBattleActive)
        {
            currentHP = MaxHP;
        }
        UpdateHPBarUI();
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
            hpSlider.maxValue = MaxHP;
            hpSlider.value = currentHP;
        }
    }

}