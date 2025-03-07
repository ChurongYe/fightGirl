using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUp : MonoBehaviour
{
    public float Count;
    public PickUpCount Bar;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Bar.ChangeCount(Count);
        }
    }
}
