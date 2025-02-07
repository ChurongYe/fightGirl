using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : EnemyControl
{
    public int attackDamage = 1;
    public GameObject Hitarea;
    public float bounceForce = 5f;
    private Rigidbody rb;
    private void Start()
    {
        damageCooldown = 2f;
        rb = GetComponent<Rigidbody>();
    }
    public override void Attack()
    {
        
    }
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

    }
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Vector3 collisionDirection = (transform.position - collision.transform.position).normalized;

            rb.AddForce(collisionDirection * bounceForce, ForceMode.Impulse);
        }
    }

}
