using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeEnemy : EnemyControl
{ 
    [Header("Move")]
    public float jumpForce = 6f; 
    public float jumpInterval = 3f; 
    public float direction = 1f; 
    private bool isGrounded = false; 
    private float jumpTimer = 0f;
    private void Start()
    {
        damageCooldown = 0.5f;
    }

    public override void HandleCollision(Collider collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            direction *= -1;
            StartCoroutine(DelayedTurn());
        }
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Playerhealth != null)
            {
                Playerhealth.TakeDamage(attackDamage);
            }
        }
        else if (collision.gameObject.CompareTag("Hitarea"))
        {
            if (health > 0)
            {
                TakeDamage(Playerhealth.attackDamage);
            }
        }
    }
    void Jump()
    {
        rb.AddForce(new Vector3(direction, 1,0) * jumpForce, ForceMode.Impulse);
        isGrounded = false; 
    }
    IEnumerator DelayedTurn()
    {
        yield return new WaitForSeconds(1f);

        transform.localScale = new Vector3(direction, transform.localScale.y, transform.localScale.z);
    }
    public override void Attack()
    {
        jumpTimer += Time.deltaTime;

        if (isGrounded && jumpTimer >= jumpInterval)
        {
            Jump();
            jumpTimer = 0f;
        }

        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, rb.velocity.z);
    }

}
