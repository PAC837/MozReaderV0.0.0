using UnityEngine;
using UnityEngine.InputSystem;

public class RoomCamera : MonoBehaviour
{
    [Header("Controls")]
    public float orbitSpeed = 100f;
    public float panSpeed = 0.5f;
    public float zoomSpeed = 10f;
    
    [Header("Camera Settings")]
    public float defaultDistance = 10f;
    public Vector3 defaultTarget = Vector3.zero;
    
    private Vector3 targetPosition;
    private float currentDistance;
    
    // New Input System references
    private Mouse mouse;
    
    void Start()
    {
        mouse = Mouse.current;
        
        // Initialize camera position and target
        targetPosition = defaultTarget;
        currentDistance = defaultDistance;
        UpdateCameraPosition();
    }
    
    void LateUpdate()
    {
        if (mouse == null) return;
        
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
        
        // MIDDLE CLICK - PAN (changed from left to avoid conflict with selection)
        if (mouse.middleButton.isPressed)
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
    
    
    void UpdateCameraPosition()
    {
        Vector3 direction = (transform.position - targetPosition).normalized;
        transform.position = targetPosition + direction * currentDistance;
    }
    
    public void SetTarget(Vector3 newTarget)
    {
        targetPosition = newTarget;
        UpdateCameraPosition();
    }
    
    public void SetDistance(float newDistance)
    {
        currentDistance = Mathf.Max(2f, newDistance);
        UpdateCameraPosition();
    }
}
