using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : EnemyControl
{
    public GameObject Hitarea;
    public float bounceForce = 5f;
    public bool isInvincible = false;
    //
    public Slider HP;
    //
    private void Start()
    {
        damageCooldown = 2f;
    }
    public override void Attack()
    {
        
    }
    public override void TakeDamage(int damage)
    {
        if (isInvincible) 
        {
            return;
        }

        base.TakeDamage(damage);

    }
    public override void HandleCollision(Collider collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Vector3 collisionDirection = (transform.position - collision.transform.position).normalized;

            rb.AddForce(collisionDirection * bounceForce, ForceMode.Impulse);
        }
        //
        //if (collision.gameObject.CompareTag("PickUp"))
          //  isInvincible = true;
        //
    }


}
