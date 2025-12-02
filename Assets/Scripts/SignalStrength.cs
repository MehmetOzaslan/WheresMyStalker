using UnityEngine;
using UnityEngine.UI;

public class SignalStrength : MonoBehaviour
{

    public Slider slider;

    public int sample_size = 5;

    public BluetoothLogger bluetoothLogger;
    


    void Start(){
        if(bluetoothLogger == null){
            bluetoothLogger = FindFirstObjectByType<BluetoothLogger>();
            
        }
    }
    
    void Update()
    {
        int rssi = bluetoothLogger.GetRSSIAtPositionSampledAndFiltered(BluetoothLogger.GetVector3Hash(bluetoothLogger.transform.position), sample_size);
        slider.value = bluetoothLogger.PercentRSSI(rssi);
    }
}
