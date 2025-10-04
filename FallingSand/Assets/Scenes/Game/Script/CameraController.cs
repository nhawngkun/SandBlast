using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float gridWidth = 90f;
    public float gridHeight = 110f;
    public float cellSize = 5f;
    public float padding = 50f;
    
    private Camera cam;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        SetupCamera();
    }
    
    void SetupCamera()
    {
        float worldWidth = gridWidth * cellSize;
        float worldHeight = gridHeight * cellSize;
        
        transform.position = new Vector3(worldWidth / 2f, -worldHeight / 2f, -10f);
        
        float aspectRatio = (float)Screen.width / Screen.height;
        float targetHeight = worldHeight + padding * 2;
        float targetWidth = worldWidth + padding * 2;
        
        if (targetWidth / aspectRatio > targetHeight)
        {
            cam.orthographicSize = (targetWidth / aspectRatio) / 2f;
        }
        else
        {
            cam.orthographicSize = targetHeight / 2f;
        }
    }
}