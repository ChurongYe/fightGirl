using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyControl : MonoBehaviour
{
    public Rigidbody rb;
    public int attackDamage = 1;
    public int health;
    public float damageCooldown;
    public PlayerHealth Playerhealth;
    public GameObject Player;
    private float lastDamageTime;
    private void Awake()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        Playerhealth = Player.gameObject.GetComponent<PlayerHealth>();
        rb = GetComponent<Rigidbody>();
    }
    public virtual void TakeDamage(int damage)
    {
        if (Time.time >= lastDamageTime + damageCooldown)
        {
            health -= damage;
            lastDamageTime = Time.time; 
            Debug.Log(gameObject.name + " takes " + damage + " damage! HP: " + health);

            if (health <= 0)
            {
                Die();
            }
        }
    }
    public void OnTriggerEnter(Collider collision)
    {
        HandleCollision(collision);
    }
    public virtual void HandleCollision(Collider collision)
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
        if(collision.gameObject.CompareTag("Ground"))
        {
            this.transform.GetComponent<Collider>().enabled = false;
        }
        else
        {
            Destroy(this.gameObject, 3f);
        }
    }
    protected virtual void Die()
    {
        Destroy(gameObject);
    }

    public abstract void Attack();

}

