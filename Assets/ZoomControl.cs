using UnityEngine;

public class ZoomControl : MonoBehaviour
{

    float min_size = 0.5f;
    public float max_size = 10.0f;

    float cur_size = 5;
    
    public Camera camera;

    void Start()
    {
        if (camera == null)
        {
            camera = GetComponent<Camera>();
            if (camera == null)
            {
                camera = Camera.main;
            }
        }
        
        if (camera != null)
        {
            cur_size = camera.orthographicSize;
        }
        camera.orthographicSize = cur_size;
        min_size = cur_size;
    }

    public void OnScrollUpdate(float in_amnt){
        if (camera == null) return;
        in_amnt = Mathf.Clamp01(in_amnt);        
        cur_size = Mathf.Lerp(min_size, max_size, in_amnt);
        camera.orthographicSize = cur_size;
    }

}
