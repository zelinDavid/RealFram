using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SVTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        string test;
        Dictionary<string,string> dict = new Dictionary<string, string>();
        dict.TryGetValue("pp", out test);
        bool ret = test == null;
        Debug.LogWarning("dddd:" + (ret) );
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
