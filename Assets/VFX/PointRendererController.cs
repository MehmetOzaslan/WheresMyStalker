using UnityEngine;
using UnityEngine.VFX;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;


public class PointRendererController : MonoBehaviour{

    // Passed into the VFX graph.
    [StructLayout(LayoutKind.Sequential)]
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
    public struct PointStruct{
        public Vector3 position;
        public float signalStrength;
        public int mac_indexed_id;
        public float timestamp;
    }



    public VisualEffect visualEffect;
    public BluetoothLogger bluetoothLogger;
    
    [Header("Point Settings")]
    public float updateInterval = 0.3f;

    static int MAX_POINTS = 100000;
    PointStruct[] pointsCPU = new PointStruct[MAX_POINTS];


    private GraphicsBuffer points;
    private Coroutine updateCoroutine;

    public string macIndexedIdProperty = "MacIndexedId";
    public string pointCountProperty = "PointCount";
    public string pointsProperty = "Points";
    public string playerPositionProperty = "PlayerPosition";

    int writeHead = 0;
    int pointCount = 0;

    void Start(){
        int pointCountID = Shader.PropertyToID(pointCountProperty);
        int pointsID     = Shader.PropertyToID(pointsProperty);

        Debug.Log($"HasInt(PointCount): {visualEffect.HasInt(pointCountID)}");
        Debug.Log($"HasUInt(PointCount): {visualEffect.HasUInt(pointCountID)}");
        Debug.Log($"HasGraphicsBuffer(Points): {visualEffect.HasGraphicsBuffer(pointsID)}");

        if (visualEffect == null)
        {
            visualEffect = GetComponent<VisualEffect>();
        }
        
        if (bluetoothLogger == null)
        {
            bluetoothLogger = FindFirstObjectByType<BluetoothLogger>();
        }
        
        bluetoothLogger.OnFilteredAddressChangedEvent += OnMacSelected;

        bluetoothLogger.OnDataPointLoggedEvent += OnDataPointLogged;

        InitBuffer();
    }


    void OnDataPointLogged(DataPoint dataPoint){
        if (writeHead >= MAX_POINTS)
        {
            writeHead = 0;
        }
        pointsCPU[writeHead] = new PointStruct{position = new Vector3(dataPoint.local_x, dataPoint.local_y, dataPoint.local_z), signalStrength = bluetoothLogger.PercentRSSI(dataPoint.rssi), mac_indexed_id = bluetoothLogger.mac_id_map[dataPoint.address], timestamp = (float)dataPoint.timestamp};
        writeHead++;
        pointCount++;
    }
    
    void OnDisable(){
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
    }

    void OnDestroy()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
        
        if (points != null)
        {
            points.Release();
            points = null;
        }
    }

    void InitBuffer(){
        if (points != null)
        {
            points.Release();
        }
        
        points = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MAX_POINTS, Marshal.SizeOf(typeof(PointStruct)));
    }
    

    void OnMacSelected(string address){
        if (visualEffect != null)
        {
            if(address == "default"){
                visualEffect.SetInt("MacIndexedId", -1);
            }
            else{
                visualEffect.SetInt("MacIndexedId", bluetoothLogger.mac_id_map[address]);
            }
        }
    }

    
    public void UpdateBuffer(){
        points.SetData(pointsCPU);        
        if (visualEffect != null)
        {
            visualEffect.SetGraphicsBuffer(pointsProperty, points);
            visualEffect.SetInt(pointCountProperty, pointCount);
            visualEffect.SetVector3(playerPositionProperty, bluetoothLogger.transform.position);
        }
    }


}