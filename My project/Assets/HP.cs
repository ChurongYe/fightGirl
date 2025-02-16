using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HP : MonoBehaviour
{
    public PlayerHealth Health;
    public float CurrentHealth;
    public Slider HPSlider;
    void Awake()
    {
        CurrentHealth = Health.health;
    }

    void Update()
    {
        HPSlider.value = Health.health / CurrentHealth;

    }
}
