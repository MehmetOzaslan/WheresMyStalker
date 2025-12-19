using UnityEngine;

public class SetScaleFromCamera : MonoBehaviour
{
    public Camera targetCamera;
    public float scaleMultiplier = 1.0f; // Multiplier for the scale calculation
    public bool scaleUniformly = true; // If true, scales X, Y, Z equally; if false, can set individual axes
    
    private float baseOrthographicSize;
    private Vector3 baseScale;

    void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }
        
        if (targetCamera != null)
        {
            baseOrthographicSize = targetCamera.orthographicSize;
            baseScale = transform.localScale;
        }
    }

    void Update()
    {
        if (targetCamera == null || !targetCamera.orthographic) return;
        
        // Calculate scale factor based on orthographic size ratio
        float scaleFactor = (targetCamera.orthographicSize / baseOrthographicSize) * scaleMultiplier;
        
        if (scaleUniformly)
        {
            // Scale uniformly based on orthographic size
            transform.localScale = baseScale * scaleFactor;
        }
        else
        {
            // Scale each axis independently (useful for UI elements or specific scaling needs)
            transform.localScale = new Vector3(
                baseScale.x * scaleFactor,
                baseScale.y * scaleFactor,
                baseScale.z * scaleFactor
            );
        }
    }
}
