using UnityEngine;

[CreateAssetMenu(fileName = "NewSynergyData", menuName = "AutoBattler/SynergyData")]
public class SynergyData : ScriptableObject { }

[CreateAssetMenu(fileName = "NewEventData", menuName = "AutoBattler/EventData")]
public class EventData : ScriptableObject { }

// 유닛 인스턴스 뼈대 (추후 Scripts/Units/UnitInstance.cs로 이관 예정)
public class UnitInstance : MonoBehaviour { }