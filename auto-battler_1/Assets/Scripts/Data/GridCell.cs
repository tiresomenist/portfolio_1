using UnityEngine;

[System.Serializable]
public class GridCell
{
    public Vector2Int gridPosition;
    public bool isOccupied;
    public UnitInstance occupyingUnit; // 2D 스크립트 기반 유닛 인스턴스
    public bool isPlayerSide;
    public Vector2 worldPosition; // 2D 월드 포지션으로 수정 (X, Y)

    public GridCell(Vector2Int pos, bool playerSide, Vector2 worldPos)
    {
        gridPosition = pos;
        isPlayerSide = playerSide;
        worldPosition = worldPos;
        isOccupied = false;
        occupyingUnit = null;
    }

    public void PlaceUnit(UnitInstance unit)
    {
        occupyingUnit = unit;
        isOccupied = (unit != null);
    }

    public void ClearCell()
    {
        occupyingUnit = null;
        isOccupied = false;
    }
}