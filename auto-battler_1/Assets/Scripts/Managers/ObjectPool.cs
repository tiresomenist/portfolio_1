using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MonoBehaviour를 상속받는 모든 컴포넌트에 대응하는 제네릭 오브젝트 풀 클래스
/// </summary>
public class ObjectPool<T> where T : MonoBehaviour
{
    private T prefab;
    private Transform poolRoot;
    private Queue<T> poolQueue = new Queue<T>();

    /// <summary>
    /// 오브젝트 풀 초기화 및 초기 수량만큼 선행 생성(Warming up)
    /// </summary>
    public ObjectPool(T prefab, int initialSize, string poolName = "ObjectPool")
    {
        this.prefab = prefab;

        // 하이어라키 창을 깔끔하게 정리하기 위한 루트 홀더 생성
        GameObject rootGo = new GameObject($"[Pool] {poolName}");
        poolRoot = rootGo.transform;

        for (int i = 0; i < initialSize; i++)
        {
            T obj = CreateNewInstance();
            obj.gameObject.SetActive(false);
            poolQueue.Enqueue(obj);
        }
    }

    /// <summary>
    /// 풀에서 대기 중인 오브젝트를 꺼냅니다. 부족하면 실시간으로 생성합니다.
    /// </summary>
    public T Get(Vector3 position, Quaternion rotation)
    {
        T obj;

        if (poolQueue.Count > 0)
        {
            obj = poolQueue.Dequeue();
        }
        else
        {
            // 풀이 말랐을 때 유연하게 확장(자동 방어선)
            obj = CreateNewInstance();
            Debug.LogWarning($"⚠️ [ObjectPool<{typeof(T).Name}>] 풀 잔여량이 부족하여 실시간으로 인스턴스를 확장 생성했습니다.");
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.gameObject.SetActive(true);

        return obj;
    }

    /// <summary>
    /// 사용이 끝난 오브젝트를 반환하고 비활성화합니다.
    /// </summary>
    public void Release(T obj)
    {
        if (obj == null) return;

        obj.gameObject.SetActive(false);
        obj.transform.SetParent(poolRoot); // 루트 밑으로 원위치
        poolQueue.Enqueue(obj);
    }

    private T CreateNewInstance()
    {
        T newObj = Object.Instantiate(prefab, poolRoot);
        return newObj;
    }
}