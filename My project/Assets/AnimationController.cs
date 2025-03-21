using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private Animator Ani;
    //public Collider Collider;
    private void Awake()
    {
        Ani = GetComponent<Animator>();
    }
    void Start()
    {
        Ani.SetBool("IsFall", true);
;    }

    //void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.CompareTag("Ground"))
    //    {
    //        Ani.SetBool("IsFall", false);
    //        Debug.Log("ok");
    //    } 
    //}
    public void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Ground"))
        {
            Ani.SetBool("IsFall",false);
            Debug.Log("ok");
        }
    }
}
