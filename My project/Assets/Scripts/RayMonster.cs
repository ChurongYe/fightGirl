using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayMonster : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 3f;      
    public float rotateSpeed = 180f;  
    public float moveRange = 3f;    
    public float waitTime = 5f;       
    public float rotationSpeed = 2f;  
    public float rotationAngle = 45f;  

    private float time;
    private Vector3 startPos;        
    private Vector3 targetPos;        
    private bool isMoving = false;    

    void Start()
    {
        startPos = transform.position;
        StartCoroutine(Wander());
    }
    IEnumerator Wander()
    {
        while (true)
        {
            targetPos = startPos + new Vector3(
                Random.Range(-moveRange, moveRange),
                0f,
                Random.Range(-moveRange, moveRange)
            );

            yield return StartCoroutine(RotateTowards(targetPos));

            yield return StartCoroutine(MoveTo(targetPos));

            yield return StartCoroutine(Rotate());


        }
    }

    IEnumerator RotateTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, rotateSpeed * Time.deltaTime
            );
            yield return null;
        }
    }

    IEnumerator MoveTo(Vector3 target)
    {
        isMoving = true;
        while (Vector3.Distance(transform.position, target) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target, moveSpeed * Time.deltaTime
            );
            yield return null;
        }
        isMoving = false;
    }
    IEnumerator Rotate()
    {
        float initialRotationY = transform.eulerAngles.y;
        float elapsedTime = 0f;  
        float duration = 7f;
        time = 0f;
        while (elapsedTime < duration) 
        {
            elapsedTime += Time.deltaTime; 

            time += Time.deltaTime * rotationSpeed;
            float yRotation = Mathf.Sin(time) * rotationAngle;

            transform.rotation = Quaternion.Euler(0, initialRotationY + yRotation, 0);

            yield return null;  
        }
    }
}
