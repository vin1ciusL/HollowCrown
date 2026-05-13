using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchObject : MonoBehaviour
{
    public GameObject objectToSwitch1, objectToSwitch2;

    private void Start()
    {
        objectToSwitch1.SetActive(true);
        objectToSwitch2.SetActive(false);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            objectToSwitch1.SetActive(!objectToSwitch1.activeSelf);
            objectToSwitch2.SetActive(!objectToSwitch2.activeSelf);
        }
    }
}
