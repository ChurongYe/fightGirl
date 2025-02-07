using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyControl : MonoBehaviour
{
    public int health;
    public float damageCooldown;
    public PlayerHealth Playerhealth;
    public GameObject Player;
    private float lastDamageTime;
    private void Awake()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        Playerhealth = Player.gameObject.GetComponent<PlayerHealth>();
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

    protected virtual void Die()
    {
        Destroy(gameObject);
    }

    public abstract void Attack();

}

