using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System;   
using System.Collections;
public class MacUIGenerator : MonoBehaviour
{

    [Header("Bluetooth Scanner")]
    public BluetoothLEScanner scanner; 

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
            buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = address;
            Slider slider = buttonObj.GetComponentInChildren<Slider>();
            if (slider != null)
            {
                slider.value = 0;
                deviceSliders[address] = slider;
            }

            
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnButtonClicked(address, button));
                deviceButtons[address] = button;
                SetButtonColor(button, normalColor);
            }
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
