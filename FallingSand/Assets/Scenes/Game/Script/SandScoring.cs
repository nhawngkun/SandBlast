using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System.Collections;

public class SandScoring : MonoBehaviour
{
    [Header("Scoring Settings")]
    public int pointsPerCell = 10;
    public float clearDelay = 0.1f;
    public bool allowDiagonalMovement = true;

    [Header("Effect Settings")]
    public float highlightDuration = 0.5f; // Thời gian sáng lên
    public float highlightIntensity = 2f; // Độ sáng (nhân với màu gốc)
    public float fadeOutDuration = 0.3f; // Thời gian biến mất

    private SandSimulation sandSimulation;
    private bool isPlayingEffect = false; // Ngăn chặn nhiều hiệu ứng cùng lúc

    void Start()
    {
        sandSimulation = GetComponent<SandSimulation>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Test bằng phím Space
        {
            CheckAndClearPaths();
        }
    }

    /// <summary>
    /// Kiểm tra và xóa tất cả các đường nối cát cùng màu từ cột đầu tiên đến cột cuối cùng
    /// Cùng với tất cả các hạt cùng màu liên kết
    /// </summary>
    public int CheckAndClearPaths()
    {
        if (isPlayingEffect) return 0;

        int totalScore = 0;
        HashSet<Vector2Int> allCellsToRemove = new HashSet<Vector2Int>();
        HashSet<Color> processedColors = new HashSet<Color>();

        // Duyệt qua tất cả các ô ở cột đầu tiên (x = 0)
        for (int y = 0; y < sandSimulation.gridHeight; y++)
        {
            if (sandSimulation.grid[0, y] > 0)
            {
                Color startColor = sandSimulation.colorGrid[0, y];

                if (processedColors.Contains(startColor)) continue;

                List<Vector2Int> path = FindPathToRightEdge(new Vector2Int(0, y), startColor);

                if (path != null && path.Count > 0)
                {
                    processedColors.Add(startColor);

                    HashSet<Vector2Int> connectedCells = FindAllConnectedCells(startColor, path);
                    int scoreForThisColor = connectedCells.Count * pointsPerCell;
                    totalScore += scoreForThisColor;

                    // Thêm các ô này vào danh sách tổng để xóa
                    foreach (var cell in connectedCells)
                    {
                        allCellsToRemove.Add(cell);
                    }

                    // ================================================================
                    // == PHẦN THÊM MỚI ĐỂ HIỂN THỊ HIỆU ỨNG ĐIỂM SỐ ==
                    // ================================================================
                    if (scoreForThisColor > 0)
                    {
                        // 1. Mở UIetaCore thông qua UIManager của bạn
                        UIetaCore scoreEffectUI = UIManager.Instance.OpenUI<UIetaCore>();
                        
                        // 2. Kiểm tra để chắc chắn UI đã được mở thành công
                        if (scoreEffectUI != null)
                        {
                            // 3. Gọi phương thức để hiển thị hiệu ứng với điểm và màu tương ứng
                            scoreEffectUI.ShowScoreEffect(scoreForThisColor, startColor);
                        }
                    }
                    // ================================================================
                }
            }
        }

        if (allCellsToRemove.Count > 0)
        {
           
            var uiCore = FindObjectOfType<UICore>();
            if (uiCore != null)
            {
                uiCore.AddScore(totalScore);
            }
            
            
            SoundManager.Instance.PlayVFXSound(2);
            
           
            StartCoroutine(PlayClearEffect(allCellsToRemove, totalScore));
        }

        return totalScore;
    }

    /// <summary>
    /// Coroutine phát hiệu ứng sáng lên rồi biến mất
    /// </summary>
    private IEnumerator PlayClearEffect(HashSet<Vector2Int> cellsToRemove, int score)
    {
        isPlayingEffect = true;

        // Lưu màu gốc của các ô
        Dictionary<Vector2Int, Color> originalColors = new Dictionary<Vector2Int, Color>();
        foreach (var cell in cellsToRemove)
        {
            originalColors[cell] = sandSimulation.colorGrid[cell.x, cell.y];
        }

        // Phase 1: Sáng lên
        foreach (var cell in cellsToRemove)
        {
            Color originalColor = originalColors[cell];
            Color brightColor = originalColor * highlightIntensity;
            brightColor.a = originalColor.a; // Giữ nguyên alpha

            // Tween màu sáng lên
            DOTween.To(
                () => sandSimulation.colorGrid[cell.x, cell.y],
                color => sandSimulation.colorGrid[cell.x, cell.y] = color,
                brightColor,
                highlightDuration * 0.5f
            ).SetEase(Ease.OutQuad);
        }

        // Đợi hiệu ứng sáng lên hoàn thành
        yield return new WaitForSeconds(highlightDuration * 0.5f);

        // Phase 2: Giữ sáng một chút
        yield return new WaitForSeconds(highlightDuration * 0.5f);

        // Phase 3: Fade out và xóa
        foreach (var cell in cellsToRemove)
        {
            Color currentColor = sandSimulation.colorGrid[cell.x, cell.y];
            Color transparentColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0);

            // Tween biến mất
            DOTween.To(
                () => sandSimulation.colorGrid[cell.x, cell.y],
                color => sandSimulation.colorGrid[cell.x, cell.y] = color,
                transparentColor,
                fadeOutDuration
            ).SetEase(Ease.InQuad);
        }

        // Đợi hiệu ứng biến mất hoàn thành
        yield return new WaitForSeconds(fadeOutDuration);

        // Xóa các ô khỏi grid
        foreach (var cell in cellsToRemove)
        {
            sandSimulation.grid[cell.x, cell.y] = 0;
            sandSimulation.colorGrid[cell.x, cell.y] = Color.clear;
        }

        Debug.Log($"Cleared {cellsToRemove.Count} cells, Score: {score}");

        isPlayingEffect = false;
        
        // ================================================================
        // == QUAN TRỌNG: Kích hoạt simulation sau khi xóa cát ==
        // ================================================================
        // Cát phía trên sẽ rơi xuống chỗ trống
        sandSimulation.TriggerSimulation();
        // ================================================================
    }

    /// <summary>
    /// Tìm đường đi từ một điểm bắt đầu đến cột cuối cùng bên phải (BFS)
    /// </summary>
    private List<Vector2Int> FindPathToRightEdge(Vector2Int start, Color targetColor)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);
        parentMap[start] = new Vector2Int(-1, -1); // Đánh dấu điểm bắt đầu

        Vector2Int[] directions = GetMovementDirections();

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // Kiểm tra xem đã đến cột cuối cùng chưa
            if (current.x == sandSimulation.gridWidth - 1)
            {
                return ReconstructPath(parentMap, current);
            }

            // Duyệt qua các ô lân cận
            foreach (var direction in directions)
            {
                Vector2Int next = current + direction;

                // Kiểm tra điều kiện hợp lệ
                if (IsValidCell(next) && !visited.Contains(next))
                {
                    // Kiểm tra màu sắc có khớp không
                    if (ColorsMatch(sandSimulation.colorGrid[next.x, next.y], targetColor))
                    {
                        visited.Add(next);
                        parentMap[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }
        }

        return null; // Không tìm thấy đường đi
    }

    /// <summary>
    /// Tái tạo đường đi từ parentMap
    /// </summary>
    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> parentMap, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = end;

        while (parentMap.ContainsKey(current) && parentMap[current] != new Vector2Int(-1, -1))
        {
            path.Add(current);
            current = parentMap[current];
        }
        path.Add(current); // Thêm điểm bắt đầu

        path.Reverse(); // Đảo ngược để có đường đi từ đầu đến cuối
        return path;
    }

    /// <summary>
    /// Kiểm tra ô có hợp lệ không
    /// </summary>
    private bool IsValidCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < sandSimulation.gridWidth &&
               cell.y >= 0 && cell.y < sandSimulation.gridHeight &&
               sandSimulation.grid[cell.x, cell.y] > 0;
    }

    /// <summary>
    /// Tìm tất cả các hạt cát cùng màu liên kết với đường đi bằng Flood Fill
    /// </summary>
    private HashSet<Vector2Int> FindAllConnectedCells(Color targetColor, List<Vector2Int> initialPath)
    {
        HashSet<Vector2Int> connectedCells = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        // Bắt đầu từ tất cả các ô trong đường đi ban đầu
        foreach (var pathCell in initialPath)
        {
            if (!visited.Contains(pathCell))
            {
                queue.Enqueue(pathCell);
                visited.Add(pathCell);
                connectedCells.Add(pathCell);
            }
        }

        Vector2Int[] directions = GetMovementDirections();

        // Flood Fill để tìm tất cả hạt cùng màu liên kết
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // Duyệt qua các ô lân cận
            foreach (var direction in directions)
            {
                Vector2Int next = current + direction;

                // Kiểm tra điều kiện hợp lệ
                if (IsValidCell(next) && !visited.Contains(next))
                {
                    // Kiểm tra màu sắc có khớp không
                    if (ColorsMatch(sandSimulation.colorGrid[next.x, next.y], targetColor))
                    {
                        visited.Add(next);
                        connectedCells.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }
        }

        return connectedCells;
    }

    /// <summary>
    /// Lấy danh sách các hướng di chuyển dựa trên cài đặt
    /// </summary>
    private Vector2Int[] GetMovementDirections()
    {
        if (allowDiagonalMovement)
        {
            // 8 hướng (thẳng + chéo)
            return new Vector2Int[] {
                new Vector2Int(0, -1),   // Lên
                new Vector2Int(0, 1),    // Xuống
                new Vector2Int(-1, 0),   // Trái
                new Vector2Int(1, 0),    // Phải
                new Vector2Int(-1, -1),  // Chéo trên trái
                new Vector2Int(1, -1),   // Chéo trên phải
                new Vector2Int(-1, 1),   // Chéo dưới trái
                new Vector2Int(1, 1)     // Chéo dưới phải
            };
        }
        else
        {
            // 4 hướng (chỉ thẳng)
            return new Vector2Int[] {
                new Vector2Int(0, -1),  // Lên
                new Vector2Int(0, 1),   // Xuống
                new Vector2Int(-1, 0),  // Trái
                new Vector2Int(1, 0)    // Phải
            };
        }
    }

    private bool ColorsMatch(Color color1, Color color2)
    {
        float threshold = 0.1f;
        return Mathf.Abs(color1.r - color2.r) < threshold &&
               Mathf.Abs(color1.g - color2.g) < threshold &&
               Mathf.Abs(color1.b - color2.b) < threshold;
    }

    /// <summary>
    /// Tìm tất cả các đường đi có thể từ cột đầu tiên đến cột cuối cùng
    /// Cùng với tất cả hạt cùng màu liên kết
    /// </summary>
    public List<HashSet<Vector2Int>> FindAllConnectedRegions()
    {
        List<HashSet<Vector2Int>> allRegions = new List<HashSet<Vector2Int>>();
        HashSet<Vector2Int> processedCells = new HashSet<Vector2Int>();
        HashSet<Color> processedColors = new HashSet<Color>();

        for (int y = 0; y < sandSimulation.gridHeight; y++)
        {
            Vector2Int start = new Vector2Int(0, y);
            if (sandSimulation.grid[start.x, start.y] > 0 && !processedCells.Contains(start))
            {
                Color startColor = sandSimulation.colorGrid[start.x, start.y];

                // Tránh xử lý cùng một màu nhiều lần
                if (processedColors.Contains(startColor)) continue;

                List<Vector2Int> path = FindPathToRightEdge(start, startColor);

                if (path != null && path.Count > 0)
                {
                    processedColors.Add(startColor);
                    HashSet<Vector2Int> connectedRegion = FindAllConnectedCells(startColor, path);
                    allRegions.Add(connectedRegion);

                    // Đánh dấu các ô đã xử lý
                    foreach (var cell in connectedRegion)
                    {
                        processedCells.Add(cell);
                    }
                }
            }
        }

        return allRegions;
    }
}