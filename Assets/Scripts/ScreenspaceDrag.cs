using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;

public class ScreenspaceDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    public Camera targetCamera;
    
    public SetPosition positionBinder;
    
    [Header("Zoom Settings")]
    public float zoomSpeed = 0.5f;
    public float minOrthographicSize = 0.5f;
    public float maxOrthographicSize = 40.0f;
    public float pinchSensitivity = 0.01f;
    
    private RectTransform rectT;
    private Vector3 initialWorldPoint;
    private Vector2 initialScreenPoint;
    private bool isDragging = false;
    
    // Pinch zoom variables
    private float lastPinchDistance = 0f;
    private bool isPinching = false;

    void Start()
    {
        rectT = GetComponent<RectTransform>();
        
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        // Enable enhanced touch support for better touch input
        EnhancedTouch.EnhancedTouchSupport.Enable();
    }
    
    void Update()
    {
        HandlePinchZoom();
    }
    
    void HandlePinchZoom()
    {
        if (targetCamera == null || !targetCamera.orthographic) return;
        
        // Get active touches
        var touchCount = EnhancedTouch.Touch.activeTouches.Count;
        
        if (touchCount == 2)
        {
            // Get the two touch points
            var touch1 = EnhancedTouch.Touch.activeTouches[0];
            var touch2 = EnhancedTouch.Touch.activeTouches[1];
            
            // Calculate distance between the two touches
            float currentDistance = Vector2.Distance(touch1.screenPosition, touch2.screenPosition);
            
            if (!isPinching)
            {
                // Start of pinch gesture
                isPinching = true;
                lastPinchDistance = currentDistance;
            }
            else
            {
                // Calculate the change in distance
                float deltaDistance = currentDistance - lastPinchDistance;
                
                // Apply zoom based on pinch delta
                float currentSize = targetCamera.orthographicSize;
                float newSize = currentSize - (deltaDistance * pinchSensitivity);
                
                // Clamp to min/max bounds
                newSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);
                
                // Apply the zoom
                targetCamera.orthographicSize = newSize;
                
                // Update last distance for next frame
                lastPinchDistance = currentDistance;
            }
        }
        else
        {
            // Reset pinch state when not pinching
            isPinching = false;
            lastPinchDistance = 0f;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Prevent drag from starting if multiple touches are active (pinch gesture)
        var touchCount = EnhancedTouch.Touch.activeTouches.Count;
        if (touchCount > 1)
        {
            return;
        }

        isDragging = true;
        initialScreenPoint = eventData.position;

        if (positionBinder != null)
        {
            positionBinder.enabled = false;
        }
        
        Vector3 screenPoint = new Vector3(initialScreenPoint.x, initialScreenPoint.y, targetCamera.nearClipPlane);
        initialWorldPoint = targetCamera.ScreenToWorldPoint(screenPoint);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || targetCamera == null) return;
        
        // Stop dragging if multiple touches are detected (user started pinching)
        var touchCount = EnhancedTouch.Touch.activeTouches.Count;
        if (touchCount > 1)
        {
            isDragging = false;
            return;
        }
        
        // Convert current screen point to world coordinates
        Vector3 currentScreenPoint = new Vector3(eventData.position.x, eventData.position.y, targetCamera.nearClipPlane);
        Vector3 currentWorldPoint = targetCamera.ScreenToWorldPoint(currentScreenPoint);
        
        // Calculate the world space delta from initial click to current position
        Vector3 worldDelta = currentWorldPoint - initialWorldPoint;
        
        // Move camera by the inverse of the delta (so the world point under cursor stays fixed)
        // This maintains the absolute world coordinate relationship
        targetCamera.transform.position -= worldDelta;
        
        // Recalculate initial world point based on new camera position for next frame
        initialScreenPoint = eventData.position;
        initialWorldPoint = targetCamera.ScreenToWorldPoint(new Vector3(initialScreenPoint.x, initialScreenPoint.y, targetCamera.nearClipPlane));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (targetCamera == null || !targetCamera.orthographic) return;
        
        float scrollDelta = eventData.scrollDelta.y;
        
        float currentSize = targetCamera.orthographicSize;
        float newSize = currentSize - (scrollDelta * zoomSpeed);
        
        newSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);
        
        targetCamera.orthographicSize = newSize;
    }
}
