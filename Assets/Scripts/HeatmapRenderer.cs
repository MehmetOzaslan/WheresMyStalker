using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VFX;


public class HeatmapRenderer : MonoBehaviour
{
    public GameObject prefabQuad;


    public float prefabQuadScale = 0.2f;

    public float verticalOffset = -0.3f;
    public Gradient colorGradient = new Gradient();
    public float quadOpacity = 0.50f;
    public float fade_distance = 0.2f;
    public AnimationCurve fade_curve = new AnimationCurve();
    
    public BluetoothLogger bluetoothLogger;
    
    private Dictionary<Vector3Int, GameObject> _quadMap = new Dictionary<Vector3Int, GameObject>();
    
    float update_frequency = 0.1f;
    float time_since_last_update = 0.0f;

    string selected_address = "default";

    void Start()
    {
        if(fade_curve.keys.Length == 0){
            fade_curve.AddKey(0.0f, 0.0f);
            fade_curve.AddKey(1.0f, 1.0f);
        }
        
        if (bluetoothLogger == null)
        {
            bluetoothLogger = FindFirstObjectByType<BluetoothLogger>();
        }
        
        if (bluetoothLogger != null)
        {
            bluetoothLogger.OnDataPointLoggedEvent += OnDataPointLogged;
            bluetoothLogger.OnFilteredAddressChangedEvent += OnFilteredAddressChanged;
        }
    }

    void UpdateAllQuadForFilter(string address){
        foreach (var quad in _quadMap){
            int rssi = bluetoothLogger.GetRSSIAtPositionFiltered(quad.Key);
            float percentRSSI = bluetoothLogger.PercentRSSI(rssi);
            SetQuadColor(quad.Value, colorGradient.Evaluate(percentRSSI));
            Debug.Log($"HeatmapRenderer: Quad {quad.Key} has RSSI {rssi} and percent RSSI {percentRSSI}");
        }
    }


    void OnFilteredAddressChanged(string address){
        UpdateAllQuadForFilter(address);
        selected_address = address;
    }
    
    void OnDestroy()
    {
        if (bluetoothLogger != null)
        {
            bluetoothLogger.OnDataPointLoggedEvent -= OnDataPointLogged;
        }
    }

    void Update()
    {
        if (bluetoothLogger == null) return;
        
        time_since_last_update += Time.deltaTime;
        if(time_since_last_update >= update_frequency){
            Debug.Log($"HeatmapRenderer: Transform position: {transform.position}");
            Debug.Log($"HeatmapRenderer: Quad position: {BluetoothLogger.GetVector3Hash(transform.position)}");
            Debug.Log($"HeatmapRenderer: Quad map contains key: {_quadMap.ContainsKey(BluetoothLogger.GetVector3Hash(transform.position))}");
            time_since_last_update = 0f;
        }
    
        Vector3Int currentHash = BluetoothLogger.GetVector3Hash(transform.position);
        if(!_quadMap.ContainsKey(currentHash)){
            CreateQuad(currentHash);
            Debug.Log("HeatmapRenderer: Created quad at position");
        }

        GameObject quad = _quadMap[currentHash];
        int rssi = bluetoothLogger.GetRSSIAtPositionSampledAndFiltered(currentHash, 5);
        float percentRSSI = bluetoothLogger.PercentRSSI(rssi);
        SetQuadColor(quad, colorGradient.Evaluate(percentRSSI));


        foreach (var q in _quadMap){
            float distance = Vector3.Distance(transform.position, q.Value.transform.position);

            if(distance > fade_distance){
                continue;
            }

            float fade = fade_curve.Evaluate(Mathf.Clamp01(distance / fade_distance));

            Color color = q.Value.GetComponent<Renderer>().material.color;
            q.Value.GetComponent<Renderer>().material.color = color;
            q.Value.GetComponent<Renderer>().material.SetColor("_EmissionColor", color);
        }
    }
    
    void OnDataPointLogged(DataPoint dataPoint)
    {
        Vector3 position = new Vector3(dataPoint.local_x, dataPoint.local_y, 0);
        Vector3Int hashPosition = BluetoothLogger.GetVector3Hash(position);
        
        GameObject quad = GetObjectAtPosition(position);
        int rssi = bluetoothLogger.GetRSSIAtPositionFiltered(hashPosition);
        float percentRSSI = bluetoothLogger.PercentRSSI(rssi);
        SetQuadColor(quad, colorGradient.Evaluate(percentRSSI));
    }
    
    void SetQuadColor(GameObject quad, Color color){
        Renderer[] renderers = quad.GetComponentsInChildren<Renderer>();
        color.a = quadOpacity;
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.material.color = color;
                renderer.material.SetColor("_EmissionColor", color * 2);

                if (!renderer.material.IsKeywordEnabled("_EMISSION"))
                    renderer.material.EnableKeyword("_EMISSION");
            }
        }
    }
    

    GameObject GetObjectAtPosition(Vector3 position){
        Vector3Int hashPosition = BluetoothLogger.GetVector3Hash(position);
        if(_quadMap.ContainsKey(hashPosition)){
            return _quadMap[hashPosition];
        }
        else{
            CreateQuad(hashPosition);
            return _quadMap[hashPosition];
        }
    }

    void CreateQuad(Vector3Int position){
        if (prefabQuad == null)
        {
            Debug.LogError("HeatmapRenderer: prefabQuad is not assigned! Cannot create quad.");
            return;
        }
        
        Vector3 pos = BluetoothLogger.GetWorldPositionFromHash(position) + new Vector3(0, 0, verticalOffset);
        GameObject quad = Instantiate(prefabQuad, pos, Quaternion.identity);
        quad.transform.localScale = new Vector3(1/((float)BluetoothLogger.hash_multiplier+1) * prefabQuadScale, 1/((float)BluetoothLogger.hash_multiplier+1) * prefabQuadScale, 1/((float)BluetoothLogger.hash_multiplier+1) * prefabQuadScale);    
        _quadMap.Add(position, quad);
    }
}
