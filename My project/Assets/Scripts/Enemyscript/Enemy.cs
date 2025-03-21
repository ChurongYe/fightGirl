using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy : EnemyControl
{
    public Animator ChickenLegAni;
    //private void Awake()//
    //{
    //    ChickenLegAni = GetComponentInChildren<Animator>();
    //    if (ChickenLegAni != null)
    //        Debug.Log("no ani");
    //}
    private void Start()//
    {
        //ChickenLegAni = GetComponentInChildren<Animator>();
        if (ChickenLegAni != null)
            ChickenLegAni.SetBool("IsFall", true);

    }
    public override void Attack()
    {
        
    }
    public override void HandleCollision(Collider collision)//
    {
        if (ChickenLegAni != null)
        {
            ChickenLegAni.SetBool("IsFall", false);//
            Debug.Log("stop ani");
        }

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
        if (collision.gameObject.CompareTag("Ground"))
        {
            this.transform.GetComponent<Collider>().enabled = false;
        }
        else
        {
            Destroy(this.gameObject, Disappeartime);
        }
    }
}
