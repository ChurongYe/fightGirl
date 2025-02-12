using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TagEnemy : MonoBehaviour
{
    public float attackInterval = 4f;
    public GameObject Hiterea;
    public void Start()
    {
        Hiterea.SetActive(false);
        InvokeRepeating(nameof(StartFiring), 0f, attackInterval);
    }
    void StartFiring()
    {
        StartCoroutine(FireAttack());
    }

    IEnumerator FireAttack()
    {
        Hiterea.SetActive(true);
        yield return new WaitForSeconds(1f);
        Hiterea.SetActive(false);

    }
}
