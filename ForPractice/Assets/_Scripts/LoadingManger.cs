using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance { get; private set; }

    [SerializeField] private GameObject loadingCanvasGroup;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Image raycastBlocker; // 전체 화면을 막는 투명 이미지

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // async/await 기반의 비동기 씬 로드 함수
    public async void LoadSceneAsync(string sceneName)
    {
        // UI 최적화 및 예외 방지: 로딩 중에는 배경 UI가 클릭되지 않도록 레이캐스트 블록 활성화
        raycastBlocker.raycastTarget = true;
        loadingCanvasGroup.SetActive(true);
        progressBar.value = 0f;

        // 유니티 씬 비동기 로드 시작
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        // 씬 로드가 완료될 때까지 메인 스레드를 블로킹하지 않고 대기
        while (!operation.isDone)
        {
            // operation.progress는 0에서 0.9까지 움직이므로 이를 보정하여 슬라이더에 반영
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            progressBar.value = progress;

            // 다음 프레임까지 대기 (Task.Yield를 쓰면 메인 스레드 프레임 루프에 양보)
            await Task.Yield();
        }

        // 로드 완료 후 연산 유예 및 연출용 대기 시간 제공
        await Task.Delay(500);

        // 로딩 완료 후 UI 정리 및 레이캐스트 비활성화
        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.SetActive(false);
        }

        if (raycastBlocker != null)
        {
            raycastBlocker.raycastTarget = false;
        }
    }
}
