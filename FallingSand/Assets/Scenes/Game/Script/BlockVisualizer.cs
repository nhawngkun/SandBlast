using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BlockVisualizer : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private int[] triangles;

    void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh;
    }

    /// <summary>
    /// Vẽ hình dạng khối cát dựa trên dữ liệu hình dạng, màu sắc và kích thước ô.
    /// </summary>
    public void GenerateVisual(BlockShape shape, Color color, float cellSize)
    {
        mesh.Clear();

        // Lấy số lượng ô vuông (mỗi ô là 1 quad)
        int quadCount = shape.cells.Length;

        // Cấp phát bộ nhớ cho mesh
        vertices = new Vector3[quadCount * 4];
        colors = new Color[quadCount * 4];
        triangles = new int[quadCount * 6];

        // Tìm điểm trung tâm để căn khối vào giữa
        Vector3 centerOffset = CalculateCenter(shape, cellSize);

        for (int i = 0; i < quadCount; i++)
        {
            Vector2Int cell = shape.cells[i];
            
            // Tính toán vị trí 4 đỉnh của ô vuông
            float x = cell.x * cellSize;
            float y = -cell.y * cellSize; // Dùng -y để khớp với hệ tọa độ của mô phỏng

            int vertexIndex = i * 4;
            vertices[vertexIndex]     = new Vector3(x, y, 0) - centerOffset;
            vertices[vertexIndex + 1] = new Vector3(x, y - cellSize, 0) - centerOffset;
            vertices[vertexIndex + 2] = new Vector3(x + cellSize, y - cellSize, 0) - centerOffset;
            vertices[vertexIndex + 3] = new Vector3(x + cellSize, y, 0) - centerOffset;

            // Gán màu
            colors[vertexIndex]     = color;
            colors[vertexIndex + 1] = color;
            colors[vertexIndex + 2] = color;
            colors[vertexIndex + 3] = color;

            // Tạo 2 tam giác cho ô vuông
            int triangleIndex = i * 6;
            triangles[triangleIndex]     = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 2;
            triangles[triangleIndex + 2] = vertexIndex + 1;
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + 3;
            triangles[triangleIndex + 5] = vertexIndex + 2;
        }

        // Gán dữ liệu vào mesh
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    // Hàm này tính toán tâm của khối để căn nó vào giữa object
    private Vector3 CalculateCenter(BlockShape shape, float cellSize)
    {
        if (shape.cells.Length == 0) return Vector3.zero;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var cell in shape.cells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
            if (cell.y < minY) minY = cell.y;
            if (cell.y > maxY) maxY = cell.y;
        }

        // Trung tâm được tính dựa trên các ô ngoài cùng
        float centerX = (minX + maxX + 1) * cellSize / 2f;
        float centerY = (-minY - maxY - 1) * cellSize / 2f;

        return new Vector3(centerX, centerY, 0);
    }
}