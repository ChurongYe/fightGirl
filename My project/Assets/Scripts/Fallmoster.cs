using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fallmoster : EnemyControl
{
    [Header("Attack")]
    public int attackDamage = 1;
    private Rigidbody rb;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    public override void Attack()
    {

    }
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Playerhealth != null)
            {
                Playerhealth.TakeDamage(attackDamage);
            }
        }
        if (collision.gameObject.CompareTag("Hitarea"))
        {
            if (health > 0)
            {
                TakeDamage(Playerhealth.attackDamage);
            }
        }
        else
        {
            this.transform.GetComponent<Collider>().enabled = false;
            Destroy(this.gameObject, 2f);
        }
    }
}
