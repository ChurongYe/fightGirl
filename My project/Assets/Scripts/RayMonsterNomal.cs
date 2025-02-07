using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayMonsterNomal : MonoBehaviour
{
    public float rotationSpeed = 2f;
    public float rotationAngle = 50f;

    private float time;

    void Update()
    {
        time += Time.deltaTime * rotationSpeed;
        float yRotation = Mathf.Sin(time) * rotationAngle;

        transform.rotation = Quaternion.Euler(0, yRotation, 0);

    }
}
