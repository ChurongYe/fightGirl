using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PickUpCount : MonoBehaviour
{
    public Slider PickUpSlider;
    public float MaxCount = 10f;
    public float CurrentCount;
    void Awake()
    {
        MaxCount = 10f;
        CurrentCount = 0;
    }
    void Update()
    {
        PickUpSlider.value = CurrentCount / MaxCount;
    }
    public void ChangeCount(float Count)
    {
        CurrentCount += Count;
    }
}
