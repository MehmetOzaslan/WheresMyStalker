using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public struct DataPoint{
    public string address;
    public string name;
    public int rssi;
    public int txPower;
    public bool isConnectable;
    public float local_x;
    public float local_y;
    public double latitude;
    public double longitude;
    public double timestamp;
}


public class BluetoothLogger : MonoBehaviour
{
    public GameObject _prefabQuad;
    public Gradient colorGradient = new Gradient();
    public float quadOpacity = 0.50f;
    
    public float fade_distance = 1.0f;
    public AnimationCurve fade_curve = new AnimationCurve();

    [Header("Bluetooth Scanner")]
    public BluetoothLEScanner scanner; 
    public Dictionary<Vector3Int, GameObject> _quadMap = new Dictionary<Vector3Int, GameObject>();
    public Dictionary<Vector3Int, List<DataPoint>> _dataMap = new Dictionary<Vector3Int, List<DataPoint>>();

    public event Action<DataPoint> OnDataPointLoggedEvent;

    public string filteredAddress = "default";

    public void OnMACSelected(string address){
        Debug.Log($"BluetoothLogger: MAC {address} selected");
        filteredAddress = address;
    }

    void Start()
    {
        if(fade_curve.keys.Length == 0){
            fade_curve.AddKey(0.0f, 0.0f);
            fade_curve.AddKey(1.0f, 1.0f);
        }

        if (scanner == null)
        {
            scanner = FindFirstObjectByType<BluetoothLEScanner>();
        }
        
        if (scanner != null)
        {
            scanner.OnDeviceFoundEvent += OnDeviceUpdate;
            Debug.Log("BluetoothLogger: Connected to BluetoothLEScanner");
        }
        else
        {
            Debug.LogWarning("BluetoothLogger: BluetoothLEScanner not found! Make sure it's in the scene.");
        }
        
        // Start location service
        StartCoroutine(StartLocationService());
    }
    
    IEnumerator StartLocationService()
    {
        // Check if location is enabled by user
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("BluetoothLogger: Location services are not enabled by user. Please enable in device settings.");
            yield break;
        }

        Input.location.Start(0.5f, 0.5f);
        
        int maxWait = 20; // 20 seconds max wait
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1)
        {
            Debug.LogWarning("BluetoothLogger: Location service timed out during initialization.");
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("BluetoothLogger: Unable to determine device location. Location service failed.");
            yield break;
        }
        else
        {
            Debug.Log($"BluetoothLogger: Location service started. Location: {Input.location.lastData.latitude}, {Input.location.lastData.longitude}");
        }
    }
    
    void OnDestroy()
    {
        if (scanner != null)
        {
            scanner.OnDeviceFoundEvent -= OnDeviceUpdate;
        }
        
        // Stop location service
        if (Input.location.isEnabledByUser && Input.location.status == LocationServiceStatus.Running)
        {
            Input.location.Stop();
            Debug.Log("BluetoothLogger: Location service stopped.");
        }
    }

    float update_frequency = 0.1f;
    float time_since_last_update = 0.0f;

    void Update()
    {
        time_since_last_update += Time.deltaTime;
        if(time_since_last_update >= update_frequency){
            Debug.Log($"BluetoothLogger: Transform position: {transform.position}");
            Debug.Log($"BluetoothLogger: Quad position: {GetVector3Hash(transform.position)}");
            Debug.Log($"BluetoothLogger: Quad map contains key: {_quadMap.ContainsKey(GetVector3Hash(transform.position))}");
        }
    
        if(!_quadMap.ContainsKey(GetVector3Hash(transform.position))){
            CreateQuad(GetVector3Hash(transform.position));
            Debug.Log("BluetoothLogger: Created quad at position");
        }
        else{
            GameObject quad = _quadMap[GetVector3Hash(transform.position)];
            SetQuadColor(quad, colorGradient.Evaluate(PercentRSSI(GetAverageGlobalRSSI(GetVector3Hash(transform.position)))));
        }

        // Not very performant but currently not an issue.
        foreach (var quad in _quadMap){
            float distance = Vector3.Distance(transform.position, quad.Value.transform.position);
            float fade = fade_curve.Evaluate(Mathf.Clamp01(distance / fade_distance));
            Color color = quad.Value.GetComponent<Renderer>().material.color;
            color.a = fade;
            quad.Value.GetComponent<Renderer>().material.color = color;
        }
    }


    // Rough Estimate since there's no absolute RSSI value
    float rssi_min = -100;
    float rssi_max = -30;
    public float PercentRSSI(int rssi){

        float T = (rssi - rssi_min) / (rssi_max - rssi_min);
        return Mathf.Clamp01(Mathf.Abs(T));
    }


    public void OnDeviceUpdate(string address, string name, int rssi, int txPower, bool isConnectable){
        DataPoint dataPoint = new DataPoint();
        dataPoint.address = address;
        dataPoint.name = name;
        dataPoint.rssi = rssi;
        dataPoint.txPower = txPower;
        dataPoint.isConnectable = isConnectable;
        dataPoint.local_x = transform.position.x;
        dataPoint.local_y = transform.position.y;
        if (Input.location.status == LocationServiceStatus.Running)
        {
            dataPoint.latitude = Input.location.lastData.latitude;
            dataPoint.longitude = Input.location.lastData.longitude;
        }
        else
        {
            dataPoint.latitude = 0;
            dataPoint.longitude = 0;
        }
        dataPoint.timestamp = Time.time;

        Vector3Int position = GetVector3Hash(transform.position);
        
        if (!_dataMap.ContainsKey(position))
        {
            _dataMap[position] = new List<DataPoint>();
        }
        _dataMap[position].Add(dataPoint);

        OnDataPointLoggedEvent?.Invoke(dataPoint);


        if(filteredAddress == "default" || filteredAddress == "" || filteredAddress == null){
            GameObject quad = GetObjectAtPosition(transform.position);
            int averageGlobalRSSI = GetAverageGlobalRSSI(position);
            SetQuadColor(quad, colorGradient.Evaluate(PercentRSSI(averageGlobalRSSI)));
        }
        else{
            GameObject quad = GetObjectAtPosition(transform.position);
            int averageSpecificRSSI = GetAverageSpecificRSSI(filteredAddress);
            SetQuadColor(quad, colorGradient.Evaluate(PercentRSSI(averageSpecificRSSI)));
        }
        Debug.Log($"BluetoothLogger: OnDeviceUpdate {address} {name} {rssi} {txPower} {isConnectable}");
    }

    void SetQuadColor(GameObject quad, Color color){
        Renderer renderer = quad.GetComponent<Renderer>();
        if (renderer != null)
        {
            color.a = quadOpacity;
            renderer.material.color = color;
        }
    }

    public int hash_multiplier = 8;
    
    public Vector3Int GetVector3Hash(Vector3 position){
        return new Vector3Int(
            Mathf.FloorToInt(position.x * hash_multiplier), 
            Mathf.FloorToInt(position.y * hash_multiplier), 
            Mathf.FloorToInt(position.z * hash_multiplier)
        );
    }
    
    Vector3 GetWorldPositionFromHash(Vector3Int hashPosition){
        float cellSize = 1f / hash_multiplier;
        return new Vector3(
            hashPosition.x * cellSize + cellSize * 0.5f,
            hashPosition.y * cellSize + cellSize * 0.5f,
            hashPosition.z * cellSize + cellSize * 0.5f
        );
    }

    GameObject GetObjectAtPosition(Vector3 position){
        if(_quadMap.ContainsKey(GetVector3Hash(position))){
            return _quadMap[GetVector3Hash(position)];
        }
        else{
            CreateQuad(GetVector3Hash(position));
            return _quadMap[GetVector3Hash(position)];
        }
    }

    void CreateQuad(Vector3Int position){
        if (_prefabQuad == null)
        {
            Debug.LogError("BluetoothLogger: _prefabQuad is not assigned! Cannot create quad.");
            return;
        }
        
        Vector3 pos = GetWorldPositionFromHash(position);
        GameObject quad = Instantiate(_prefabQuad, pos, Quaternion.identity);
        quad.transform.localScale = new Vector3(1/((float)hash_multiplier+1), 1/((float)hash_multiplier+1), 1/((float)hash_multiplier+1));    
        _quadMap.Add(position, quad);
    }

    int GetAverageGlobalRSSI(Vector3Int position){
        if (!_dataMap.ContainsKey(position)){
            return (int)rssi_min;
        }

        List<DataPoint> dataPoints = _dataMap[position];
        float sum = 0;
        foreach (var dataPoint in dataPoints){
            sum += dataPoint.rssi;
        }
        return (int)(sum / dataPoints.Count);
    }

    int GetAverageSpecificRSSI(string address){

        float sum = 0;
        int count = 0;
        foreach (var data in _dataMap){
            foreach (var dataPoint in data.Value){
                if (dataPoint.address == address){
                    sum += dataPoint.rssi;
                    count++;
                }
            }
        }

        if (count == 0){
            return (int)rssi_min;
        }
        return (int)(sum / count);
    }

}
