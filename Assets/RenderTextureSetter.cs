using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Rendering;

public class RenderTextureSetter : MonoBehaviour
{
    public Camera targetCamera;
    public RawImage image; 
    public RenderTexture renderTexture;

    void Start()
    {
        // Create RenderTexture with R8G8B8_SRGB color format and D16_UNORM depth format
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(512, 512);
        
        // Set color format to R8G8B8_SRGB
        GraphicsFormat colorFormat = GraphicsFormat.R8G8B8_SRGB;
        GraphicsFormat depthFormat = GraphicsFormat.D16_UNorm;
        
        // Check if formats are supported
        bool colorSupported = SystemInfo.IsFormatSupported(colorFormat, GraphicsFormatUsage.Render);
        bool depthSupported = SystemInfo.IsFormatSupported(depthFormat, GraphicsFormatUsage.Render);
        
        if (colorSupported && depthSupported)
        {
            Debug.Log($"RenderTextureSetter: R8G8B8_SRGB color format and D16_UNORM depth format are supported on this device.");
            descriptor.graphicsFormat = colorFormat;
            descriptor.depthStencilFormat = depthFormat;
            renderTexture = new RenderTexture(descriptor);
            renderTexture.Create();
        }
        else
        {
            Debug.LogWarning($"RenderTextureSetter: GraphicsFormat not fully supported (Color: {colorSupported}, Depth: {depthSupported}). Falling back to legacy format.");
            // Fallback to legacy format
            descriptor.colorFormat = RenderTextureFormat.ARGB32;
            descriptor.sRGB = true;
            descriptor.depthBufferBits = 16;
            renderTexture = new RenderTexture(descriptor);
            renderTexture.Create();
        }
        
        // Bind to camera
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
            targetCamera.targetTexture = renderTexture;
            Debug.Log($"RenderTextureSetter: RenderTexture bound to camera: {targetCamera.name}");
        }
        else
        {
            Debug.LogError("RenderTextureSetter: No camera found!");
        }
        
        // Bind to RawImage
        if (image == null)
        {
            image = GetComponent<RawImage>();
        }
        
        if (image != null)
        {
            image.texture = renderTexture;
            Debug.Log($"RenderTextureSetter: RenderTexture bound to RawImage: {image.name}");
        }
        else
        {
            Debug.LogWarning("RenderTextureSetter: No RawImage component found!");
        }
    }


    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            if (Application.isPlaying)
            {
                Destroy(renderTexture);
            }
            else
            {
                DestroyImmediate(renderTexture);
            }
        }
    }
}
