using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : EnemyControl
{
    public GameObject Hitarea;
    public float bounceForce = 5f;
    private void Start()
    {
        damageCooldown = 2f;
    }
    public override void Attack()
    {
        
    }
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

    }
    public override void HandleCollision(Collider collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Vector3 collisionDirection = (transform.position - collision.transform.position).normalized;

            rb.AddForce(collisionDirection * bounceForce, ForceMode.Impulse);
        }
    }

}
