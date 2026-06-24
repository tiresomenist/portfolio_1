using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Geometry Settings")]
    [SerializeField] private int width = 6;
    [SerializeField] private int height = 3;
    [SerializeField] private float cellSize = 1.2f;
    [SerializeField] private float fieldGap = 1.5f; // 아군과 적군 진영 사이의 Y축 공백 거리

    private GridCell[,] playerGrid;
    private GridCell[,] enemyGrid;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        GenerateBattlefield();
    }

    private void GenerateBattlefield()
    {
        playerGrid = new GridCell[width, height];
        enemyGrid = new GridCell[width, height];

        Vector2 origin = transform.position;

        // 아군 격자 생성 (Y축 하단 배치)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 worldPos = origin + new Vector2(x * cellSize, y * cellSize);
                playerGrid[x, y] = new GridCell(new Vector2Int(x, y), true, worldPos);
            }
        }

        // 적군 격자 생성 (Y축 상단 배치, fieldGap 반영)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float enemyYOffset = (y + height) * cellSize + fieldGap;
                Vector2 worldPos = origin + new Vector2(x * cellSize, enemyYOffset);
                enemyGrid[x, y] = new GridCell(new Vector2Int(x, y), false, worldPos);
            }
        }
    }

    public GridCell GetCell(int x, int y, bool isPlayerSide)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return isPlayerSide ? playerGrid[x, y] : enemyGrid[x, y];
    }

    public GridCell GetEmptyCell(bool isPlayerSide)
    {
        GridCell[,] targetGrid = isPlayerSide ? playerGrid : enemyGrid;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!targetGrid[x, y].isOccupied)
                {
                    return targetGrid[x, y];
                }
            }
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            DrawPreviewGizmos();
            return;
        }

        DrawActiveGridGizmos(playerGrid, Color.green);
        DrawActiveGridGizmos(enemyGrid, Color.red);
    }

    private void DrawActiveGridGizmos(GridCell[,] grid, Color color)
    {
        if (grid == null) return;
        Gizmos.color = color;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 pos = grid[x, y].worldPosition;
                // 2D 전장이므로 WireCube의 두께를 Z축이 아닌 Z를 0으로 두고 X, Y 크기만 지정
                Gizmos.DrawWireCube(pos, new Vector3(cellSize * 0.95f, cellSize * 0.95f, 0.1f));
            }
        }
    }

    private void DrawPreviewGizmos()
    {
        Vector2 origin = transform.position;

        // 에디터 아군 미리보기 (초록색 선)
        Gizmos.color = Color.green;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 worldPos = origin + new Vector2(x * cellSize, y * cellSize);
                Gizmos.DrawWireCube(worldPos, new Vector3(cellSize * 0.95f, cellSize * 0.95f, 0.1f));
            }
        }

        // 에디터 적군 미리보기 (빨간색 선)
        Gizmos.color = Color.red;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float enemyYOffset = (y + height) * cellSize + fieldGap;
                Vector2 worldPos = origin + new Vector2(x * cellSize, enemyYOffset);
                Gizmos.DrawWireCube(worldPos, new Vector3(cellSize * 0.95f, cellSize * 0.95f, 0.1f));
            }
        }
    }
}