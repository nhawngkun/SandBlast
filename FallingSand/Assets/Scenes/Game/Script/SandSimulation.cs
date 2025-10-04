using UnityEngine;

public class SandSimulation : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 90;
    public int gridHeight = 110;
    public float cellSize = 5f;

    [Header("Simulation Speed")]
    [Tooltip("Sá»‘ bÆ°á»›c mÃ´ phá»ng trong má»—i frame. TÄƒng giÃ¡ trá»‹ nÃ y Ä‘á»ƒ cÃ¡t rÆ¡i nhanh hÆ¡n.")]
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

    private int lossLineY; // Vá»‹ trÃ­ váº¡ch Ä‘á» (theo chá»‰ sá»‘ dÃ²ng)
    private bool isGameOver = false; // Tráº¡ng thÃ¡i thua

    void Start()
    {
        InitializeGrids();
        SetupMeshRendering();
        sandScoring = gameObject.AddComponent<SandScoring>();
        LoadSandState(); // KhÃ´i phá»¥c tráº¡ng thÃ¡i cÃ¡t khi vÃ o game

        // Váº¡ch Ä‘á» á»Ÿ 1/5 chiá»u cao há»™p cÃ¡t (tÃ­nh tá»« trÃªn xuá»‘ng)
        lossLineY = gridHeight / 5;
    }

    void OnApplicationQuit()
    {
        SaveSandState(); // LÆ°u tráº¡ng thÃ¡i cÃ¡t khi thoÃ¡t game
    }

    public void SaveSandState()
    {
        // LÆ°u grid
        System.Text.StringBuilder gridData = new System.Text.StringBuilder();
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                gridData.Append(_grid[i, j]);
                gridData.Append(",");
            }
        }
        PlayerPrefs.SetString("SandGrid", gridData.ToString());

        // LÆ°u colorGrid (r,g,b,a)
        System.Text.StringBuilder colorData = new System.Text.StringBuilder();
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                Color c = _colorGrid[i, j];
                colorData.AppendFormat("{0}|{1}|{2}|{3},", c.r, c.g, c.b, c.a);
            }
        }
        PlayerPrefs.SetString("SandColorGrid", colorData.ToString());
        PlayerPrefs.Save();
    }

    public void LoadSandState()
    {
        // KhÃ´i phá»¥c grid
        string gridData = PlayerPrefs.GetString("SandGrid", "");
        if (!string.IsNullOrEmpty(gridData))
        {
            var gridArr = gridData.Split(',');
            int idx = 0;
            for (int i = 0; i < gridWidth; i++)
            {
                for (int j = 0; j < gridHeight; j++)
                {
                    if (idx < gridArr.Length && float.TryParse(gridArr[idx], out float val))
                        _grid[i, j] = val;
                    else
                        _grid[i, j] = 0f;
                    idx++;
                }
            }
        }

        // KhÃ´i phá»¥c colorGrid
        string colorData = PlayerPrefs.GetString("SandColorGrid", "");
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
        bool hadMovement = false;
        for (int i = 0; i < simulationStepsPerFrame; i++)
        {
            if (SimulateSand())
            {
                hadMovement = true;
            }
        }

       
        if (!isGameOver && !hadMovement && CheckLossCondition())
        {
            isGameOver = true;
            UIManager.Instance.OpenUI<UILoss>();
            SoundManager.Instance.PlayVFXSound(3);
            
        }

        if (!hadMovement && !isGameOver)
        {
            int score = sandScoring.CheckAndClearPaths();
            if (score > 0)
            {
                Debug.Log($"Player scored: {score} points!");
            }
        }

        RenderGrid();
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
            // Quét xen kẽ để tạo hiệu ứng tự nhiên hơn
            if (j % 2 == 0)
            {
                // Hàng chẵn: quét từ trái sang phải
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
                // Hàng lẻ: quét từ phải sang trái
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

    /// <summary>
    /// Tách logic cập nhật một hạt cát ra hầm riêng để tránh lặp code
    /// Trả về true nếu hạt cát này đã di chuyển
    /// </summary>
    bool UpdateSandParticle(int i, int j)
    {
        // Chỉ xử lý nếu ô hiện tại có cát
        if (_grid[i, j] <= 0) return false;

        // 1. Ưu tiên rơi thẳng xuống
        if (WithinRows(j + 1) && _grid[i, j + 1] == 0)
        {
            MoveSand(i, j, i, j + 1);
            return true;
        }
        // 2. Nếu không thể rơi thẳng, xét rơi chéo
        else
        {
            bool canGoLeft = WithinCols(i - 1) && WithinRows(j + 1) && _grid[i - 1, j + 1] == 0;
            bool canGoRight = WithinCols(i + 1) && WithinRows(j + 1) && _grid[i + 1, j + 1] == 0;

            if (canGoLeft && canGoRight)
            {
                // Nếu có thể đi cả 2 hướng, chọn ngẫu nhiên
                if (Random.value < 0.5f)
                {
                    MoveSand(i, j, i - 1, j + 1); // Rơi chéo trái
                }
                else
                {
                    MoveSand(i, j, i + 1, j + 1); // Rơi chéo phải
                }
                return true;
            }
            else if (canGoLeft)
            {
                MoveSand(i, j, i - 1, j + 1); // Chỉ có thể rơi chéo trái
                return true;
            }
            else if (canGoRight)
            {
                MoveSand(i, j, i + 1, j + 1); // Chỉ có thể rơi chéo phải
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

        mesh.Clear();
        if (quadCount > 0)
        {
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
        isGameOver = false; // Reset trạng thái thua khi chơi lại
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