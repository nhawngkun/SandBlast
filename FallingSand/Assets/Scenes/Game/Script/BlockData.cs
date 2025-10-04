using UnityEngine;
[CreateAssetMenu(fileName = "New Block Data", menuName = "Sand Blocks/Block Data")]
public class BlockData : ScriptableObject
{
    [Header("Block Shape Settings")]
    public BlockType blockType;
    public int subSquareSize = 7;

    // Phương thức GetBlockShape() giữ nguyên
    public BlockShape GetBlockShape()
    {
        int size = subSquareSize;
        Vector2Int[] baseOffsets;
        switch (blockType)
        {
            case BlockType.Square:
            default:
                baseOffsets = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
                break;
            case BlockType.LShape:
                baseOffsets = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2) };
                break;
            case BlockType.TShape:
                baseOffsets = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1) };
                break;
            case BlockType.ZShape:
                baseOffsets = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1) };
                break;
            case BlockType.IHorizontal:
                // I ngang: 4 ô liên tiếp theo trục x
                baseOffsets = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
                break;
            case BlockType.THorizontal:
                // T ngang: 3 ô ngang + 1 ô ở giữa phía dưới
                baseOffsets = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, -1) };
                break;
            case BlockType.TReverse:
                // T ngược: 3 ô ngang + 1 ô ở giữa phía trên
                baseOffsets = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1) };
                break;
            case BlockType.LHorizontal:
                // L ngang: 3 ô ngang + 1 ô ở dưới cùng bên phải
                baseOffsets = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(2, -1) };
                break;
        }

        Vector2Int[] cells = new Vector2Int[size * size * 4];
        int index = 0;
        foreach (var offset in baseOffsets)
        {
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    cells[index++] = new Vector2Int(offset.x * size + x, offset.y * size + y);
                }
            }
        }
        // Trả về BlockShape với cells
        return new BlockShape(cells);
    }
}