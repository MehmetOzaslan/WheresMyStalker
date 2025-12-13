package com.unity_lib_helper.unityble

import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothManager
import android.bluetooth.le.*
import android.content.Context
import android.os.Build
import android.util.Base64
import android.util.Log
import androidx.core.util.isEmpty
import java.util.UUID

/**
 * Unity-compatible Bluetooth LE Scanner
 * Exposes Bluetooth scanning functionality to Unity via UnitySendMessage
 * 
 * This class wraps Android's BluetoothLeScanner and provides a simple interface
 * for Unity games to scan for BLE devices. Scan results are sent to Unity
 * via UnitySendMessage using reflection to avoid compile-time dependency on Unity classes.
 */
class UnityBLEScanner private constructor(private val context: Context) {

    companion object {
        private const val TAG = "UnityBLEScanner"
        private var instance: UnityBLEScanner? = null

        /**
         * Get or create the singleton instance
         * @param context Application context
         * @return UnityBLEScanner instance
         */
        @JvmStatic
        fun getInstance(context: Context): UnityBLEScanner {
            if (instance == null) {
                instance = UnityBLEScanner(context.applicationContext)
            }
            return instance!!
        }




        /**
         * Get existing instance if available
         * @return UnityBLEScanner instance or null
         */
        @JvmStatic
        fun getInstance(): UnityBLEScanner? {
            return instance
        }

        /**
         * Set the Unity GameObject name that will receive callbacks
         * @param name GameObject name in Unity scene
         */
        @JvmStatic
        fun setUnityGameObjectName(name: String) {
            instance?.unityGameObjectName = name
        }

        /**
         * Initialize the scanner
         * @param context Application context (optional if instance already exists)
         * @return true if Bluetooth is available and enabled
         */
        @JvmStatic
        fun initialize(context: Context?): Boolean {
            val scanner = if (context != null) {
                getInstance(context)
            } else {
                instance ?: run {
                    Log.e(TAG, "No instance available. Call initialize(context) with context first.")
                    return false
                }
            }
            
            val adapter = scanner.bluetoothManager?.adapter
            val scannerAvailable = scanner.bluetoothLeScanner != null
            val initialized = adapter != null && adapter.isEnabled && scannerAvailable
            
            if (!initialized) {
                Log.w(TAG, "Bluetooth initialization failed. Available: ${adapter != null}, Enabled: ${adapter?.isEnabled}, Scanner: $scannerAvailable")
            } else {
                Log.d(TAG, "Bluetooth scanner initialized successfully")
            }

            return initialized
        }

        /**
         * Start scanning for BLE devices
         * @param scanMode Scan mode (0=LOW_POWER, 1=BALANCED, 2=LOW_LATENCY, 3=OPPORTUNISTIC)
         */
        @JvmStatic
        fun startScan(scanMode: Int = ScanSettings.SCAN_MODE_LOW_LATENCY) {
            val scanner = instance
            if (scanner == null) {
                Log.e(TAG, "Scanner instance is null. Call initialize() first.")
                return
            }
            scanner.startScanInternal(scanMode)
        }

        /**
         * Stop scanning
         */
        @JvmStatic
        fun stopScan() {
            instance?.stopScanInternal()
        }

        /**
         * Check if currently scanning
         * @return true if scan is active
         */
        @JvmStatic
        fun isScanning(): Boolean {
            return instance?.isScanning ?: false
        }
    }

    private val bluetoothManager: BluetoothManager? by lazy {
        context.getSystemService(Context.BLUETOOTH_SERVICE) as? BluetoothManager
    }

    private val bluetoothLeScanner: BluetoothLeScanner?
        get() = bluetoothManager?.adapter?.bluetoothLeScanner

    private var scanCallback: ScanCallback? = null
    private var isScanning = false

    // Unity GameObject name to send messages to
    private var unityGameObjectName: String = "BluetoothLEScanner"

    private fun startScanInternal(scanMode: Int) {
        if (isScanning) {
            Log.w(TAG, "Scan already running")
            return
        }

        val scanner = bluetoothLeScanner ?: run {
            Log.e(TAG, "BluetoothLeScanner is null")
            sendToUnity("OnScanFailed", "SCANNER_NULL")
            return
        }

        val settingsBuilder = ScanSettings.Builder()
            .setScanMode(scanMode)
        
        // Enable scanning on all supported PHY layers (Bluetooth 5.0+ feature)
        // Only available on Android 8.0+ (API 26+)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            try {
                settingsBuilder.setPhy(ScanSettings.PHY_LE_ALL_SUPPORTED)
                Log.d(TAG, "Enabled PHY_LE_ALL_SUPPORTED for multi-PHY scanning")
            } catch (e: Exception) {
                Log.w(TAG, "PHY_LE_ALL_SUPPORTED not available on this device: ${e.message}")
            }
        }
        
        val settings = settingsBuilder.build()

        scanCallback = object : ScanCallback() {
            override fun onScanResult(callbackType: Int, result: ScanResult) {
                super.onScanResult(callbackType, result)

                // Send device info to Unity as pipe-delimited string
                val deviceInfo = buildDeviceInfo(result)
                sendToUnity("OnDeviceFound", deviceInfo)
            }

            override fun onBatchScanResults(results: MutableList<ScanResult>) {
                super.onBatchScanResults(results)
                for (result in results) {
                    val deviceInfo = buildDeviceInfo(result)
                    sendToUnity("OnDeviceFound", deviceInfo)
                }
            }

            override fun onScanFailed(errorCode: Int) {
                super.onScanFailed(errorCode)
                val errorMsg = when (errorCode) {
                    ScanCallback.SCAN_FAILED_ALREADY_STARTED -> "ALREADY_STARTED"
                    ScanCallback.SCAN_FAILED_APPLICATION_REGISTRATION_FAILED -> "REGISTRATION_FAILED"
                    ScanCallback.SCAN_FAILED_FEATURE_UNSUPPORTED -> "FEATURE_UNSUPPORTED"
                    ScanCallback.SCAN_FAILED_INTERNAL_ERROR -> "INTERNAL_ERROR"
                    else -> "UNKNOWN_$errorCode"
                }
                Log.e(TAG, "Scan failed: $errorMsg")
                sendToUnity("OnScanFailed", errorMsg)
                isScanning = false
            }
        }

        try {
            scanner.startScan(null, settings, scanCallback)
            isScanning = true
            Log.d(TAG, "BLE scan started with mode: $scanMode")
            sendToUnity("OnScanStarted", "SUCCESS")
        } catch (e: SecurityException) {
            Log.e(TAG, "SecurityException: Missing BLUETOOTH_SCAN permission", e)
            sendToUnity("OnScanFailed", "PERMISSION_DENIED")
        } catch (e: Exception) {
            Log.e(TAG, "Failed to start scan", e)
            sendToUnity("OnScanFailed", "EXCEPTION: ${e.message}")
        }
    }


    private fun stopScanInternal() {
        if (!isScanning) {
            Log.d(TAG, "No active scan to stop")
            return
        }

        scanCallback?.let { callback ->
            try {
                bluetoothLeScanner?.stopScan(callback)
                Log.d(TAG, "BLE scan stopped")
                sendToUnity("OnScanStopped", "SUCCESS")
            } catch (e: Exception) {
                Log.e(TAG, "Failed to stop scan", e)
            }
        }

        scanCallback = null
        isScanning = false
    }


    /**
     * Build device info string for Unity
     * Extended format with all available advertising data
     * Format: "address|name|rssi|txPower|isConnectable|deviceType|primaryPhy|secondaryPhy|advertisingSetId|periodicInterval|manufacturerData|serviceUuids|serviceData|advertisingFlags|rawBytes|timestamp"
     * 
     * @param result ScanResult from Android
     * @return Pipe-delimited string with device information
     */
    private fun buildDeviceInfo(result: ScanResult): String {
        // Basic device info (fields 1-5, original format for backward compatibility)
        val address = result.device.address ?: "UNKNOWN"
        val name = result.device.name ?: ""
        val rssi = result.rssi
        val txPower = result.txPower
        val isConnectable = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            result.isConnectable
        } else {
            true // Default for older versions
        }

        // Escape pipes in strings
        val sanitizedName = name.replace("|", "_")
        
        // Device type
        val deviceType = when (result.device.type) {
            BluetoothDevice.DEVICE_TYPE_LE -> "LE"
            BluetoothDevice.DEVICE_TYPE_CLASSIC -> "CLASSIC"
            BluetoothDevice.DEVICE_TYPE_DUAL -> "DUAL"
            else -> "UNKNOWN"
        }
        
        // PHY info (Android 8.0+)
        val primaryPhy = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            result.primaryPhy
        } else {
            -1
        }
        
        val secondaryPhy = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            result.secondaryPhy
        } else {
            -1
        }

        // Advertising set ID (Android 8.0+)
        val advertisingSetId = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            result.advertisingSid ?: -1
        } else {
            -1
        }
        
        // Periodic advertising interval (Android 8.0+)
        val periodicInterval = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            result.periodicAdvertisingInterval ?: -1
        } else {
            -1
        }
        
        // Timestamp
        val timestamp = result.timestampNanos / 1_000_000 // Convert nanoseconds to milliseconds
        
        // Extract ScanRecord (advertising data)
        val scanRecord = result.scanRecord
        val manufacturerData = extractManufacturerData(scanRecord)
        val serviceUuids = extractServiceUuids(scanRecord)
        val serviceData = extractServiceData(scanRecord)
        val advertisingFlags = extractAdvertisingFlags(scanRecord)
        val rawBytes = extractRawBytes(scanRecord)
        
       return buildString {
            append(address)
            append("|")
            append(sanitizedName)
            append("|")
            append(rssi)
            append("|")
            append(txPower)
            append("|")
            append(isConnectable)
            append("|")
            append(deviceType)
            append("|")
            append(primaryPhy)
            append("|")
            append(secondaryPhy)
            append("|")
            append(advertisingSetId)
            append("|")
            append(periodicInterval)
            append("|")
            append(manufacturerData)
            append("|")
            append(serviceUuids)
            append("|")
            append(serviceData)
            append("|")
            append(advertisingFlags)
            append("|")
            append(rawBytes)
            append("|")
            append(timestamp)
        }
    }
    
    /**
     * Extract manufacturer data from ScanRecord
     * Format: "companyId:base64data,companyId2:base64data2"
     */
    private fun extractManufacturerData(scanRecord: ScanRecord?): String {
        val manufacturerData = scanRecord?.manufacturerSpecificData ?: return ""
        if (manufacturerData.size() == 0) return ""

        val builder = StringBuilder()
        for (i in 0 until manufacturerData.size()) {
            val companyId = manufacturerData.keyAt(i)
            val data = manufacturerData.valueAt(i)
            val base64Data = Base64.encodeToString(data, Base64.NO_WRAP)

            if (builder.isNotEmpty()) {
                builder.append(',')
            }
            builder.append("$companyId:$base64Data")
        }
        return builder.toString()
    }
    
    /**
     * Extract all service UUIDs from ScanRecord as 128-bit UUIDs
     * Format: comma-separated UUIDs
     */
    private fun extractServiceUuids(scanRecord: ScanRecord?): String {
        if (scanRecord == null) return ""
        
        val serviceUuids = scanRecord.serviceUuids
        if (serviceUuids == null || serviceUuids.isEmpty()) {
            return ""
        }
        
        return serviceUuids
            .filter { it != null }
            .joinToString(",") { it.toString().uppercase() }
    }
    
    /**
     * Extract service data from ScanRecord
     * Format: "uuid:base64data,uuid2:base64data2"
     */
    private fun extractServiceData(scanRecord: ScanRecord?): String {
        if (scanRecord == null) return ""
        
        val serviceData = scanRecord.getServiceData()
        if (serviceData == null || serviceData.isEmpty()) {
            return ""
        }
        
        return serviceData.entries.joinToString(",") { (uuid, data) ->
            val base64Data = Base64.encodeToString(data, Base64.NO_WRAP)
            "${uuid.toString().uppercase()}:$base64Data"
        }
    }
    
    /**
     * Extract advertising flags from ScanRecord
     */
    private fun extractAdvertisingFlags(scanRecord: ScanRecord?): String {
        if (scanRecord == null) return ""
        
        val advertiseFlags = scanRecord.advertiseFlags
        return if (advertiseFlags != null && advertiseFlags >= 0) {
            advertiseFlags.toString()
        } else {
            ""
        }
    }
    
    /**
     * Extract raw advertising bytes from ScanRecord
     * Returns Base64 encoded string
     */
    private fun extractRawBytes(scanRecord: ScanRecord?): String {
        if (scanRecord == null) return ""
        
        val bytes = scanRecord.bytes
        return if (bytes != null && bytes.isNotEmpty()) {
            Base64.encodeToString(bytes, Base64.NO_WRAP)
        } else {
            ""
        }
    }

    /**
     * Send message to Unity using reflection
     * This avoids compile-time dependency on Unity classes
     * 
     * @param methodName Method name in Unity C# script
     * @param data Data to pass to Unity method
     */
    private fun sendToUnity(methodName: String, data: String) {
        try {
            // Use reflection to call UnityPlayer.UnitySendMessage
            // This avoids compile-time dependency on Unity classes
            val unityPlayerClass = Class.forName("com.unity3d.player.UnityPlayer")
            val unitySendMessageMethod = unityPlayerClass.getMethod(
                "UnitySendMessage",
                String::class.java,
                String::class.java,
                String::class.java
            )
            unitySendMessageMethod.invoke(null, unityGameObjectName, methodName, data)
        } catch (e: ClassNotFoundException) {
            Log.w(TAG, "Unity classes not found. Running outside Unity? Message: $methodName")
        } catch (e: Exception) {
            Log.e(TAG, "Failed to send message to Unity: $methodName", e)
        }
    }
}

