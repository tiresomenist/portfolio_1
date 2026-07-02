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

    private ObjectPool<Projectile> projectilePool; // 실시간 제네릭 오브젝트 풀

    public UnitInstance CurrentTarget => currentTarget;

    private void Awake()
    {
        selfInstance = GetComponent<UnitInstance>();
        fsm = GetComponent<UnitFSM>();
    }

    private void Start()
    {
        // ★ [Lazy 초기화로 변경] Awake 시점엔 UnitData 로드가 안 끝났을 수 있으므로 Start에서 안전하게 생성합니다.
        InitProjectilePool();
    }

    /// <summary>
    /// 유닛 설계도 데이터를 기반으로 원거리 투사체 풀을 안전하게 워밍업합니다.
    /// </summary>
    private void InitProjectilePool()
    {
        if (selfInstance == null) selfInstance = GetComponent<UnitInstance>();

        // 코드로 소환된 유닛도 UnitInstance가 징검다리로 개방한 ProjectilePrefab을 실시간으로 캐치합니다.
        if (selfInstance.AttackRange > 1 && selfInstance.ProjectilePrefab != null)
        {
            projectilePool = new ObjectPool<Projectile>(
                selfInstance.ProjectilePrefab,
                3,
                $"{selfInstance.gameObject.name}_ProjPool"
            );
            Debug.Log($"<color=#32CD32>⚙️ [풀 장전 완수] {selfInstance.UnitName} 전용 원거리 투사체 풀이 생성되었습니다.</color>");
        }
    }

    private void Update()
    {
        if (!UnitPlacement.IsBattleActive || selfInstance.IsDead) return;

        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }
    }

    public bool SearchClosestEnemy()
    {
        if (currentTarget != null && !currentTarget.IsDead) return true;

        UnitInstance[] allUnits = FindObjectsOfType<UnitInstance>();
        UnitInstance bestTarget = null;
        float minDistance = float.MaxValue;

        foreach (var unit in allUnits)
        {
            if (unit == null || unit.IsDead || unit == selfInstance) continue;
            if (unit.IsPlayerSide == selfInstance.IsPlayerSide) continue;

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

    public bool IsTargetInAttackRange()
    {
        if (currentTarget == null || currentTarget.IsDead) return false;

        float distance = Vector2.Distance(transform.position, currentTarget.transform.position);
        float maxAttackDistance = selfInstance.AttackRange * 1.2f;
        return distance <= maxAttackDistance;
    }

    public void ExecuteAttackLoop()
    {
        if (currentTarget == null || currentTarget.IsDead)
        {
            isAttacking = false;
            fsm.TransitionTo(UnitFSM.FsmState.Idle);
            return;
        }

        if (!IsTargetInAttackRange())
        {
            if (!isAttacking)
            {
                fsm.TransitionTo(UnitFSM.FsmState.MoveToTarget);
            }
            return;
        }

        if (attackCooldownTimer <= 0f && !isAttacking)
        {
            StartCoroutine(CoAttackSequence());
        }
    }

    private IEnumerator CoAttackSequence()
    {
        isAttacking = true;
        attackCooldownTimer = 1f / Mathf.Max(0.1f, selfInstance.AttackSpeed);

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(hitTimingDelay);

        if (currentTarget != null && !currentTarget.IsDead)
        {
            // 1. 근/원거리 분기
            if (projectilePool != null)
            {
                // [원거리 진짜 투사체 풀링 발사]
                Projectile proj = projectilePool.Get(transform.position, Quaternion.identity);
                proj.Shoot(currentTarget, selfInstance.AttackDamage, (releasedProj) =>
                {
                    projectilePool.Release(releasedProj);
                });

                Debug.Log($"<color=#00BFFF>🏹 [투사체 풀링 발사] {selfInstance.UnitName}가 풀에서 발사체를 꺼내 {currentTarget.UnitName}에게 날렸습니다.</color>");
            }
            else
            {
                // 근거리 평타/투사체 미등록 시 예외 구동 물리 타격
                currentTarget.TakeDamage(selfInstance.AttackDamage);
                Debug.Log($"<color=#FF4500>💥 [히트 판정] {selfInstance.UnitName}가 {currentTarget.UnitName}에게 {selfInstance.AttackDamage}의 피해를 입혔습니다.</color>");
            }
            // 2. 공격에 성공할 때마다 마나 적립
            bool isManaFull = selfInstance.GainMana(10);

            // 3. 마나가 가득 차면 즉시 스킬 가동
            if (isManaFull)
            {
                ExecuteCastSkill();
            }
        }

        isAttacking = false;
    }

    public void ResetCombatTarget()
    {
        StopAllCoroutines();
        currentTarget = null;
        isAttacking = false;
        attackCooldownTimer = 0f;
    }

    private void ExecuteCastSkill()
    {
        Debug.Log($"<color=orange>✨🔥 [스킬 발동] {selfInstance.UnitName}의 마나가 가득 차 고유 스킬을 시전합니다! 🔥✨</color>");

        // 1. 파티클 이펙트 바인딩 연동 (있다면 타겟 혹은 내 위치에 생성 후 자동 파괴)
        if (selfInstance.SkillEffectPrefab != null)
        {
            Vector3 spawnPos = (selfInstance.UnitSkillType == SkillType.TeamBuff) ? transform.position : (currentTarget != null ? currentTarget.transform.position : transform.position);
            ParticleSystem effect = Instantiate(selfInstance.SkillEffectPrefab, spawnPos, Quaternion.identity);
            Destroy(effect.gameObject, 3.0f); // 3초 뒤 클린업 메모리 방어선
        }

        // 2. 기획 데이터 기반 5대 스킬 유형 기하학 분기 연산 연동
        UnitInstance[] allUnits = FindObjectsByType<UnitInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        switch (selfInstance.UnitSkillType)
        {
            case SkillType.SingleDamage:
                // [단일 강타]: 현재 타겟에게 공격력과 무관하게 고유 스킬 파워만큼 강력한 대미지 주입
                if (currentTarget != null)
                {
                    currentTarget.TakeDamage((int)selfInstance.SkillValue);
                    Debug.Log($"<color=#FF00FF>⚡ [스킬 - 단일강타] {currentTarget.UnitName}에게 대미지 {(int)selfInstance.SkillValue} 가함.</color>");
                }
                break;

            case SkillType.AoEDamage:
                // [범위 AOE 피해]: 타겟 중심으로 지정된 가상 반경 내의 모든 적 전멸 유도
                if (currentTarget != null)
                {
                    Vector3 center = currentTarget.transform.position;
                    foreach (var unit in allUnits)
                    {
                        if (unit != null && !unit.IsDead && unit.IsPlayerSide != selfInstance.IsPlayerSide)
                        {
                            if (Vector2.Distance(center, unit.transform.position) <= selfInstance.SkillRadius)
                            {
                                unit.TakeDamage((int)selfInstance.SkillValue);
                            }
                        }
                    }
                }
                break;

            case SkillType.TeamBuff:
                // [아군 버프]: 내 주변 반경 내에 있는 든든한 아군들을 전수 조사하여 체력 즉시 치유/회복
                Vector3 myPos = transform.position;
                foreach (var unit in allUnits)
                {
                    if (unit != null && !unit.IsDead && unit.IsPlayerSide == selfInstance.IsPlayerSide)
                    {
                        if (Vector2.Distance(myPos, unit.transform.position) <= selfInstance.SkillRadius)
                        {
                            // 버프 연산 예시: 아군 유닛 피 회복 주입 (TakeDamage의 음수 연산 우회용 간이 힐)
                            unit.GainMana(10); // 팀 응원 버프로 마나를 채워주는 유니크 유틸기 구현!
                            Debug.Log($"<color=#00FF00>💚 [스킬 - 마나 충전] {unit.UnitName}의 마나가 10 충전되었습니다.</color>");
                        }
                    }
                }
                break;

            case SkillType.EnemyDebuff:
                // [적군 디버프]: 범위 내 적들의 전의를 상실케 하여 즉시 큰 데미지와 충격 주입
                if (currentTarget != null)
                {
                    currentTarget.TakeDamage((int)selfInstance.SkillValue);
                    // 간이 수치 감소 로그 연동
                    Debug.Log($"<color=gray>💀 [스킬 - 디버프] {currentTarget.UnitName}에게 디버프를 가했습니다.</color>");
                }
                break;

            case SkillType.CrowdControl:
                // [군중 제어 기절]: 현재 타겟 적 유닛에게 강력한 충격을 주어 FSM 올스톱 기절 메커니즘 연동
                if (currentTarget != null)
                {
                    if (currentTarget.TryGetComponent(out UnitFSM enemyFsm))
                    {
                        enemyFsm.ApplyStun(selfInstance.SkillValue); // skillValue 값만큼 초 단위 기절 적용
                        Debug.Log($"<color=gray>💀 [스킬 - 기절] {currentTarget.UnitName}에게 {selfInstance.SkillValue}만큼 기절을 가했습니다.</color>");
                    }
                }
                break;
        }

        // 3. 스킬 사용이 끝났으므로 마나통 완벽 청소
        selfInstance.UseMana();
    }
}