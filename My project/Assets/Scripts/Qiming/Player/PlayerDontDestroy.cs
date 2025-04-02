using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDontDestroy : MonoBehaviour
{
    private static PlayerDontDestroy Instance;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }
}
