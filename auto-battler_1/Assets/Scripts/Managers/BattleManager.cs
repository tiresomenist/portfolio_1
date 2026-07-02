using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 전투의 시작, 실시간 종결 감지, 배속 통제 및 승패 보상 정산을 총괄하는 매니저 컴포넌트
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("UI Canvas Reference")]
    [SerializeField] private GameObject battleResultPanel; // 꺼져야 하는 자식들을 모두 담은 부모 패널
    [SerializeField] private TMP_Text resultTitleText;      // "VICTORY" 또는 "DEFEAT"
    [SerializeField] private TMP_Text rewardText;           // 보상 및 대미지 내역 텍스트

    private bool isBattleRunning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // ★ [1번 버그 해결] 게임이 처음 켜질 때 결과창 UI 바구니를 확실하게 끄고 시작합니다.
        if (battleResultPanel != null)
        {
            battleResultPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // 중앙 집중 키 통제 시스템
        if (!isBattleRunning && Input.GetKeyDown(KeyCode.Space))
        {
            StartBattle();
        }
        else if (isBattleRunning && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseResultAndReset();
        }

        if (isBattleRunning)
        {
            CheckBattleEnd();
        }
    }

    public void StartBattle()
    {
        if (isBattleRunning) return;

        if (UnitPlacement.Instance != null && !UnitPlacement.IsBattleActive)
        {
            UnitPlacement.Instance.StartSimulationDirect();
        }

        isBattleRunning = true;

        // 전투 시작할 때도 결과 패널은 확실히 숨깁니다.
        if (battleResultPanel != null) battleResultPanel.SetActive(false);

        Time.timeScale = 1f;
        Debug.Log("<color=green>⚔️ [BattleManager] 자동 전투 돌입.</color>");
    }

    private void CheckBattleEnd()
    {
        UnitInstance[] allUnits = FindObjectsByType<UnitInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        int playerCount = 0;
        int enemyCount = 0;

        foreach (var unit in allUnits)
        {
            if (unit == null || unit.IsDead) continue;

            if (unit.IsPlayerSide) playerCount++;
            else enemyCount++;
        }

        if (playerCount == 0 && enemyCount == 0) OnBattleLose(0);
        else if (enemyCount == 0) OnBattleWin();
        else if (playerCount == 0) OnBattleLose(enemyCount);
    }

    private void OnBattleWin()
    {
        isBattleRunning = false;
        Time.timeScale = 0f;

        int rewardGold = 15;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(rewardGold);
        }

        // ★ [2번 버그 해결] 깨지던 이모지 아이콘(🏆)을 안전한 텍스트 연출로 변경 완료!
        if (battleResultPanel != null)
        {
            battleResultPanel.SetActive(true);
            if (resultTitleText != null) resultTitleText.text = "<color=#00FFFF>[ VICTORY ]</color>";
            if (rewardText != null) rewardText.text = $"관객들의 엄청난 환호! \n<color=yellow>보상 골드 +{rewardGold}G 획득</color>";
        }
    }

    private void OnBattleLose(int remainingEnemies)
    {
        isBattleRunning = false;
        Time.timeScale = 0f;

        int baseNodeDamage = 5;
        int finalDamage = baseNodeDamage + (remainingEnemies * 2);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoseHP(finalDamage);
        }

        // ★ [2번 버그 해결] 깨지던 이모지 아이콘(❌)을 안전한 텍스트 연출로 변경 완료!
        if (battleResultPanel != null)
        {
            battleResultPanel.SetActive(true);
            if (resultTitleText != null) resultTitleText.text = "<color=#FF6B6B>[ DEFEAT ]</color>";
            if (rewardText != null) rewardText.text = $"공연 피드백 싸늘함... \n<color=red>관객 호응도 -{finalDamage} 감소</color> \n(남은 적: {remainingEnemies}명)";
        }
    }

    public void SetBattleSpeed(float speedMultiplier)
    {
        if (!isBattleRunning) return;
        Time.timeScale = Mathf.Clamp(speedMultiplier, 1f, 3f);

        Debug.Log($"<color=#FFFF00>게임 속도 변환 : {speedMultiplier}배속 적용</color>");
    }

    public void CloseResultAndReset()
    {
        Time.timeScale = 1f;

        if (UnitPlacement.Instance != null)
        {
            UnitPlacement.Instance.StopSimulationAndResetDirect();
        }

        // ★ [3번 버그 해결] 결과 UI 바구니 패널을 꺼줌으로써 자식들(텍스트, 닫기버튼)이 한 번에 청소됩니다!
        if (battleResultPanel != null)
        {
            battleResultPanel.SetActive(false);
        }

        isBattleRunning = false;
        Debug.Log("<color=yellow>🔄 [전장 정비] 배치 페이즈 회귀 완료.</color>");
    }
}