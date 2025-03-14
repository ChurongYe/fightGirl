using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : EnemyControl
{
    public GameObject Hitarea;
    public float bounceForce = 5f; 
    public float knockbackDuration = 0.2f; 

    private CharacterController characterController;
    private ThirdPersonCharacter thirdPersonCharacter;
    private bool isKnockedBack = false;
    public bool isInvincible = false;
    //
    public Slider HP;
    //
    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        thirdPersonCharacter = GetComponent<ThirdPersonCharacter>();

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
            StartCoroutine(Knockback(collision));

            //rb.AddForce(collisionDirection * bounceForce, ForceMode.Impulse);
        }
        //
        //if (collision.gameObject.CompareTag("PickUp"))
          //  isInvincible = true;
        //
    }
    private IEnumerator Knockback(Collider collision)
    {
        isKnockedBack = true;
        thirdPersonCharacter.enabled = false; 

        Vector3 knockbackDirection = (transform.position - collision.transform.position).normalized;
        knockbackDirection.y = 0; 

        float timer = 0f;
        while (timer < knockbackDuration)
        {
            characterController.Move(knockbackDirection * bounceForce * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        thirdPersonCharacter.enabled = true;
        isKnockedBack = false;
    }
}
