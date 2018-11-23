using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        InitNet();
        DontDestroyOnLoad(gameObject);
    }

    private void InitNet()
    {
        gameObject.AddComponent<NetManager>();
        LoginManager.Instance.LoginConnect();
    }    
}
