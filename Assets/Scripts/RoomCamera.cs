using UnityEngine;
using UnityEngine.InputSystem;

public class RoomCamera : MonoBehaviour
{
    [Header("Controls")]
    public float orbitSpeed = 100f;
    public float panSpeed = 0.5f;
    public float zoomSpeed = 10f;
    
    [Header("Auto-Framing")]
    public float paddingMultiplier = 1.5f;
    public bool autoFrameOnStart = true;
    public bool trackRoomChanges = true;
    public float yOffset = 0f;
    
    private Vector3 targetPosition;
    private float currentDistance;
    private Vector3 lastRoomSize;
    
    // New Input System references
    private Mouse mouse;
    
    void Start()
    {
        mouse = Mouse.current;
        
        if (autoFrameOnStart)
        {
            FrameRoom();
        }
    }
    
    void LateUpdate()
    {
        if (mouse == null) return;
        
        // Track room size changes
        if (trackRoomChanges)
        {
            Vector3 currentRoomSize = GetRoomBounds();
            if (currentRoomSize != lastRoomSize)
            {
                FrameRoom();
                lastRoomSize = currentRoomSize;
            }
        }
        
        // Get mouse delta
        Vector2 mouseDelta = mouse.delta.ReadValue();
        
        // RIGHT CLICK - ORBIT
        if (mouse.rightButton.isPressed)
        {
            float h = mouseDelta.x * orbitSpeed * Time.deltaTime * 0.1f;
            float v = -mouseDelta.y * orbitSpeed * Time.deltaTime * 0.1f;
            
            transform.RotateAround(targetPosition, Vector3.up, h);
            transform.RotateAround(targetPosition, transform.right, v);
            
            currentDistance = Vector3.Distance(transform.position, targetPosition);
        }
        
        // LEFT CLICK - PAN
        if (mouse.leftButton.isPressed)
        {
            float h = -mouseDelta.x * panSpeed * Time.deltaTime * currentDistance * 0.01f;
            float v = -mouseDelta.y * panSpeed * Time.deltaTime * currentDistance * 0.01f;
            
            Vector3 move = transform.right * h + transform.up * v;
            targetPosition += move;
            transform.position += move;
        }
        
        // MOUSE WHEEL - ZOOM
        float scroll = mouse.scroll.ReadValue().y;
        if (scroll != 0)
        {
            currentDistance -= scroll * zoomSpeed * 0.01f;
            currentDistance = Mathf.Max(2f, currentDistance);
            UpdateCameraPosition();
        }
    }
    
    void FrameRoom()
    {
        WallSizer[] walls = FindObjectsByType<WallSizer>(FindObjectsSortMode.None);
        
        if (walls.Length == 0)
        {
            Debug.LogWarning("RoomCamera: No walls found with WallSizer component");
            return;
        }
        
        Bounds roomBounds = new Bounds(walls[0].transform.position, Vector3.zero);
        foreach (WallSizer wall in walls)
        {
            Vector3 size = wall.transform.localScale;
            Vector3 center = wall.transform.position;
            
            Bounds wallBounds = new Bounds(center, size);
            roomBounds.Encapsulate(wallBounds);
        }
        
        targetPosition = roomBounds.center;
        targetPosition.y = roomBounds.center.y + yOffset;  // CHANGED: Now centers on room's vertical middle
        
        float maxDimension = Mathf.Max(roomBounds.size.x, roomBounds.size.z);
        currentDistance = maxDimension * paddingMultiplier;
        
        float height = roomBounds.size.y * 0.8f;
        Vector3 offset = new Vector3(-1, height / currentDistance, -1).normalized;
        transform.position = targetPosition + offset * currentDistance;
        
        transform.LookAt(targetPosition);
        
        lastRoomSize = GetRoomBounds();
    }
    
    Vector3 GetRoomBounds()
    {
        WallSizer[] walls = FindObjectsByType<WallSizer>(FindObjectsSortMode.None);
        if (walls.Length == 0) return Vector3.zero;
        
        Bounds roomBounds = new Bounds(walls[0].transform.position, Vector3.zero);
        foreach (WallSizer wall in walls)
        {
            roomBounds.Encapsulate(new Bounds(wall.transform.position, wall.transform.localScale));
        }
        
        return roomBounds.size;
    }
    
    void UpdateCameraPosition()
    {
        Vector3 direction = (transform.position - targetPosition).normalized;
        transform.position = targetPosition + direction * currentDistance;
    }
    
    public void ReframeRoom()
    {
        FrameRoom();
    }
}
