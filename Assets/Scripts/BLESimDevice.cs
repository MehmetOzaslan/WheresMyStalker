using UnityEngine;
using System;
using System.Collections;


public class BLESimDevice : MonoBehaviour
{
    // The idea of this class is to provide a way to simulate actual signals from positions in unity editor mode because otherwise building for Android takes forever. 
    // Requires a BluetoothLES scanner to be in the scene to hook into its callback.

    static uint FAKE_MAC_ADDRESS_COUNTER = 0;

    uint FAKE_MAC_ADDRESS = 0;

    public int maxrssi = -35;
    public int minrssi = -110;


    public float rssi_str_multiplier = 1;
    public float max_dist = 20; // meters

    public float update_period = 0.1f;

    [Header("Bluetooth Scanner")]
    public BluetoothLEScanner scanner; 


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    #if !UNITY_EDITOR  
        enabled = false;
        gameObject.SetActive(false);
    #endif

        FAKE_MAC_ADDRESS = FAKE_MAC_ADDRESS_COUNTER;
        FAKE_MAC_ADDRESS_COUNTER += 1;

        // Try to find the BluetoothLEScanner in the scene.
        if (scanner == null)
        {
            scanner = FindFirstObjectByType<BluetoothLEScanner>();
            if (scanner == null)
            {
                Debug.LogWarning("BLESimDevice: BluetoothLEScanner not found in the scene!");
            }
            else
            {
                Debug.Log("BLESimDevice: Found BluetoothLEScanner in scene.");
            }
        }

        if (scanner != null)
        {
            StartCoroutine(FakeDataCoroutine());
        }
    }


    float getRSSI(){
        float rssi = 0;

        if(scanner != null){
            float distance = Vector3.Distance(transform.position, scanner.transform.position);

            distance = Mathf.Clamp(distance, 0, max_dist);
            float t = distance / max_dist;
            rssi = Mathf.Lerp(maxrssi, minrssi, t) * rssi_str_multiplier;
        }

        return rssi;
    }
    
    IEnumerator FakeDataCoroutine()
    {
        while (true)
        {
            float RSSI = getRSSI();
            int txPower = UnityEngine.Random.Range(-10, 10);
            bool isConnectable = UnityEngine.Random.Range(0, 2) == 1;
            string rawData = $"{FAKE_MAC_ADDRESS}|{name}|{(int)RSSI}|{txPower}|{isConnectable}";

            scanner.OnDeviceFound(rawData);

            yield return new WaitForSeconds(UnityEngine.Random.Range(update_period - update_period /2, update_period + update_period /2));
        }
    }


    

}
