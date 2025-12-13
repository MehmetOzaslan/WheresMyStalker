using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class BlUIText : MonoBehaviour
{

    public TextMeshProUGUI text;
    public BluetoothLogger bluetoothLogger;

    public string textString = "MAC:  RSSI:  dBm TX Power:  dBm Is Connectable:  ";
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();

        bluetoothLogger = FindFirstObjectByType<BluetoothLogger>();

        if (bluetoothLogger != null && bluetoothLogger.scanner != null)
        {
            bluetoothLogger.scanner.OnDeviceFoundEvent += OnDeviceUpdate;
        }
    }
    
    void OnDestroy()
    {
        if (bluetoothLogger != null && bluetoothLogger.scanner != null)
        {
            bluetoothLogger.scanner.OnDeviceFoundEvent -= OnDeviceUpdate;
        }
    }

    public void OnDeviceUpdate(string address, string name, int rssi, int txPower, bool isConnectable, string rawData){


        textString = "MAC: " + address + "\n RSSI: " + rssi + " dBm\n TX Power: " + txPower + " dBm\n Is Connectable: " + isConnectable;
        // textString += "\n Latitude: " + Input.location.lastData.latitude + "\n Longitude: " + Input.location.lastData.longitude;
        textString += "\n Transform position: " + bluetoothLogger.transform.position;
        textString += "\n Vector3Hash: " + BluetoothLogger.GetVector3Hash(bluetoothLogger.transform.position);
        textString += "\n Raw Data: " + rawData;


        text.text = textString;
    }

}
