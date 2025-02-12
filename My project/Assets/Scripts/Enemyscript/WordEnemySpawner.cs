using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class WordEnemySpawner : MonoBehaviour
{
    public GameObject wordPrefab;
    public float attackInterval = 3f;
    public float Distance = 80f;  
    public LayerMask Wordenemy;
    private bool Ifhit = false;
    private void Update()
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z > 0 && screenPos.x >= 0 && screenPos.x <= Screen.width && screenPos.y >= 0 && screenPos.y <= Screen.height)
        {
            Ifhit = true;  
        }
        else
        {
            Ifhit = false;  
        }
    }
    public void Start()
    {
        InvokeRepeating(nameof(StartFiring), 0f, attackInterval);
    }

    void StartFiring()
    {
        if (Ifhit)
        {
            StartCoroutine(FireAttack());
        }
    }

    IEnumerator FireAttack()
    {
        Vector3 direction = Random.onUnitSphere;
        Vector3 spawnPosition = transform.position + transform.forward;
        Instantiate(wordPrefab, spawnPosition, Quaternion.LookRotation(direction));
        yield return null;

    }

}
