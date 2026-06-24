using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button startButton;

    private void Start()
    {
        // 버튼 클릭 시 게임 씬 비동기 로드 시작
        startButton.onClick.AddListener(() => {
            LoadingManager.Instance.LoadSceneAsync("GameScene");
        });
    }
}