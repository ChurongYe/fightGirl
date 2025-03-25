using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayMonsterNomal : MonoBehaviour
{
    public float rotationSpeed = 2f;  
    public float rotationAngle = 50f; 

    private float time;
    private float initialYRotation; 

    void Start()
    {
        initialYRotation = transform.eulerAngles.y;
    }

    void Update()
    {
        time += Time.deltaTime * rotationSpeed;

        float yRotation = initialYRotation + Mathf.Sin(time) * rotationAngle;

        transform.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}

