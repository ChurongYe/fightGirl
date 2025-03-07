using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigTest : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.gameObject.GetComponent<Rigidbody>().AddForce(Vector3.right*2f,ForceMode.Impulse);
            Debug.Log("ok");
        }
    }
}
