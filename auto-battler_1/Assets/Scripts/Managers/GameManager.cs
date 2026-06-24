using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Live Stats")]
    [SerializeField] private int currentGold;
    [SerializeField] private int playerHP;
    [SerializeField] private int currentNodeIndex;

    [Header("Player Infrastructure Collections")]
    [SerializeField] private List<RelicData> ownedRelics = new List<RelicData>();
    [SerializeField] private List<UnitData> ownedUnits = new List<UnitData>();

    // 외부 읽기 전용 프로퍼티 캡슐화
    public int CurrentGold => currentGold;
    public int PlayerHP => playerHP;
    public int CurrentNodeIndex => currentNodeIndex;
    public IReadOnlyList<RelicData> OwnedRelics => ownedRelics;
    public IReadOnlyList<UnitData> OwnedUnits => ownedUnits;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
    }

    public void LoseHP(int amount)
    {
        playerHP -= amount;
        if (playerHP <= 0)
        {
            playerHP = 0;
            // TODO: 게임 오버 및 정산 시퀀스 트리거 가동부
        }
    }

    public void AddRelic(RelicData relic)
    {
        if (relic != null && !ownedRelics.Contains(relic))
        {
            ownedRelics.Add(relic);
            // TODO: 유물 획득에 따른 고유 시너지 효과 역계산 버스 가동
        }
    }

    public void AddUnit(UnitData unit)
    {
        if (unit != null)
        {
            ownedUnits.Add(unit);
        }
    }
}