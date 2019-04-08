using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SVTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
       List<object> temList = new List<object>();
       SVResourceObj obj = new SVResourceObj();
       string str = "2";

        temList.Add(obj);
        temList.Add(this);

         foreach (object item  in temList)
        {
            Debug.LogWarning("itemmmm: " + item.GetType());
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
