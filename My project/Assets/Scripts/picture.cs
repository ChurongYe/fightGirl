using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class picture : EnemyControl
{
    public override void Attack()
    {

    }

    public override void HandleCollision(Collider collision)//
    {
        //base below from parent
        if (collision.gameObject.CompareTag("Player"))
        {

            if (Playerhealth != null)
            {
                Playerhealth.TakeDamage(attackDamage);
            }
        }
    }
}
