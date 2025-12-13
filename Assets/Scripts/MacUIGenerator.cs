using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System;   
using System.Collections;
using System.Diagnostics.Tracing;
using UnityEngine.Rendering.Universal;
using Unity.Tutorials.Core.Editor;



// TODO: I have a feeling this is where a lot of performance issues are coming from.
// Maybe use some pooling for the prefabs. 
public class MacUIGenerator : MonoBehaviour
{

    [Header("Bluetooth Scanner")]
    public BluetoothLEScanner scanner; 

// TODO Pooling
    // public BatchedPool batchedPool;

    public RawDataParser rawDataParser;

    public GameObject buttonPrefab;

    public GameObject buttonContainer;

    public BluetoothLogger bluetoothLogger;

    public HashSet<string> deviceAddresses = new HashSet<string>();
    
    private Button currentlySelectedButton;

    private Dictionary<string, Button> deviceButtons = new Dictionary<string, Button>();
    private Dictionary<string, Slider> deviceSliders = new Dictionary<string, Slider>();
    
    [Header("Selection Settings")]
    public Color selectedColor = Color.magenta;
    public Color normalColor = Color.black;
    
    [Header("Global View Button")]
    public string defaultButtonText = "Global View";


    public float sliderUpdateInterval = 0.2f;
    public int sliderSampleSize = 5;

    void UpdateSlider(string address){
        Slider slider = deviceSliders[address];
        if (slider != null)
        {
            float percentRSSI = bluetoothLogger.PercentRSSI(bluetoothLogger.GetAverageSpecificRSSISampled(address, sliderSampleSize));
            slider.value = percentRSSI;
            // Debug.Log($"MacUIGenerator: UpdateSlider {address} {percentRSSI}");
        }
    }

    private IEnumerator UpdateSlidersCoroutine()
    {
        while (true)
        {
            foreach (string address in deviceAddresses)
            {
                UpdateSlider(address);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }
    
    
    public void OnDeviceUpdate(string address, string name, int rssi, int txPower, bool isConnectable, string rawData){
        if (!deviceAddresses.Contains(address)){
            deviceAddresses.Add(address);
            GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer.transform);
            Slider slider = buttonObj.GetComponentInChildren<Slider>();
            if (slider != null)
            {
                slider.value = 0;
                deviceSliders[address] = slider;
            }


            MacUIBinder binder = buttonObj.GetComponent<MacUIBinder>();

            
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                binder.button = button;
                button.onClick.AddListener(() => OnButtonClicked(address, button));
                deviceButtons[address] = button;
                SetButtonColor(button, normalColor);
            }
            
            RawDataParser.CompanyData data = rawDataParser.GetCompanyData(rawData);
            string findMyTag = "";
            string companyName = "";
            
            try
            {
                findMyTag = rawDataParser.GetFindMyDevice(data);
                companyName = rawDataParser.GetCompanyName(data) + " " + findMyTag;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning("Error getting companyName/findMyTag: " + ex.Message);
                companyName = "N/A";
            }

            if(name.IsNullOrEmpty()){
                name = "N/A";
            }
            if(companyName.IsNullOrEmpty()){
                companyName = "N/A";
            }
            if(address.IsNullOrEmpty()){
                address = "N/A";
            }

            binder.address.text = address;
            binder.deviceName.text = name;
            binder.company.text = companyName;
        }                
    }
    
    private void OnButtonClicked(string address, Button clickedButton)
    {
        if (currentlySelectedButton != null && currentlySelectedButton != clickedButton)
        {
            SetButtonColor(currentlySelectedButton, normalColor);
        }
        
        currentlySelectedButton = clickedButton;
        SetButtonColor(clickedButton, selectedColor);
        
        // Debug.Log($"Selected device: {address}");

        bluetoothLogger.OnMACSelected(address);
    }
    
    private void SetButtonColor(Button button, Color color)
    {
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = color;
        }
    }

    void Start()
    {
        rawDataParser = gameObject.AddComponent<RawDataParser>();

        CreateDefaultButton();

        StartCoroutine(UpdateSlidersCoroutine());

        if (scanner == null)
        {
            scanner = FindFirstObjectByType<BluetoothLEScanner>();
        }
        if (scanner != null)
        {
            scanner.OnDeviceFoundEvent += OnDeviceUpdate;
            // Debug.Log("BluetoothLogger: Connected to BluetoothLEScanner");
        }
    }
    
    private void CreateDefaultButton()
    {
        GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer.transform);
        buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = defaultButtonText;
        
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnButtonClicked("default", button));
            deviceButtons["default"] = button;
            currentlySelectedButton = button;
            SetButtonColor(button, selectedColor);
        }
    }
}
