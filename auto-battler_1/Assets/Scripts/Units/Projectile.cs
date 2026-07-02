using System;
using UnityEngine;

/// <summary>
/// 원거리 발사체의 유도 무빙, 적 유닛 충돌 판정 및 오브젝트 풀 반환을 전담하는 컴포넌트
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Projectile Property")]
    [SerializeField] private float speed = 7f;
    [SerializeField] private float hitRadius = 0.2f; // 가상 충돌 반경

    private UnitInstance targetUnit;
    private int damage;
    private Action<Projectile> onReleaseCallback; // 나를 관리하는 풀에 복귀하기 위한 델리게이트 식별자

    /// <summary>
    /// 원거리 발사 시 발사 데이터 및 컴뱃 풀 링크 주입
    /// </summary>
    public void Shoot(UnitInstance target, int dmg, Action<Projectile> releaseCallback)
    {
        targetUnit = target;
        damage = dmg;
        onReleaseCallback = releaseCallback;
    }

    private void Update()
    {
        // 비행 도중 타겟이 이미 의문사(다른 아군에 의해)했다면 공중 유실 처리하여 풀로 반환
        if (targetUnit == null || targetUnit.IsDead || !targetUnit.gameObject.activeInHierarchy)
        {
            ReturnToPool();
            return;
        }

        // 타겟 유닛을 향해 호밍(유도) 이동
        Vector3 targetPos = targetUnit.transform.position;
        Vector3 moveDir = (targetPos - transform.position).normalized;
        transform.position += moveDir * speed * Time.deltaTime;

        // 충돌 체크 (물리 Trigger 오버헤드를 회피하기 위한 2D 거리 기반 고속 연산)
        float distance = Vector2.Distance(transform.position, targetPos);
        if (distance <= hitRadius)
        {
            OnHitTarget();
        }
    }

    private void OnHitTarget()
    {
        if (targetUnit != null && !targetUnit.IsDead)
        {
            targetUnit.TakeDamage(damage);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        // 델리게이트 콜백을 트리거하여 할당되어 있던 제네릭 풀로 안전하게 전송 및 반환
        onReleaseCallback?.Invoke(this);
    }
}