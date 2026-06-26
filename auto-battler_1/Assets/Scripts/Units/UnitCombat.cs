using System.Collections;
using UnityEngine;

/// <summary>
/// 유닛의 실시간 타겟팅, 사거리 판정, 공격 루프 및 데미지 프로세스를 전담하는 컴포넌트
/// </summary>
[RequireComponent(typeof(UnitInstance))]
public class UnitCombat : MonoBehaviour
{
    [Header("Combat Configuration")]
    [SerializeField] private float hitTimingDelay = 0.25f; // 애니메이션 타격 타이밍 오프셋

    private UnitInstance selfInstance;
    private UnitFSM fsm;
    private UnitInstance currentTarget;
    private float attackCooldownTimer = 0f;
    private bool isAttacking = false;

    public UnitInstance CurrentTarget => currentTarget;

    private void Awake()
    {
        selfInstance = GetComponent<UnitInstance>();
        fsm = GetComponent<UnitFSM>();
    }

    private void Update()
    {
        if (!UnitPlacement.IsBattleActive || selfInstance.IsDead) return;

        // 공격 쿨다운 타이머 연산 (초당 공격 횟수 역수 계산)
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 전장 내 살아있는 적 유닛 중 가장 가까운 대상을 탐색하여 타겟으로 지정합니다.
    /// </summary>
    public bool SearchClosestEnemy()
    {
        // 현재 타겟이 유효하고 살려져 있다면 굳이 재탐색하지 않음
        if (currentTarget != null && !currentTarget.IsDead) return true;

        UnitInstance[] allUnits = FindObjectsOfType<UnitInstance>();
        UnitInstance bestTarget = null;
        float minDistance = float.MaxValue;

        foreach (var unit in allUnits)
        {
            if (unit == null || unit.IsDead || unit == selfInstance) continue;
            if (unit.IsPlayerSide == selfInstance.IsPlayerSide) continue; // 아군 피아식별

            float dist = Vector2.Distance(transform.position, unit.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestTarget = unit;
            }
        }

        currentTarget = bestTarget;
        return currentTarget != null;
    }

    /// <summary>
    /// 타겟이 내 공격 사거리 영역 안에 도달했는지 실시간 판정합니다.
    /// </summary>
    public bool IsTargetInAttackRange()
    {
        if (currentTarget == null || currentTarget.IsDead) return false;

        float distance = Vector2.Distance(transform.position, currentTarget.transform.position);

        // 격자 칸당 크기 보정 계수(1.2f) 반영
        float maxAttackDistance = selfInstance.AttackRange * 1.2f;
        return distance <= maxAttackDistance;
    }

    /// <summary>
    /// UnitFSM의 Attack 상태에서 매 프레임 호출하는 핵심 공격 루프 제어기
    /// </summary>
    public void ExecuteAttackLoop()
    {
        if (currentTarget == null || currentTarget.IsDead)
        {
            isAttacking = false;
            fsm.TransitionTo(UnitFSM.FsmState.Idle);
            return;
        }

        // 사거리에서 벗어났다면 추적 상태로 되돌림
        if (!IsTargetInAttackRange())
        {
            if (!isAttacking)
            {
                fsm.TransitionTo(UnitFSM.FsmState.MoveToTarget);
            }
            return;
        }

        // 쿨다운이 완료되었고 공격 중이 아니라면 타격 시퀀스 발동
        if (attackCooldownTimer <= 0f && !isAttacking)
        {
            StartCoroutine(CoAttackSequence());
        }
    }

    /// <summary>
    /// 사거리 규격별(근거리/원거리) 공격 분기 및 타이밍 기반 히트 코루틴
    /// </summary>
    private IEnumerator CoAttackSequence()
    {
        isAttacking = true;

        // 공속(AttackSpeed) 역수로 다음 평타 쿨다운 장전
        attackCooldownTimer = 1f / Mathf.Max(0.1f, selfInstance.AttackSpeed);

        if (selfInstance.AttackRange <= 1)
        {
            // [근거리 공격 분기] 격자 1칸짜리 근접 물리 타격
            Debug.Log($"<color=#FF6347>[근접 공격] {selfInstance.UnitName} ➡️ {currentTarget.UnitName} 시동!</color>");

            // 임시 애니메이터 트리거 연동 준비
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null) anim.SetTrigger("Attack");

            // 애니메이션 싱크에 맞추어 타이밍 딜레이 대기 (히트 판정 유예)
            yield return new WaitForSeconds(hitTimingDelay);

            // 대기 후 타겟이 여전히 살아있다면 실시간 데미지 가산
            if (currentTarget != null && !currentTarget.IsDead)
            {
                currentTarget.TakeDamage(selfInstance.AttackDamage);
                Debug.Log($"<color=#FF4500>💥 [히트 판정] {selfInstance.UnitName}가 {currentTarget.UnitName}에게 {selfInstance.AttackDamage}의 피해를 입혔습니다.</color>");
            }
        }
        else
        {
            // [원거리 공격 분기] 투사체 가상 비행 시뮬레이션 유도
            Debug.Log($"<color=#1E90FF>[원거리 발사] {selfInstance.UnitName} ➡️ {currentTarget.UnitName} 투사체 가동!</color>");

            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null) anim.SetTrigger("Attack");

            yield return new WaitForSeconds(hitTimingDelay);

            // 투사체 날아가는 임시 시간 추가 대기 (원거리 느낌 재현)
            yield return new WaitForSeconds(0.2f);

            if (currentTarget != null && !currentTarget.IsDead)
            {
                currentTarget.TakeDamage(selfInstance.AttackDamage);
                Debug.Log($"<color=#00BFFF>🏹 [투사체 적중] {selfInstance.UnitName}의 발사체가 {currentTarget.UnitName}에게 명중!</color>");
            }
        }

        isAttacking = false;
    }

    /// <summary>
    /// 전투 리셋(ESC 입력 등) 시 타겟 상태를 클린업합니다.
    /// </summary>
    public void ResetCombatTarget()
    {
        StopAllCoroutines();
        currentTarget = null;
        isAttacking = false;
        attackCooldownTimer = 0f;
    }
}