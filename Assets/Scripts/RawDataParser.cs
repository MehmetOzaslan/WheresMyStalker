using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class RawDataParser : MonoBehaviour
{
    public int address = 0;
    public int sanitizedName = 1;
    public int rssi = 2;
    public int txPower = 3;
    public int isConnectable = 4;
    public int deviceType = 5;
    public int primaryPhy = 6;
    public int secondaryPhy = 7;
    public int advertisingSetId = 8;
    public int periodicInterval = 9;
    public int manufacturerData = 10;
    public int serviceUuids = 11;
    public int serviceData = 12;
    public int advertisingFlags = 13;
    public int rawBytes = 14;
    public int timestamp = 15;


    public struct CompanyData{
        public int id;
        public string base_64_decoded;
    }


    public Dictionary<int, string> company_id_names = new Dictionary<int, string>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        string filePath = Application.dataPath + "/OfflineAnalysis/data/company_id_easyparse.csv";
        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] parts = line.Split(new char[] { ',' }, 2);
                if (parts.Length == 2)
                {
                    int key;
                    if (int.TryParse(parts[0].Trim(), out key))
                    {
                        string name = parts[1].Trim();
                        company_id_names[key] = name;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("CSV file not found: " + filePath);
        }
    }


    public CompanyData GetCompanyData(string rawData){

        // Split the input string using the '|' delimiter
        string[] fields = rawData.Split('|');
        
        // Set variables corresponding to the fields, using the indices defined above
        // string addressVal = fields.Length > address ? fields[address] : "";
        // string sanitizedNameVal = fields.Length > sanitizedName ? fields[sanitizedName] : "";
        // string rssiVal = fields.Length > rssi ? fields[rssi] : "";
        // string txPowerVal = fields.Length > txPower ? fields[txPower] : "";
        // string isConnectableVal = fields.Length > isConnectable ? fields[isConnectable] : "";
        // string deviceTypeVal = fields.Length > deviceType ? fields[deviceType] : "";
        // string primaryPhyVal = fields.Length > primaryPhy ? fields[primaryPhy] : "";
        // string secondaryPhyVal = fields.Length > secondaryPhy ? fields[secondaryPhy] : "";
        // string advertisingSetIdVal = fields.Length > advertisingSetId ? fields[advertisingSetId] : "";
        // string periodicIntervalVal = fields.Length > periodicInterval ? fields[periodicInterval] : "";
        // string serviceUuidsVal = fields.Length > serviceUuids ? fields[serviceUuids] : "";
        // string serviceDataVal = fields.Length > serviceData ? fields[serviceData] : "";
        // string advertisingFlagsVal = fields.Length > advertisingFlags ? fields[advertisingFlags] : "";
        // string rawBytesVal = fields.Length > rawBytes ? fields[rawBytes] : "";
        // string timestampVal = fields.Length > timestamp ? fields[timestamp] : "";

        string manufacturerDataVal = fields.Length > manufacturerData ? fields[manufacturerData] : "";

        string[] parts = manufacturerDataVal.Split(':');
        int id = 0;
        string base_64_decoded = "";
        if (parts.Length == 2)
        {
            int.TryParse(parts[0], out id);
            try
            {
                byte[] data = System.Convert.FromBase64String(parts[1]);
                base_64_decoded = System.Text.Encoding.UTF8.GetString(data);
            }
            catch
            {
                base_64_decoded = "";
            }
        }

        CompanyData companyData;
        companyData.id = id;
        companyData.base_64_decoded = base_64_decoded;

        return companyData;
    }


    public string GetCompanyName(CompanyData companyData){

        if (company_id_names != null && 
            companyData.id >= 0 && 
            company_id_names.ContainsKey(companyData.id)){
            return company_id_names[companyData.id];
        }
        else{
            return "";
        }
    }

    public string GetFindMyDevice(CompanyData companyData){

        if (!string.IsNullOrEmpty(companyData.base_64_decoded))
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(companyData.base_64_decoded);
            if (data.Length >= 4 && data[0] == 0x12 && data[1] == 0x19)
            {
                return "Find My";
            }
        }
        return "";
    }
}
