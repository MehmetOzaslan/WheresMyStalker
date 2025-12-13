using UnityEngine;
using System.Collections.Generic;
using System;   
using System.Collections;
using System.Diagnostics.Tracing;


// Note this isn't actually a pool it's just a way of batching item construction.
public class BatchedPool : MonoBehaviour {
    public List<GameObject> pool = new List<GameObject>();
    private int poolIndex = 0;
    public GameObject prefab;
    public int poolInitSize = 1000;
    public float poolSizeMultiplier = 1.5f;

    public void SetPrefab(GameObject prefab){
        this.prefab = prefab;
    }

    public void Init(){
        for (int i = 0; i < poolInitSize; i++){
            AddObj();
        }
    }

    private void AddObj(){
        GameObject obj = Instantiate(prefab);
        obj.SetActive(false);
        pool.Add(obj);
    }

    public GameObject GetNewObject(){
        if(poolIndex > poolInitSize){
            int items_to_add = (int) ((float) pool.Count * poolSizeMultiplier);
            for(int i = 0; i < items_to_add; i++){
                AddObj();
            }
        }

        GameObject obj = pool[poolIndex];
        poolIndex++;

        obj.SetActive(true);
        return obj;
    }

    public void DeactivatePool(){     
        foreach(var poolObj in pool){
            poolObj.SetActive(false);
        }
    }

    public void DestroyPool(){
        foreach(var poolObj in pool){
            Destroy(poolObj);
        }
    }
}