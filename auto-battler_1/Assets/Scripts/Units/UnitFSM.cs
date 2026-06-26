using UnityEngine;

/// <summary>
/// 유닛의 상태(Idle, MoveToTarget, Attack, Dead) 제어를 담당하는 유한 상태 머신 컴포넌트
/// </summary>
public class UnitFSM : MonoBehaviour
{
    public enum FsmState { Idle, MoveToTarget, Attack, Dead }

    [Header("Current Active FSM State")]
    [SerializeField] private FsmState currentState = FsmState.Idle;

    private UnitInstance selfInstance;
    private UnitCombat combat; // 추가된 컴뱃 매니저 링크
    private Animator animator;

    public FsmState CurrentState => currentState;

    private void Awake()
    {
        selfInstance = GetComponent<UnitInstance>();
        combat = GetComponent<UnitCombat>();
        animator = GetComponentInChildren<Animator>();
    }

    public void SetInitialState()
    {
        TransitionTo(FsmState.Idle);
    }

    private void Update()
    {
        if (!UnitPlacement.IsBattleActive || currentState == FsmState.Dead) return;

        UpdateStateBehavior();
    }

    public void TransitionTo(FsmState newState)
    {
        if (currentState == newState && newState != FsmState.Idle) return;

        ExitCurrentState(currentState);
        currentState = newState;
        EnterNewState(currentState);
    }

    private void EnterNewState(FsmState state)
    {
        if (selfInstance == null) selfInstance = GetComponent<UnitInstance>();
        if (combat == null) combat = GetComponent<UnitCombat>();
        Debug.Log($"[FSM {selfInstance.UnitName}] 진입 상태 ➡️ {state}");

        switch (state)
        {
            case FsmState.Idle:
                if (animator != null) animator.Play("Idle");
                break;
            case FsmState.MoveToTarget:
                if (animator != null) animator.Play("Move");
                break;
            case FsmState.Attack:
                // 스위칭 즉시 공격 루프 개시 유도
                break;
            case FsmState.Dead:
                if (animator != null) animator.Play("Die");
                ExecuteDeathSequence();
                break;
        }
    }

    private void UpdateStateBehavior()
    {
        if (selfInstance == null) selfInstance = GetComponent<UnitInstance>();

        if (combat == null)
        {
            combat = GetComponent<UnitCombat>();
            if (combat == null)
            {
                combat = gameObject.AddComponent<UnitCombat>();
            }
        }
        switch (currentState)
        {
            case FsmState.Idle:
                // 1. 적군 타겟 탐색 위임
                if (combat.SearchClosestEnemy())
                {
                    // 2. 사거리 안에 있다면 공격으로, 멀다면 이동으로 분기
                    if (combat.IsTargetInAttackRange())
                        TransitionTo(FsmState.Attack);
                    else
                        TransitionTo(FsmState.MoveToTarget);
                }
                break;

            case FsmState.MoveToTarget:
                // 타겟이 죽었거나 유실되었다면 다시 Idle로 복귀 (재타겟팅 유도)
                if (combat.CurrentTarget == null || combat.CurrentTarget.IsDead)
                {
                    TransitionTo(FsmState.Idle);
                    break;
                }

                // 사거리 내부로 좁혀졌다면 공격 상태로 전이
                if (combat.IsTargetInAttackRange())
                {
                    TransitionTo(FsmState.Attack);
                }
                else
                {
                    // 타겟 방향 기동 추적 연산
                    Vector3 moveDir = (combat.CurrentTarget.transform.position - transform.position).normalized;
                    transform.position += moveDir * 3f * Time.deltaTime;
                }
                break;

            case FsmState.Attack:
                // 3. 오늘 자 핵심 목표: Attack 상태에서 UnitCombat 공격 로직 상시 가동
                combat.ExecuteAttackLoop();
                break;
        }
    }

    private void ExitCurrentState(FsmState state)
    {
        // 상태 탈출 시 클린업
    }

    private void ExecuteDeathSequence()
    {
        if (selfInstance == null) selfInstance = GetComponent<UnitInstance>();
        Debug.Log($"사망 처리 발동 - 격자 반환");

        if (selfInstance.CurrentCell != null)
        {
            selfInstance.CurrentCell.isOccupied = false;
        }
        gameObject.SetActive(false);
    }
}