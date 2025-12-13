using UnityEngine;
using System;
using System.Collections;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// Unity C# bridge for Android Bluetooth LE Scanner
/// </summary>
public class BluetoothLEScanner : MonoBehaviour
{
    private AndroidJavaObject scannerPlugin;
    private bool isInitialized = false;
    private AndroidJavaObject unityActivity;

    // Events for Unity
    public event Action<string, string, int, int, bool, string> OnDeviceFoundEvent; // address, name, rssi, txPower, isConnectable, (raw data)
    public event Action<string> OnRawDeviceInfoReceivedEvent;
    public event Action<string> OnScanFailed;
    public event Action OnScanStarted;
    public event Action OnScanStopped;

    private bool permissionsRequested = false;




    void Start()
    {
        RequestPermissions();
    }

    void RequestPermissions()
    {
#if UNITY_ANDROID
        if (permissionsRequested) return;
        permissionsRequested = true;
        
        var permissions = new System.Collections.Generic.List<string>();
        
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            permissions.Add(Permission.FineLocation);
        }
        
        if (!Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
        {
            permissions.Add(Permission.CoarseLocation);
        }

        if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN"))
        {
            permissions.Add("android.permission.BLUETOOTH_SCAN");
        }
        if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_CONNECT"))
        {
            permissions.Add("android.permission.BLUETOOTH_CONNECT");
        }



        if (permissions.Count > 0)
        {
            PermissionCallbacks callbacks = new PermissionCallbacks();
            callbacks.PermissionDenied += OnPermissionDenied;
            callbacks.PermissionGranted += OnPermissionGranted;
            
            Permission.RequestUserPermissions(permissions.ToArray(), callbacks);
        }
        else
        {
            Initialize();
            StartScan(2);
        }
#else
        Initialize();
        StartScan(2);
#endif
    }

#if UNITY_ANDROID
    void OnPermissionGranted(string permission)
    {
        Debug.Log($"Permission granted: {permission}");
        CheckAllPermissionsGranted();
    }
    
    void OnPermissionDenied(string permission)
    {
        Debug.LogWarning($"Permission denied: {permission}. User must enable it in settings.");
    }
    
    void CheckAllPermissionsGranted()
    {
        bool hasLocation = Permission.HasUserAuthorizedPermission(Permission.FineLocation) ||
                          Permission.HasUserAuthorizedPermission(Permission.CoarseLocation);
        
        if (hasLocation)
        {
            Initialize();
            StartScan(2);
        }
    }
#endif


    void Initialize()
    {
        // Gets the library instance for the bluetooth scanner
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject context = unityActivity.Call<AndroidJavaObject>("getApplicationContext");

            AndroidJavaClass scannerClass = new AndroidJavaClass("com.unity_lib_helper.unityble.UnityBLEScanner");
            scannerPlugin = scannerClass.CallStatic<AndroidJavaObject>("getInstance", context);

            bool initialized = scannerClass.CallStatic<bool>("initialize", context);

            if (initialized)
            {
                isInitialized = true;
                Debug.Log("Bluetooth LE Scanner initialized successfully");
            }
            else
            {
                Debug.LogError("Failed to initialize Bluetooth LE Scanner. Check if Bluetooth is enabled.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initializing Bluetooth scanner: {e.Message}");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Start scanning for BLE devices
    /// </summary>
    /// <param name="scanMode">0=LOW_POWER, 1=BALANCED, 2=LOW_LATENCY, 3=OPPORTUNISTIC</param>
    public void StartScan(int scanMode = 2)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Scanner not initialized. Attempting to initialize...");
            Initialize();
            if (!isInitialized) return;
        }

        try
        {
            AndroidJavaClass scannerClass = new AndroidJavaClass("com.unity_lib_helper.unityble.UnityBLEScanner");
            scannerClass.CallStatic("startScan", scanMode);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error starting scan: {e.Message}");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Stop scanning
    /// </summary>
    public void StopScan()
    {
        if (!isInitialized) return;

        try
        {
            AndroidJavaClass scannerClass = new AndroidJavaClass("com.unity_lib_helper.unityble.UnityBLEScanner");
            scannerClass.CallStatic("stopScan");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error stopping scan: {e.Message}");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Check if currently scanning
    /// </summary>
    public bool IsScanning()
    {
        if (!isInitialized) return false;

        try
        {
            AndroidJavaClass scannerClass = new AndroidJavaClass("com.unity_lib_helper.unityble.UnityBLEScanner");
            return scannerClass.CallStatic<bool>("isScanning");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error checking scan status: {e.Message}");
            return false;
        }
    }


    /// Called by Android when a device sends a packet
    /// Format: "address|name|rssi|txPower|isConnectable (raw data)"
    public void OnDeviceFound(string deviceInfo)
    {

        try
        {
            OnRawDeviceInfoReceivedEvent?.Invoke(deviceInfo);
            string[] parts = deviceInfo.Split('|');
            if (parts.Length >= 5)
            {
                string address = parts[0];
                string name = parts[1];
                int rssi = int.Parse(parts[2]);
                int txPower = int.Parse(parts[3]);
                bool isConnectable = bool.Parse(parts[4]);

                Debug.Log($"Device found: {name} ({address}) RSSI: {rssi} dBm, TX Power: {txPower} dBm, Connectable: {isConnectable}");

                OnDeviceFoundEvent?.Invoke(address, name, rssi, txPower, isConnectable, deviceInfo);
            }
            else
            {
                Debug.LogWarning($"Invalid device info format: {deviceInfo}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing device info: {e.Message}. Data: {deviceInfo}");
            Debug.LogException(e);
        }
    }

    public void OnScanFailedCallback(string error)
    {
        Debug.LogError($"BLE Scan failed: {error}");
        OnScanFailed?.Invoke(error);
    }

    public void OnScanStartedCallback(string status)
    {
        Debug.Log($"BLE Scan started: {status}");
        OnScanStarted?.Invoke();
    }

    public void OnScanStoppedCallback(string status)
    {
        Debug.Log($"BLE Scan stopped: {status}");
        OnScanStopped?.Invoke();
    }

    void OnDestroy()
    {
        if (IsScanning())
        {
            StopScan();
        }
    }
}

