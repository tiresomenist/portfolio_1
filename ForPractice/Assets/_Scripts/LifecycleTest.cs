using UnityEngine;

public class LifecycleTest : MonoBehaviour
{
    [SerializeField] private string objectName = "Default";
    private int fixCount = 0;
    private int updateCount = 0;
    private int lateCount = 0;

    private void Awake()
    {
        Debug.Log($"[{objectName}] Awake 호출됨");
    }

    private void OnEnable()
    {
        Debug.Log($"[{objectName}] OnEnable 호출됨");
    }

    private void Start()
    {
        Debug.Log($"[{objectName}] Start 호출됨");
    }

    private void FixedUpdate()
    {
        // FixedUpdate와 Update는 로그가 너무 많이 찍히므로 
        // 흐름을 한번 본 후에는 주석 처리하는 것이 좋습니다.
        if (fixCount < 30)
        {
            Debug.Log($"[{objectName}] FixedUpdate 호출 (물리 주기)");
            fixCount++;
        }
    }

    private void Update()
    {
        // Space바를 누르면 오브젝트를 비활성화했다가 켜는 테스트용
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"\n--- [Space] 키 입력: {objectName} 컴포넌트를 토글합니다. ---");
            this.enabled = !this.enabled;
        }
    }

    private void LateUpdate()
    {
        if (lateCount < 10)
        {
            Debug.Log($"[{objectName}] LateUpdate 호출 (카메라 주기에 적합)");
            lateCount++;
        }
    }

    private void OnDisable()
    {
        Debug.Log($"[{objectName}] OnDisable 호출됨 (비활성화)");
    }

    private void OnDestroy()
    {
        Debug.Log($"[{objectName}] OnDestroy 호출됨 (파괴/종료)");
    }
}