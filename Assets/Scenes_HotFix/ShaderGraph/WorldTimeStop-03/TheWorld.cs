using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheWorld : MonoBehaviour
{

    public Material material;
    public float timeStop = 0;
    public float _timeStopCurrrent = 0;
    // Start is called before the first frame update
    void Start()
    {
        material = GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            timeStop = 3;
        }
        if (Input.GetMouseButtonDown(1))
        {
            timeStop = 0;
        }
        _timeStopCurrrent += (timeStop - _timeStopCurrrent) * Time.deltaTime * 0.5f;
        material.SetFloat("_Scale", _timeStopCurrrent);
    }
}
