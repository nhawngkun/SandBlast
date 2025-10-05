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
    public float highlightDuration = 0.5f; // Thá»i gian sÃ¡ng lÃªn
    public float highlightIntensity = 2f; // Äá»™ sÃ¡ng (nhÃ¢n vá»›i mÃ u gá»‘c)
    public float fadeOutDuration = 0.3f; // Thá»i gian biáº¿n máº¥t

    private SandSimulation sandSimulation;
    private bool isPlayingEffect = false; // NgÄƒn cháº·n nhiá»u hiá»‡u á»©ng cÃ¹ng lÃºc

    void Start()
    {
        sandSimulation = GetComponent<SandSimulation>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Test báº±ng phÃ­m Space
        {
            CheckAndClearPaths();
        }
    }

    /// <summary>
    /// Kiá»ƒm tra vÃ  xÃ³a táº¥t cáº£ cÃ¡c Ä‘Æ°á»ng ná»‘i cÃ¡t cÃ¹ng mÃ u tá»« cá»™t Ä‘áº§u tiÃªn Ä‘áº¿n cá»™t cuá»‘i cÃ¹ng
    /// CÃ¹ng vá»›i táº¥t cáº£ cÃ¡c háº¡t cÃ¹ng mÃ u liÃªn káº¿t
    /// </summary>
    public int CheckAndClearPaths()
    {
        if (isPlayingEffect) return 0;

        int totalScore = 0;
        HashSet<Vector2Int> allCellsToRemove = new HashSet<Vector2Int>();
        HashSet<Color> processedColors = new HashSet<Color>();

        // Duyá»‡t qua táº¥t cáº£ cÃ¡c Ã´ á»Ÿ cá»™t Ä‘áº§u tiÃªn (x = 0)
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

                    // ThÃªm cÃ¡c Ã´ nÃ y vÃ o danh sÃ¡ch tá»•ng Ä‘á»ƒ xÃ³a
                    foreach (var cell in connectedCells)
                    {
                        allCellsToRemove.Add(cell);
                    }

                    // ================================================================
                    // == PHáº¦N THÃŠM Má»šI Äá»‚ HIá»‚N THá»Š HIá»†U á»¨NG ÄIá»‚M Sá» ==
                    // ================================================================
                    if (scoreForThisColor > 0)
                    {
                        // 1. Má»Ÿ UIetaCore thÃ´ng qua UIManager cá»§a báº¡n
                        UIetaCore scoreEffectUI = UIManager.Instance.OpenUI<UIetaCore>();
                        
                        // 2. Kiá»ƒm tra Ä‘á»ƒ cháº¯c cháº¯n UI Ä‘Ã£ Ä‘Æ°á»£c má»Ÿ thÃ nh cÃ´ng
                        if (scoreEffectUI != null)
                        {
                            // 3. Gá»i phÆ°Æ¡ng thá»©c Ä‘á»ƒ hiá»ƒn thá»‹ hiá»‡u á»©ng vá»›i Ä‘iá»ƒm vÃ  mÃ u tÆ°Æ¡ng á»©ng
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
    /// Coroutine phÃ¡t hiá»‡u á»©ng sÃ¡ng lÃªn rá»“i biáº¿n máº¥t
    /// </summary>
    private IEnumerator PlayClearEffect(HashSet<Vector2Int> cellsToRemove, int score)
    {
        isPlayingEffect = true;

        // LÆ°u mÃ u gá»‘c cá»§a cÃ¡c Ã´
        Dictionary<Vector2Int, Color> originalColors = new Dictionary<Vector2Int, Color>();
        foreach (var cell in cellsToRemove)
        {
            originalColors[cell] = sandSimulation.colorGrid[cell.x, cell.y];
        }

        // Phase 1: SÃ¡ng lÃªn
        foreach (var cell in cellsToRemove)
        {
            Color originalColor = originalColors[cell];
            Color brightColor = originalColor * highlightIntensity;
            brightColor.a = originalColor.a; // Giá»¯ nguyÃªn alpha

            // Tween mÃ u sÃ¡ng lÃªn
            DOTween.To(
                () => sandSimulation.colorGrid[cell.x, cell.y],
                color => sandSimulation.colorGrid[cell.x, cell.y] = color,
                brightColor,
                highlightDuration * 0.5f
            ).SetEase(Ease.OutQuad);
        }

        // Äá»£i hiá»‡u á»©ng sÃ¡ng lÃªn hoÃ n thÃ nh
        yield return new WaitForSeconds(highlightDuration * 0.5f);

        // Phase 2: Giá»¯ sÃ¡ng má»™t chÃºt
        yield return new WaitForSeconds(highlightDuration * 0.5f);

        // Phase 3: Fade out vÃ  xÃ³a
        foreach (var cell in cellsToRemove)
        {
            Color currentColor = sandSimulation.colorGrid[cell.x, cell.y];
            Color transparentColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0);

            // Tween biáº¿n máº¥t
            DOTween.To(
                () => sandSimulation.colorGrid[cell.x, cell.y],
                color => sandSimulation.colorGrid[cell.x, cell.y] = color,
                transparentColor,
                fadeOutDuration
            ).SetEase(Ease.InQuad);
        }

        // Äá»£i hiá»‡u á»©ng biáº¿n máº¥t hoÃ n thÃ nh
        yield return new WaitForSeconds(fadeOutDuration);

        // XÃ³a cÃ¡c Ã´ khá»i grid
        foreach (var cell in cellsToRemove)
        {
            sandSimulation.grid[cell.x, cell.y] = 0;
            sandSimulation.colorGrid[cell.x, cell.y] = Color.clear;
        }

        Debug.Log($"Cleared {cellsToRemove.Count} cells, Score: {score}");

        isPlayingEffect = false;
    }

    /// <summary>
    /// TÃ¬m Ä‘Æ°á»ng Ä‘i tá»« má»™t Ä‘iá»ƒm báº¯t Ä‘áº§u Ä‘áº¿n cá»™t cuá»‘i cÃ¹ng bÃªn pháº£i (BFS)
    /// </summary>
    private List<Vector2Int> FindPathToRightEdge(Vector2Int start, Color targetColor)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);
        parentMap[start] = new Vector2Int(-1, -1); // ÄÃ¡nh dáº¥u Ä‘iá»ƒm báº¯t Ä‘áº§u

        Vector2Int[] directions = GetMovementDirections();

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // Kiá»ƒm tra xem Ä‘Ã£ Ä‘áº¿n cá»™t cuá»‘i cÃ¹ng chÆ°a
            if (current.x == sandSimulation.gridWidth - 1)
            {
                return ReconstructPath(parentMap, current);
            }

            // Duyá»‡t qua cÃ¡c Ã´ lÃ¢n cáº­n
            foreach (var direction in directions)
            {
                Vector2Int next = current + direction;

                // Kiá»ƒm tra Ä‘iá»u kiá»‡n há»£p lá»‡
                if (IsValidCell(next) && !visited.Contains(next))
                {
                    // Kiá»ƒm tra mÃ u sáº¯c cÃ³ khá»›p khÃ´ng
                    if (ColorsMatch(sandSimulation.colorGrid[next.x, next.y], targetColor))
                    {
                        visited.Add(next);
                        parentMap[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }
        }

        return null; // KhÃ´ng tÃ¬m tháº¥y Ä‘Æ°á»ng Ä‘i
    }

    /// <summary>
    /// TÃ¡i táº¡o Ä‘Æ°á»ng Ä‘i tá»« parentMap
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
        path.Add(current); // ThÃªm Ä‘iá»ƒm báº¯t Ä‘áº§u

        path.Reverse(); // Äáº£o ngÆ°á»£c Ä‘á»ƒ cÃ³ Ä‘Æ°á»ng Ä‘i tá»« Ä‘áº§u Ä‘áº¿n cuá»‘i
        return path;
    }

    /// <summary>
    /// Kiá»ƒm tra Ã´ cÃ³ há»£p lá»‡ khÃ´ng
    /// </summary>
    private bool IsValidCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < sandSimulation.gridWidth &&
               cell.y >= 0 && cell.y < sandSimulation.gridHeight &&
               sandSimulation.grid[cell.x, cell.y] > 0;
    }

    /// <summary>
    /// TÃ¬m táº¥t cáº£ cÃ¡c háº¡t cÃ¡t cÃ¹ng mÃ u liÃªn káº¿t vá»›i Ä‘Æ°á»ng Ä‘i báº±ng Flood Fill
    /// </summary>
    private HashSet<Vector2Int> FindAllConnectedCells(Color targetColor, List<Vector2Int> initialPath)
    {
        HashSet<Vector2Int> connectedCells = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        // Báº¯t Ä‘áº§u tá»« táº¥t cáº£ cÃ¡c Ã´ trong Ä‘Æ°á»ng Ä‘i ban Ä‘áº§u
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

        // Flood Fill Ä‘á»ƒ tÃ¬m táº¥t cáº£ háº¡t cÃ¹ng mÃ u liÃªn káº¿t
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // Duyá»‡t qua cÃ¡c Ã´ lÃ¢n cáº­n
            foreach (var direction in directions)
            {
                Vector2Int next = current + direction;

                // Kiá»ƒm tra Ä‘iá»u kiá»‡n há»£p lá»‡
                if (IsValidCell(next) && !visited.Contains(next))
                {
                    // Kiá»ƒm tra mÃ u sáº¯c cÃ³ khá»›p khÃ´ng
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
    /// Láº¥y danh sÃ¡ch cÃ¡c hÆ°á»›ng di chuyá»ƒn dá»±a trÃªn cÃ i Ä‘áº·t
    /// </summary>
    private Vector2Int[] GetMovementDirections()
    {
        if (allowDiagonalMovement)
        {
            // 8 hÆ°á»›ng (tháº³ng + chÃ©o)
            return new Vector2Int[] {
                new Vector2Int(0, -1),   // LÃªn
                new Vector2Int(0, 1),    // Xuá»‘ng
                new Vector2Int(-1, 0),   // TrÃ¡i
                new Vector2Int(1, 0),    // Pháº£i
                new Vector2Int(-1, -1),  // ChÃ©o trÃªn trÃ¡i
                new Vector2Int(1, -1),   // ChÃ©o trÃªn pháº£i
                new Vector2Int(-1, 1),   // ChÃ©o dÆ°á»›i trÃ¡i
                new Vector2Int(1, 1)     // ChÃ©o dÆ°á»›i pháº£i
            };
        }
        else
        {
            // 4 hÆ°á»›ng (chá»‰ tháº³ng)
            return new Vector2Int[] {
                new Vector2Int(0, -1),  // LÃªn
                new Vector2Int(0, 1),   // Xuá»‘ng
                new Vector2Int(-1, 0),  // TrÃ¡i
                new Vector2Int(1, 0)    // Pháº£i
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
    /// TÃ¬m táº¥t cáº£ cÃ¡c Ä‘Æ°á»ng Ä‘i cÃ³ thá»ƒ tá»« cá»™t Ä‘áº§u tiÃªn Ä‘áº¿n cá»™t cuá»‘i cÃ¹ng
    /// CÃ¹ng vá»›i táº¥t cáº£ háº¡t cÃ¹ng mÃ u liÃªn káº¿t
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

                // TrÃ¡nh xá»­ lÃ½ cÃ¹ng má»™t mÃ u nhiá»u láº§n
                if (processedColors.Contains(startColor)) continue;

                List<Vector2Int> path = FindPathToRightEdge(start, startColor);

                if (path != null && path.Count > 0)
                {
                    processedColors.Add(startColor);
                    HashSet<Vector2Int> connectedRegion = FindAllConnectedCells(startColor, path);
                    allRegions.Add(connectedRegion);

                    // ÄÃ¡nh dáº¥u cÃ¡c Ã´ Ä‘Ã£ xá»­ lÃ½
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