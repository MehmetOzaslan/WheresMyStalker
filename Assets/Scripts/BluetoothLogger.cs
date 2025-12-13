using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

public struct DataPoint{
    public uint point_id;
    public int rssi;
    public int txPower;
    public bool isConnectable;
    public float local_x;
    public float local_y;
    public float local_z;
    public double latitude;
    public double longitude;
    public double timestamp;
}


// Logs data continously and publishes to the datamap. 
// Data is also published in the OnDataPointLoggedEvent.
public class BluetoothLogger : MonoBehaviour
{
    [Header("Bluetooth Scanner")]
    public BluetoothLEScanner scanner; 

    public Dictionary<Vector3Int, List<DataPoint>> _dataMap = new Dictionary<Vector3Int, List<DataPoint>>();

    public Dictionary<uint, string> _rawDataMap = new Dictionary<uint, string>();

    // packet id -> mac id
    public Dictionary<uint, int> _addressMap = new Dictionary<uint, int>();

    // mac id -> name
    public Dictionary<int, string> _nameMap = new Dictionary<int, string>();


    // mac id -> mac string
    public Dictionary<int, string> id_mac_map = new Dictionary<int, string>();

    // mac string -> mac id
    public Dictionary<string, int> mac_id_map = new Dictionary<string, int>();



    // this is fairly nasty
    public Dictionary<string, List<string>> address_raw_data_map = new Dictionary<string, List<string>>();

    public event Action<DataPoint> OnDataPointLoggedEvent;

    public event Action<String> OnFilteredAddressChangedEvent;


    public string filteredAddress = "default";

    public string GetAddressFromMacId(int macId){
        return id_mac_map.ContainsKey(macId) ? id_mac_map[macId] : null;
    }

    public void OnMACSelected(string address){
        Debug.Log($"BluetoothLogger: MAC {address} selected");
        filteredAddress = address;
        OnFilteredAddressChangedEvent?.Invoke(address);
    }



    public void SaveData(){
        Debug.Log($"BluetoothLogger: Saving data to {Application.persistentDataPath + "/data.csv"}");
        string filePath = Application.persistentDataPath + "/data.csv";

        using (StreamWriter writer = new StreamWriter(filePath, false))
        {
            writer.WriteLine("address,name,rssi,txPower,isConnectable,local_x,local_y,local_z,latitude,longitude,timestamp,rawData");
        }
        
        foreach (var data in _dataMap){
            foreach (var dataPoint in data.Value){
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {   
                    int macId = _addressMap[dataPoint.point_id];
                    string address = GetAddressFromMacId(macId);
                    writer.WriteLine($"{address},{_nameMap[macId]},{dataPoint.rssi},{dataPoint.txPower},{dataPoint.isConnectable},{dataPoint.local_x},{dataPoint.local_y},{dataPoint.local_z},{dataPoint.latitude},{dataPoint.longitude},{dataPoint.timestamp},{_rawDataMap[dataPoint.point_id]}");
                }
            }
        }
    }

    void Start()
    {
        if (scanner == null)
        {
            scanner = FindFirstObjectByType<BluetoothLEScanner>();
        }
        
        if (scanner != null)
        {
            scanner.OnDeviceFoundEvent += OnDeviceUpdate;
            // scanner.OnRawDeviceInfoReceivedEvent += OnRawDeviceInfoReceived;
            Debug.Log("BluetoothLogger: Connected to BluetoothLEScanner");
        }
        else
        {
            Debug.LogWarning("BluetoothLogger: BluetoothLEScanner not found! Make sure it's in the scene.");
        }
        
        // Start location service
        StartCoroutine(StartLocationService());
    }

    // public void OnRawDeviceInfoReceived(string rawData){
    //     Debug.Log($"BluetoothLogger: OnDeviceFoundRawData {rawData}");
    // }
    
    IEnumerator StartLocationService()
    {
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("BluetoothLogger: Location services are not enabled by user. Please enable in device settings.");
            yield break;
        }

        Input.location.Start(0.5f, 0.5f);
        
        int maxWait = 20;
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
        
        SaveData();
        // Stop location service
        if (Input.location.isEnabledByUser && Input.location.status == LocationServiceStatus.Running)
        {
            Input.location.Stop();
            Debug.Log("BluetoothLogger: Location service stopped.");
        }
    }


    // Rough Estimate since there's no absolute RSSI value
    float rssi_min = -110;
    float rssi_max = -35;

    public float PercentRSSI(int rssi, double perfectRssi = -20.0, double worstRssi = -90.0)
    {
        float signalQuality = (rssi - rssi_min) / (rssi_max - rssi_min);

        return (float)signalQuality;
    }


    static uint point_id_counter = 0;

    private void RegisterMacAddress(string address, out int macId)
    {
        if (!mac_id_map.TryGetValue(address, out macId))
        {
            macId = mac_id_map.Count;
            mac_id_map[address] = macId;
            id_mac_map[macId] = address;
        }
    }

    public void OnDeviceUpdate(string address, string name, int rssi, int txPower, bool isConnectable, string rawData){
        DataPoint dataPoint = new DataPoint();
        dataPoint.point_id = point_id_counter++;
        dataPoint.rssi = rssi;
        dataPoint.txPower = txPower;
        dataPoint.isConnectable = isConnectable;
        dataPoint.local_x = transform.position.x;
        dataPoint.local_y = transform.position.y;
        dataPoint.local_z = transform.position.z;
        _rawDataMap[dataPoint.point_id] = rawData;


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

        // Register address and get macId
        RegisterMacAddress(address, out int macId);
        _addressMap[dataPoint.point_id] = macId;
        
        // Populate name map (mac_id -> name)
        _nameMap[macId] = name;
        
        OnDataPointLoggedEvent?.Invoke(dataPoint);
            
        // Debug.Log($"BluetoothLogger: OnDeviceUpdate {address} {name} {rssi} {txPower} {isConnectable}");
    }


    public static int hash_multiplier = 7;
    
    public static Vector3Int GetVector3Hash(Vector3 position){
        return new Vector3Int(
            Mathf.FloorToInt(position.x * hash_multiplier), 
            Mathf.FloorToInt(position.y),
            Mathf.FloorToInt(position.z * hash_multiplier)
        );
    }

    public static Vector3 GetWorldPositionFromHash(Vector3Int hashPosition){
        float cellSize = 1f / (float)hash_multiplier;
        return new Vector3(
            hashPosition.x * cellSize + cellSize * 0.5f,
            hashPosition.y + 0.5f,
            hashPosition.z * cellSize + cellSize * 0.5f
        );
    }
    
    public int GetRSSIAtPositionFiltered(Vector3Int position){

        if(filteredAddress == "default" || filteredAddress == "" || filteredAddress == null){
            return GetAverageGlobalRSSI(position);
        }
        else{
            return GetAverageSpecificRSSIAtPosition(position, filteredAddress);
        }
    }
    
    public int GetAverageSpecificRSSIAtPosition(Vector3Int position, string address){
        if (!_dataMap.ContainsKey(position)){
            return (int)rssi_min;
        }

        if (!mac_id_map.ContainsKey(address)){
            return (int)rssi_min;
        }
        int targetMacId = mac_id_map[address];

        List<DataPoint> dataPoints = _dataMap[position];
        float sum = 0;
        int count = 0;
        foreach (var dataPoint in dataPoints){
            if (_addressMap[dataPoint.point_id] == targetMacId){
                sum += dataPoint.rssi;
                count++;
            }
        }

        if (count == 0){
            return (int)rssi_min;
        }
        return (int)(sum / count);
    }
    
    public int GetRSSIAtPositionSampledAndFiltered(Vector3Int position, int sample_size = 5)
    {
        if(filteredAddress == "default" || string.IsNullOrEmpty(filteredAddress))
        {
            if (!_dataMap.ContainsKey(position))
                return (int)rssi_min;

            List<DataPoint> dataPoints = _dataMap[position];
            if (dataPoints.Count == 0)
                return (int)rssi_min;

            dataPoints.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
            
            int samplesToTake = Mathf.Min(sample_size, dataPoints.Count);
            float sum = 0f;
            
            for (int i = dataPoints.Count - samplesToTake; i < dataPoints.Count; i++)
            {
                sum += dataPoints[i].rssi;
            }
            
            return (int)(sum / samplesToTake);
        }
        else
        {
            if (!_dataMap.ContainsKey(position))
                return (int)rssi_min;

            if (!mac_id_map.ContainsKey(filteredAddress))
                return (int)rssi_min;
            int targetMacId = mac_id_map[filteredAddress];

            List<DataPoint> filteredPoints = new List<DataPoint>();
            foreach (var dataPoint in _dataMap[position])
            {
                if (_addressMap[dataPoint.point_id] == targetMacId)
                {
                    filteredPoints.Add(dataPoint);
                }
            }
            
            if (filteredPoints.Count == 0)
                return (int)rssi_min;

            filteredPoints.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
            
            int samplesToTake = Mathf.Min(sample_size, filteredPoints.Count);
            float sum = 0f;
            
            for (int i = filteredPoints.Count - samplesToTake; i < filteredPoints.Count; i++)
            {
                sum += filteredPoints[i].rssi;
            }
            
            return (int)(sum / samplesToTake);
        }
    }



    public int GetAverageGlobalRSSI(Vector3Int position){
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

    public int GetAverageSpecificRSSI(string address){

        if (!mac_id_map.ContainsKey(address)){
            return (int)rssi_min;
        }
        int targetMacId = mac_id_map[address];

        float sum = 0;
        int count = 0;
        foreach (var data in _dataMap){
            foreach (var dataPoint in data.Value){
                if (_addressMap[dataPoint.point_id] == targetMacId){
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

    public int GetAverageSpecificRSSISampled(string address, int sample_size = 5){
        if (!mac_id_map.ContainsKey(address)){
            return (int)rssi_min;
        }
        int targetMacId = mac_id_map[address];

        List<DataPoint> matchingPoints = new List<DataPoint>();
        
        foreach (var data in _dataMap){
            foreach (var dataPoint in data.Value){
                if (_addressMap[dataPoint.point_id] == targetMacId){
                    matchingPoints.Add(dataPoint);
                }
            }
        }

        if (matchingPoints.Count == 0){
            return (int)rssi_min;
        }
        
        matchingPoints.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
        
        int samplesToTake = Mathf.Min(sample_size, matchingPoints.Count);
        float sum = 0f;
        
        for (int i = matchingPoints.Count - samplesToTake; i < matchingPoints.Count; i++)
        {
            sum += matchingPoints[i].rssi;
        }
        
        return (int)(sum / samplesToTake);
    }

}
