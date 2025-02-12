using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedEnemy : MonoBehaviour
{
    public GameObject attackPrefab;
    public float attackSpeed = 10f;
    public float attackInterval = 3f;
    public float attackAngle = 30f;
    public int shotCount = 3;
    public float bulletDelay = 0.2f;
    float offset = 0.5f; 
    public void Start()
    {
        InvokeRepeating(nameof(StartFiring), 0f, attackInterval);
    }

    void StartFiring()
    {
        StartCoroutine(FireAttack());
    }

    IEnumerator FireAttack()
    {
        Vector3 forward = transform.forward;
        Vector3 leftDir = Quaternion.Euler(0, -attackAngle, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, attackAngle, 0) * forward;

        for (int i = 0; i < shotCount; i++)
        {
            SpawnProject(transform.position, forward);
            SpawnProject(transform.position, leftDir);
            SpawnProject(transform.position, rightDir);
            yield return new WaitForSeconds(bulletDelay);
        }
    }
    void SpawnProject(Vector3 position, Vector3 direction)
    {
        GameObject project = Instantiate(attackPrefab, position, Quaternion.LookRotation(direction));
        Rigidbody rb = project.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = direction * attackSpeed;
        }
    }
}
