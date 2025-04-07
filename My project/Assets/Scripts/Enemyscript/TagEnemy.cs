using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TagEnemy : MonoBehaviour
{
    public float attackInterval = 4f;
    public GameObject[] Hiterea;
    public void Start()
    {
        UpdateEnemyList();
        foreach (GameObject hiterea in Hiterea)
        {
            hiterea.SetActive(false);
            InvokeRepeating(nameof(StartFiring), 0f, attackInterval);
        }
    }
    void StartFiring()
    {
        StartCoroutine(FireAttack());
    }
    void UpdateEnemyList()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        Hiterea = allObjects.Where(obj => obj.name == "TagEnemy").ToArray();
    }

    IEnumerator FireAttack()
    {
        foreach (GameObject hiterea in Hiterea)
        {
            hiterea.GetComponent<tag>().animator.SetTrigger("tag");
            yield return new WaitForSeconds(0.3f);
            hiterea.SetActive(true);
            yield return new WaitForSeconds(1f);
            hiterea.SetActive(false);
        }

    }
}
