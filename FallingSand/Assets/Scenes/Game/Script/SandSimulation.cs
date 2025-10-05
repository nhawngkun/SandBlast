using UnityEngine;
using System.Collections;

public class SandSimulation : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 90;
    public int gridHeight = 110;
    public float cellSize = 5f;

    [Header("Simulation Speed")]
    [Tooltip("Số bước mô phỏng trong mỗi lần chạy simulation.")]
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
    
    // Quản lý simulation
    private bool needsSimulation = false;
    private Coroutine activeSimulationCoroutine = null;

    void Start()
    {
        InitializeGrids();
        SetupMeshRendering();
        sandScoring = gameObject.AddComponent<SandScoring>();
        LoadSandState();

        lossLineY = gridHeight / 5;
        
        // Kiểm tra nếu có cát từ save data thì cần simulate
        if (HasAnySand())
        {
            TriggerSimulation();
        }
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
                        _grid[i, j] = val;
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
        // Chỉ render
        RenderGrid();
    }

    
    public void TriggerSimulation()
    {
        if (!isGameOver && activeSimulationCoroutine == null)
        {
            needsSimulation = true;
            activeSimulationCoroutine = StartCoroutine(RunSimulationUntilStable());
        }
    }

   
    private IEnumerator RunSimulationUntilStable()
    {
        int noMovementCount = 0;
        int stableThreshold = 2; // Cần 2 lần liên tiếp không movement

        while (needsSimulation && !isGameOver)
        {
            bool hadMovement = false;
            
            // Chạy nhiều bước simulation (cát rơi)
            for (int i = 0; i < simulationStepsPerFrame; i++)
            {
                if (SimulateSand())
                {
                    hadMovement = true;
                }
            }

            //  Kiểm tra cát có đang rơi không
            if (!hadMovement)
            {
                noMovementCount++;
                
                //  Cát đã ổn định (không rơi nữa)
                if (noMovementCount >= stableThreshold)
                {
                    //  Kiểm tra điều kiện thua
                    if (CheckLossCondition())
                    {
                        isGameOver = true;
                        UIManager.Instance.OpenUI<UILoss>();
                        SoundManager.Instance.PlayVFXSound(3);
                        needsSimulation = false;
                        break;
                    }

                    //  Kiểm tra và clear paths (xóa cát khi có điểm)
                    int score = sandScoring.CheckAndClearPaths();
                    if (score > 0)
                    {
                        Debug.Log($"Player scored: {score} points!");
                        
                        // QUAN TRỌNG: Sau khi xóa cát, cho cát RƠI NGAY xuống chỗ trống
                        // Gọi NHIỀU LẦN để cát rơi hết xuống
                        for (int i = 0; i < 10; i++)
                        {
                            if (!SimulateSand())
                            {
                                // Không còn cát rơi nữa thì dừng
                                break;
                            }
                        }
                        
                        // Reset counter để tiếp tục kiểm tra
                        noMovementCount = 0;
                        needsSimulation = true;
                    }
                    else
                    {
                        // Không có gì để clear, cát hoàn toàn ổn định
                        needsSimulation = false;
                        break;
                    }
                }
            }
            else
            {
                // Còn movement (cát đang rơi), reset counter
                noMovementCount = 0;
            }
            
            // Chờ 1 frame trước khi tiếp tục
            yield return null;
        }

        // Kết thúc simulation
        activeSimulationCoroutine = null;
    }

    /// <summary>
    /// Kiểm tra có cát nào trong grid không
    /// </summary>
    private bool HasAnySand()
    {
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                if (_grid[i, j] > 0)
                    return true;
            }
        }
        return false;
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
        
        // Kích hoạt simulation KHI có cát mới được thêm
        TriggerSimulation();
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

    /// <summary>
    /// Mô phỏng vật lý cát rơi xuống
    /// Return: true nếu có cát di chuyển, false nếu tất cả đã ổn định
    /// </summary>
    bool SimulateSand()
    {
        bool anyMovement = false;
        
        // Duyệt từ dưới lên trên (trừ hàng cuối cùng)
        for (int j = gridHeight - 2; j >= 0; j--)
        {
            // Duyệt zigzag để tránh bias
            if (j % 2 == 0)
            {
                // Hàng chẵn: duyệt từ trái sang phải
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
                // Hàng lẻ: duyệt từ phải sang trái
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
    /// Cập nhật 1 hạt cát tại vị trí (i, j)
    /// Cát sẽ rơi xuống dưới, hoặc chéo trái/phải nếu bị chặn
    /// </summary>
    bool UpdateSandParticle(int i, int j)
    {
        if (_grid[i, j] <= 0) return false;

        // Ưu tiên 1: Rơi thẳng xuống
        if (WithinRows(j + 1) && _grid[i, j + 1] == 0)
        {
            MoveSand(i, j, i, j + 1);
            return true;
        }
        else
        {
            // Ưu tiên 2: Rơi chéo trái hoặc phải
            bool canGoLeft = WithinCols(i - 1) && WithinRows(j + 1) && _grid[i - 1, j + 1] == 0;
            bool canGoRight = WithinCols(i + 1) && WithinRows(j + 1) && _grid[i + 1, j + 1] == 0;

            if (canGoLeft && canGoRight)
            {
                // Cả 2 bên đều trống -> chọn random
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

    /// <summary>
    /// Di chuyển cát từ vị trí (fromX, fromY) sang (toX, toY)
    /// </summary>
    void MoveSand(int fromX, int fromY, int toX, int toY)
    {
        _grid[toX, toY] = _grid[fromX, fromY];
        _colorGrid[toX, toY] = _colorGrid[fromX, fromY];
        _grid[fromX, fromY] = 0;
        _colorGrid[fromX, fromY] = Color.clear;
    }

    /// <summary>
    /// Render grid thành mesh để hiển thị
    /// </summary>
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
        isGameOver = false;
        needsSimulation = false;
        
        // Dừng simulation đang chạy nếu có
        if (activeSimulationCoroutine != null)
        {
            StopCoroutine(activeSimulationCoroutine);
            activeSimulationCoroutine = null;
        }
    }

    bool WithinCols(int i) => i >= 0 && i < gridWidth;
    bool WithinRows(int j) => j >= 0 && j < gridHeight;

    void OnDestroy()
    {
        if (activeSimulationCoroutine != null)
        {
            StopCoroutine(activeSimulationCoroutine);
            activeSimulationCoroutine = null;
        }
    }
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