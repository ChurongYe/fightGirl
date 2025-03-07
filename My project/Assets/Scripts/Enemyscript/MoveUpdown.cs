using GLTFast.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveUpdown : MonoBehaviour
{
    public float amplitude = 1f;
    [SerializeField] private float speed;
    private Vector3 startPos;

    void Start()
    {
        speed = UnityEngine.Random.Range(1f,3f);
        startPos = transform.position;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * amplitude;
        transform.position = startPos + new Vector3(0, offset, 0);
    }
}
