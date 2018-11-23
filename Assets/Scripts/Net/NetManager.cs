using NetData;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetManager : MonoBehaviour {

    bool bInit = false;
    void Awake()
    {
        if(!bInit)
        {
            bInit = true;
            NetCore.Init();
        }
    }

    // Update is called once per frame
    void Update ()
    {
        NetCore.Dispatch();
    }
}
