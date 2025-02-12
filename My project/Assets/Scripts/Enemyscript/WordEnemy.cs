using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WordEnemy : EnemyControl
{
    [Header("Attack")]
    public float moveSpeed = 5f;
    public float followDistance = 20f;

    public override void Attack()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, Player.transform.position);


        if (distanceToPlayer < followDistance)
        {
            Vector3 direction = (Player.transform.position - transform.position).normalized;

            transform.position += direction * moveSpeed * Time.deltaTime;
        }
        Destroy(this.gameObject, 6f);
    }

}
