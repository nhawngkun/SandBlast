using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class DraggableBlock : MonoBehaviour
{
    private Vector3 initialPosition;
    private Vector3 offset;
    private Camera mainCamera;
    private bool isDragging = false;

    private SandSimulation sandSimulation;
    private GameManager gameManager;
    private BlockShape shape;
    private Color blockColor;

    // --- THÊM CÁC BIẾN MỚI ---
    private BoxCollider2D dragArea;
    private Bounds blockMeshBounds;
    // --- KẾT THÚC THÊM BIẾN ---

    public void Initialize(SandSimulation sim, GameManager manager, BlockShape blockShape, Color color, BoxCollider2D dragCollider)
    {
        sandSimulation = sim;
        gameManager = manager;
        shape = blockShape;
        blockColor = color;
        dragArea = dragCollider; // << Gán collider

        mainCamera = Camera.main;
        initialPosition = transform.position;

        // Lấy bounds của mesh để tính toán giới hạn
        blockMeshBounds = GetComponent<MeshFilter>().mesh.bounds;
    }

    private void OnMouseDown()
    {
        offset = transform.position - GetMouseWorldPos();
        isDragging = true;
        SoundManager.Instance.PlayVFXSound(5);
    }

    private void OnMouseDrag()
    {
        if (!isDragging || dragArea == null) return;

        // --- LOGIC GIỚI HẠN KÉO THẢ ---
        Vector3 targetPosition = GetMouseWorldPos() + offset;

        // Lấy giới hạn của vùng kéo và kích thước của khối
        Bounds dragBounds = dragArea.bounds;
        Vector3 blockExtents = blockMeshBounds.extents;

        // Kẹp vị trí của khối để nó không đi ra ngoài vùng collider
        float clampedX = Mathf.Clamp(targetPosition.x, dragBounds.min.x + blockExtents.x, dragBounds.max.x - blockExtents.x);
        float clampedY = Mathf.Clamp(targetPosition.y, dragBounds.min.y + blockExtents.y, dragBounds.max.y - blockExtents.y);

        transform.position = new Vector3(clampedX, clampedY, targetPosition.z);
        // --- KẾT THÚC LOGIC GIỚI HẠN ---
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        // --- LOGIC KIỂM TRA VỊ TRÍ THẢ MỚI (ĐÃ THAY ĐỔI) ---
        if (IsAnyBlockCellOverlappingSimulationBounds())
        {
            // Nếu có, thêm khối vào mô phỏng.
            sandSimulation.AddSandBlock(shape, blockColor, transform.position);
            SoundManager.Instance.PlayVFXSound(4); // Âm thanh khi cát chạm vào hộp
            gameManager.BlockUsed();
            Destroy(gameObject);
        }
        else
        {
            SoundManager.Instance.PlayVFXSound(5);
            // Nếu không có phần nào chạm vào lưới, trả khối về vị trí ban đầu
            transform.position = initialPosition;
        }
        // --- KẾT THÚC LOGIC MỚI ---
    }

    /// <summary>
    /// **(HÀM MỚI)** Kiểm tra xem có ít nhất một ô của khối giao thoa (overlap)
    /// với khu vực lưới mô phỏng hay không.
    /// </summary>
    private bool IsAnyBlockCellOverlappingSimulationBounds()
    {
        float worldWidth = sandSimulation.gridWidth * sandSimulation.cellSize;
        float worldHeight = sandSimulation.gridHeight * sandSimulation.cellSize;
        Vector3 blockCenter = transform.position;

        foreach (var cell in shape.cells)
        {
            // Tính toán vị trí thế giới của từng ô nhỏ cấu thành nên khối
            float cellWorldX = blockCenter.x + (cell.x * sandSimulation.cellSize) - blockMeshBounds.center.x;
            float cellWorldY = blockCenter.y - (cell.y * sandSimulation.cellSize) - blockMeshBounds.center.y;

            // Lấy các cạnh của ô
            float cellRightEdge = cellWorldX + sandSimulation.cellSize;
            float cellLeftEdge = cellWorldX;
            float cellTopEdge = cellWorldY;
            float cellBottomEdge = cellWorldY - sandSimulation.cellSize;

            // Điều kiện để một ô KHÔNG giao với lưới là:
            // 1. Toàn bộ ô nằm bên phải lưới (cellLeftEdge > worldWidth)
            // 2. Toàn bộ ô nằm bên trái lưới (cellRightEdge < 0)
            // 3. Toàn bộ ô nằm bên trên lưới (cellBottomEdge > 0)
            // 4. Toàn bộ ô nằm bên dưới lưới (cellTopEdge < -worldHeight)
            // Nếu không thỏa mãn bất kỳ điều kiện nào ở trên, tức là nó CÓ giao thoa.
            bool isOverlapping = !(cellLeftEdge > worldWidth || cellRightEdge < 0 || cellBottomEdge > 0 || cellTopEdge < -worldHeight);

            if (isOverlapping)
            {
                return true; // Chỉ cần một ô giao thoa là đủ, trả về true ngay lập tức
            }
        }

        return false; // Nếu duyệt hết tất cả các ô mà không có ô nào giao thoa
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mainCamera.WorldToScreenPoint(transform.position).z;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }
}