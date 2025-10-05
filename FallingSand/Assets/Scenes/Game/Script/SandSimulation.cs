using UnityEngine;

public class SandSimulation : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 90;
    public int gridHeight = 110;
    public float cellSize = 5f;

    [Header("Simulation Speed")]
    [Tooltip("Số bước mô phỏng trong mỗi frame. Tăng giá trị này để cát rơi nhanh hơn.")]
    public int simulationStepsPerFrame = 5;

    [Header("Visual Settings")]
    public Material sandMaterial;

    private float[,] _grid;
    private Color[,] _colorGrid;

    public float[,] grid => _grid;
    public Color[,] colorGrid => _colorGrid;

    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private int[] triangles;
    private SandScoring sandScoring;

    private int lossLineY;
    private bool isGameOver = false;
    
    // Đưa biến ra ngoài Update
    private bool hadMovement = false;
    private int simulationStep = 0;
    
    // Dirty flag để tối ưu rendering
    private bool needsRender = false;

    void Start()
    {
        InitializeGrids();
        SetupMeshRendering();
        sandScoring = gameObject.AddComponent<SandScoring>();
        LoadSandState(); // LoadSandState sẽ tự động set needsRender nếu có cát

        lossLineY = gridHeight / 5;
    }

    void OnApplicationQuit()
    {
        SaveSandState();
    }

    public void SaveSandState()
    {
        System.Text.StringBuilder gridData = new System.Text.StringBuilder();
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                gridData.Append(_grid[i, j]);
                gridData.Append(",");
            }
        }
        PlayerPrefs.SetString("SG", gridData.ToString());

        System.Text.StringBuilder colorData = new System.Text.StringBuilder();
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                Color c = _colorGrid[i, j];
                colorData.AppendFormat("{0}|{1}|{2}|{3},", c.r, c.g, c.b, c.a);
            }
        }
        PlayerPrefs.SetString("SCG", colorData.ToString());
        PlayerPrefs.Save();
    }

    public void LoadSandState()
    {
        bool hasLoadedData = false;
        
        string gridData = PlayerPrefs.GetString("SG", "");
        if (!string.IsNullOrEmpty(gridData))
        {
            var gridArr = gridData.Split(',');
            int idx = 0;
            for (int i = 0; i < gridWidth; i++)
            {
                for (int j = 0; j < gridHeight; j++)
                {
                    if (idx < gridArr.Length && float.TryParse(gridArr[idx], out float val))
                    {
                        _grid[i, j] = val;
                        if (val > 0) hasLoadedData = true; // Phát hiện có cát
                    }
                    else
                        _grid[i, j] = 0f;
                    idx++;
                }
            }
        }

        string colorData = PlayerPrefs.GetString("SCG", "");
        if (!string.IsNullOrEmpty(colorData))
        {
            var colorArr = colorData.Split(',');
            int idx = 0;
            for (int i = 0; i < gridWidth; i++)
            {
                for (int j = 0; j < gridHeight; j++)
                {
                    if (idx < colorArr.Length)
                    {
                        var comps = colorArr[idx].Split('|');
                        if (comps.Length == 4 &&
                            float.TryParse(comps[0], out float r) &&
                            float.TryParse(comps[1], out float g) &&
                            float.TryParse(comps[2], out float b) &&
                            float.TryParse(comps[3], out float a))
                        {
                            _colorGrid[i, j] = new Color(r, g, b, a);
                        }
                        else
                        {
                            _colorGrid[i, j] = Color.clear;
                        }
                    }
                    else
                    {
                        _colorGrid[i, j] = Color.clear;
                    }
                    idx++;
                }
            }
        }
        
        // Nếu đã load được cát, đánh dấu cần render
        if (hasLoadedData)
        {
            needsRender = true;
        }
    }

    void InitializeGrids()
    {
        _grid = new float[gridWidth, gridHeight];
        _colorGrid = new Color[gridWidth, gridHeight];
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                _grid[i, j] = 0f;
                _colorGrid[i, j] = Color.clear;
            }
        }
    }

    void SetupMeshRendering()
    {
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        mesh = GetComponent<MeshFilter>().mesh;
        GetComponent<MeshRenderer>().material = sandMaterial;

        int maxQuads = gridWidth * gridHeight;
        vertices = new Vector3[maxQuads * 4];
        colors = new Color[maxQuads * 4];
        triangles = new int[maxQuads * 6];
    }

    void Update()
    {
        
        RunSimulation();

        
        CheckGameState();

       
        if (needsRender)
        {
            RenderGrid();
            needsRender = false;
        }
    }

  
    void RunSimulation()
    {
        hadMovement = false;
        simulationStep = 0;
        
    
        while (simulationStep < simulationStepsPerFrame)
        {
            if (SimulateSand())
            {
                hadMovement = true;
                needsRender = true; // Đánh dấu cần render khi có movement
            }
            simulationStep++;
        }
    }

    // Tách logic kiểm tra game state
    void CheckGameState()
    {
        if (!isGameOver && !hadMovement)
        {
            if (CheckLossCondition())
            {
                isGameOver = true;
                UIManager.Instance.OpenUI<UILoss>();
                SoundManager.Instance.PlayVFXSound(3);
            }
            else
            {
                int score = sandScoring.CheckAndClearPaths();
                if (score > 0)
                {
                    Debug.Log($"Player scored: {score} points!");
                    needsRender = true; // Cần render lại sau khi clear paths
                }
            }
        }
    }

    private bool CheckLossCondition()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            if (_grid[x, lossLineY] > 0)
                return true;
        }
        return false;
    }

    public void AddSandBlock(BlockShape shape, Color color, Vector3 worldPosition)
    {
        Vector2 shapeCenter = GetShapeCenter(shape);
        int centerX = Mathf.RoundToInt(worldPosition.x / cellSize);
        int centerY = Mathf.RoundToInt(-worldPosition.y / cellSize);

        int startX = centerX - Mathf.RoundToInt(shapeCenter.x);
        int startY = centerY - Mathf.RoundToInt(shapeCenter.y);

        int finalY = FindValidDropPosition(shape, startX, startY);

        int minYOffset = int.MaxValue;
        foreach (var cell in shape.cells)
        {
            int y = finalY + cell.y;
            if (y < minYOffset)
                minYOffset = y;
        }
        int yOffset = 0;
        if (minYOffset < 0)
        {
            yOffset = -minYOffset;
        }

        foreach (var cellOffset in shape.cells)
        {
            int x = startX + cellOffset.x;
            int y = finalY + cellOffset.y + yOffset;

            if (WithinCols(x) && WithinRows(y))
            {
                _grid[x, y] = 1.0f;
                _colorGrid[x, y] = color;
            }
        }
        
        needsRender = true; // Đánh dấu cần render khi thêm block mới
    }

    private int FindValidDropPosition(BlockShape shape, int startX, int startY)
    {
        int testY = startY;

        while (HasCollision(shape, startX, testY))
        {
            testY--;

            int highestCellY = testY;
            foreach (var cell in shape.cells)
            {
                int cellY = testY + cell.y;
                if (cellY < highestCellY)
                    highestCellY = cellY;
            }

            if (highestCellY < 0)
            {
                int minYOffset = 0;
                foreach (var cell in shape.cells)
                {
                    if (cell.y < minYOffset)
                        minYOffset = cell.y;
                }
                return -minYOffset;
            }
        }

        return testY;
    }

    private bool HasCollision(BlockShape shape, int startX, int startY)
    {
        foreach (var cellOffset in shape.cells)
        {
            int x = startX + cellOffset.x;
            int y = startY + cellOffset.y;

            if (WithinCols(x) && WithinRows(y))
            {
                if (_grid[x, y] > 0)
                {
                    return true;
                }
            }
            else if (!WithinCols(x) && WithinRows(y))
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 GetShapeCenter(BlockShape shape)
    {
        if (shape.cells.Length == 0) return Vector2.zero;
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var cell in shape.cells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
            if (cell.y < minY) minY = cell.y;
            if (cell.y > maxY) maxY = cell.y;
        }
        return new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
    }

    bool SimulateSand()
    {
        bool anyMovement = false;
        
        for (int j = gridHeight - 2; j >= 0; j--)
        {
            if (j % 2 == 0)
            {
                for (int i = 0; i < gridWidth; i++)
                {
                    if (UpdateSandParticle(i, j))
                    {
                        anyMovement = true;
                    }
                }
            }
            else
            {
                for (int i = gridWidth - 1; i >= 0; i--)
                {
                    if (UpdateSandParticle(i, j))
                    {
                        anyMovement = true;
                    }
                }
            }
        }
        return anyMovement;
    }

    bool UpdateSandParticle(int i, int j)
    {
        if (_grid[i, j] <= 0) return false;

        if (WithinRows(j + 1) && _grid[i, j + 1] == 0)
        {
            MoveSand(i, j, i, j + 1);
            return true;
        }
        else
        {
            bool canGoLeft = WithinCols(i - 1) && WithinRows(j + 1) && _grid[i - 1, j + 1] == 0;
            bool canGoRight = WithinCols(i + 1) && WithinRows(j + 1) && _grid[i + 1, j + 1] == 0;

            if (canGoLeft && canGoRight)
            {
                if (Random.value < 0.5f)
                {
                    MoveSand(i, j, i - 1, j + 1);
                }
                else
                {
                    MoveSand(i, j, i + 1, j + 1);
                }
                return true;
            }
            else if (canGoLeft)
            {
                MoveSand(i, j, i - 1, j + 1);
                return true;
            }
            else if (canGoRight)
            {
                MoveSand(i, j, i + 1, j + 1);
                return true;
            }
        }
        return false;
    }

    void MoveSand(int fromX, int fromY, int toX, int toY)
    {
        _grid[toX, toY] = _grid[fromX, fromY];
        _colorGrid[toX, toY] = _colorGrid[fromX, fromY];
        _grid[fromX, fromY] = 0;
        _colorGrid[fromX, fromY] = Color.clear;
    }

    void RenderGrid()
    {
        int quadCount = 0;
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                if (_grid[i, j] > 0)
                {
                    int vertexIndex = quadCount * 4;
                    vertices[vertexIndex] = new Vector3(i * cellSize, -j * cellSize, 0);
                    vertices[vertexIndex + 1] = new Vector3(i * cellSize, -(j + 1) * cellSize, 0);
                    vertices[vertexIndex + 2] = new Vector3((i + 1) * cellSize, -(j + 1) * cellSize, 0);
                    vertices[vertexIndex + 3] = new Vector3((i + 1) * cellSize, -j * cellSize, 0);

                    Color color = _colorGrid[i, j];
                    colors[vertexIndex] = color;
                    colors[vertexIndex + 1] = color;
                    colors[vertexIndex + 2] = color;
                    colors[vertexIndex + 3] = color;

                    int triangleIndex = quadCount * 6;
                    triangles[triangleIndex] = vertexIndex;
                    triangles[triangleIndex + 1] = vertexIndex + 2;
                    triangles[triangleIndex + 2] = vertexIndex + 1;
                    triangles[triangleIndex + 3] = vertexIndex;
                    triangles[triangleIndex + 4] = vertexIndex + 3;
                    triangles[triangleIndex + 5] = vertexIndex + 2;

                    quadCount++;
                }
            }
        }

        // Chỉ update mesh khi có thay đổi, không clear toàn bộ
        if (quadCount > 0)
        {
            mesh.Clear(false); // false = giữ layout, chỉ clear data
            
            var finalVertices = new Vector3[quadCount * 4];
            var finalColors = new Color[quadCount * 4];
            var finalTriangles = new int[quadCount * 6];
            System.Array.Copy(vertices, finalVertices, quadCount * 4);
            System.Array.Copy(colors, finalColors, quadCount * 4);
            System.Array.Copy(triangles, finalTriangles, quadCount * 6);
            
            mesh.vertices = finalVertices;
            mesh.colors = finalColors;
            mesh.triangles = finalTriangles;
            mesh.RecalculateBounds();
        }
        else
        {
            mesh.Clear(false); // Xóa mesh nếu không có cát
        }
    }

    public void ResetSandGrid()
    {
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                _grid[i, j] = 0f;
                _colorGrid[i, j] = Color.clear;
            }
        }
        isGameOver = false;
        needsRender = true; // Cần render lại sau khi reset
    }

    bool WithinCols(int i) => i >= 0 && i < gridWidth;
    bool WithinRows(int j) => j >= 0 && j < gridHeight;
}

[System.Serializable]
public class BlockShape
{
    public Vector2Int[] cells;
    public BlockShape(Vector2Int[] cells)
    {
        this.cells = cells;
    }
}