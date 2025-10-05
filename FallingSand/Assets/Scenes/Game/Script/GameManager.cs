// GameManager.cs

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameManager : Singleton<GameManager>
{
    [Header("Game References")]
    public SandSimulation sandSimulation;
    public GameObject blockPrefab;
    public BoxCollider2D dragAreaCollider;

    [Header("Block Management")]
    public List<BlockData> availableBlocks;
    public List<Transform> spawnPoints;

    private int blocksUsedCount = 0;

    // Quản lý màu sắc
    private List<ColorType> availableColors = new List<ColorType>();
    private int currentMilestonesPassed = 0;

    void Start()
    {
        LoadColorState();

        if (sandSimulation == null || blockPrefab == null || availableBlocks.Count == 0 || spawnPoints.Count < 3 || dragAreaCollider == null)
        {
            Debug.LogError("Vui lòng thiết lập đầy đủ các tham chiếu trong GameManager, bao gồm cả DragAreaCollider!");
            return;
        }
        SpawnNewBlockSet();
    }

    // Khởi tạo màu mặc định
    private void InitializeDefaultColors()
    {
        availableColors.Clear();
        availableColors.Add(ColorType.Green);
        availableColors.Add(ColorType.Blue);
        availableColors.Add(ColorType.Yellow);
        availableColors.Add(ColorType.Red);
        currentMilestonesPassed = 0;
        SaveColorState();
    }

    // Cập nhật màu dựa trên milestone
    public void UpdateColorsBasedOnMilestones(int milestonesPassed)
    {
        if (milestonesPassed > currentMilestonesPassed && milestonesPassed >= 2)
        {
            currentMilestonesPassed = milestonesPassed;

            // 4 màu ban đầu + thêm 1 màu mỗi 2 mốc
            int totalColorsNeeded = 4 + (milestonesPassed / 2);

            while (availableColors.Count < totalColorsNeeded && availableColors.Count < 8)
            {
                ColorType newColor = GetNextColor(availableColors.Count);
                if (!availableColors.Contains(newColor))
                {
                    availableColors.Add(newColor);
                    Debug.Log($"Màu mới được mở khóa: {newColor}");
                }
            }

            SaveColorState();
        }
    }

    private ColorType GetNextColor(int currentColorCount)
    {
        switch (currentColorCount)
        {
            case 4: return ColorType.Pink;
            case 5: return ColorType.Purple;
            case 6: return ColorType.Orange;
            case 7: return ColorType.Cyan;
            default: return ColorType.Green;
        }
    }

    private Color GetColorFromType(ColorType type)
    {
        switch (type)
        {
            case ColorType.Green: return new Color(0, 1, 0.1f);
            case ColorType.Blue: return new Color(0.1f, 0.4f, 1);
            case ColorType.Yellow: return new Color(1, 0.92f, 0.016f);
            case ColorType.Red: return new Color(1, 0.1f, 0.1f);
            case ColorType.Pink: return new Color(1, 0.4f, 0.7f);
            case ColorType.Purple: return new Color(0.6f, 0.2f, 0.8f);
            case ColorType.Orange: return new Color(1, 0.5f, 0);
            case ColorType.Cyan: return new Color(0, 0.8f, 0.8f);
            default: return Color.white;
        }
    }

    // Lấy 3 màu ngẫu nhiên khác nhau
    private List<ColorType> GetThreeUniqueRandomColors()
    {
        List<ColorType> result = new List<ColorType>();
        List<ColorType> tempList = new List<ColorType>(availableColors);

        if (tempList.Count < 3)
        {
            Debug.LogWarning("Không đủ màu! Reset về màu mặc định.");
            InitializeDefaultColors();
            tempList = new List<ColorType>(availableColors);
        }

        for (int i = 0; i < 3; i++)
        {
            int randomIndex = Random.Range(0, tempList.Count);
            result.Add(tempList[randomIndex]);
            tempList.RemoveAt(randomIndex);
        }

        return result;
    }

    void SpawnNewBlockSet()
    {
        // Lấy 3 màu khác nhau
        List<ColorType> threeColors = GetThreeUniqueRandomColors();

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            BlockData randomBlockData = availableBlocks[Random.Range(0, availableBlocks.Count)];
            GameObject blockObject = Instantiate(blockPrefab, spawnPoints[i].position, Quaternion.identity);

            // Sử dụng màu từ danh sách 3 màu đã chọn
            ColorType blockColorType = threeColors[i];
            Color blockColor = GetColorFromType(blockColorType);
            BlockShape shape = randomBlockData.GetBlockShape();

            BlockVisualizer visualizer = blockObject.GetComponent<BlockVisualizer>();
            if (visualizer != null)
            {
                visualizer.GenerateVisual(shape, blockColor, sandSimulation.cellSize);
            }

            DraggableBlock draggable = blockObject.GetComponent<DraggableBlock>();
            if (draggable != null)
            {
                draggable.Initialize(sandSimulation, this, shape, blockColor, dragAreaCollider);
            }
        }
    }

    public void BlockUsed()
    {
        blocksUsedCount++;
        if (blocksUsedCount >= 3)
        {
            blocksUsedCount = 0;
            Invoke(nameof(SpawnNewBlockSet), 0.1f);
        }
    }

    // Reset màu về mặc định
    public void ResetColors()
    {
        InitializeDefaultColors();
    }
    
    // --- NEW METHOD ---
    /// <summary>
    /// Destroys all current draggable blocks and spawns a new set.
    /// </summary>
    public void ResetDraggableBlocks()
    {
        // Find all existing draggable blocks in the scene
        DraggableBlock[] existingBlocks = FindObjectsOfType<DraggableBlock>();
        foreach (DraggableBlock block in existingBlocks)
        {
            Destroy(block.gameObject);
        }

        // Reset the counter and spawn a new set
        blocksUsedCount = 0;
        SpawnNewBlockSet();
    }
    // --- END OF NEW METHOD ---


    private void SaveColorState()
    {
        PlayerPrefs.SetInt("CMP", currentMilestonesPassed);
        PlayerPrefs.SetInt("ACC", availableColors.Count);

        for (int i = 0; i < availableColors.Count; i++)
        {
            PlayerPrefs.SetInt($"AC_{i}", (int)availableColors[i]);
        }

        PlayerPrefs.Save();
    }

    private void LoadColorState()
    {
        currentMilestonesPassed = PlayerPrefs.GetInt("CMP", 0);
        int count = PlayerPrefs.GetInt("ACC", 0);

        if (count > 0)
        {
            availableColors.Clear();
            for (int i = 0; i < count; i++)
            {
                int colorInt = PlayerPrefs.GetInt($"AC_{i}", 0);
                availableColors.Add((ColorType)colorInt);
            }
        }
        else
        {
            InitializeDefaultColors();
        }
    }
    
}