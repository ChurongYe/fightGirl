using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FallingMonsterSpawner : MonoBehaviour
{
    public GameObject monsterPrefab;  
    public GameObject warningPrefab;  
    public float spawnHeight = 10f;   
    public float warningTime = 1.5f;  
    public float spawnRange;
    public LayerMask Ground;
    public float attackInterval;
    public int shotCount = 5;
    public List<Vector3> rangePositions = new List<Vector3>();
    public List<Vector3> groundPositions = new List<Vector3>();
    private float Waittime;
    void Start()
    {
        InvokeRepeating(nameof(StartFiring), 0f, attackInterval);
    }
    private void Update()
    {
        Waittime = Random.Range(0.2f,0.5f);
    }
    void StartFiring()
    {
        SpawnMonsterWithWarning();
    }
    void SpawnMonsterWithWarning()
    {
        for (int i = 0; i < shotCount; i++)
        {
            Vector3 RangePos;
            bool isValidPosition;

            do
            {
                RangePos = transform.position + new Vector3(
                    RandomGaussian.Range(-spawnRange, spawnRange),
                    0f,
                    RandomGaussian.Range(-spawnRange, spawnRange)
                );

                isValidPosition = true; 

                foreach (var pos in rangePositions)
                {
                    if (Vector3.Distance(RangePos, pos) < 3f)
                    {
                        isValidPosition = false; 
                        break; 
                    }
                }
            } while (!isValidPosition);

            rangePositions.Add(RangePos); 
        }
        CheckGround();
    }
    public static class RandomGaussian
    {
        // Box-Muller
        public static float Range(float min, float max)
        {
            float u1 = Random.value;
            float u2 = Random.value;

            float standardGaussian = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Cos(2.0f * Mathf.PI * u2);

            float mean = (min + max) / 2f;
            float stdDev = (max - min) / 6f;

            return Mathf.Clamp(mean + standardGaussian * stdDev, min, max);
        }
    }
    void CheckGround()
    {
        groundPositions.Clear();
        foreach (Vector3 RangePos in rangePositions)
        {
            float checkDistance = 100f;
            RaycastHit hit;
            Vector3 groundPos = Vector3.zero;
            if (Physics.Raycast(RangePos, Vector3.down, out hit, checkDistance, Ground))
            {
                groundPos = hit.point;
                groundPositions.Add(groundPos);
            }
            Debug.DrawRay(groundPos, Vector3.down * checkDistance, Color.red, 1f);
        }
        StartCoroutine(SpawnWarningsWithDelay());

    }
    IEnumerator SpawnWarningsWithDelay()
    {
        foreach (Vector3 groundPos in groundPositions)
        {
            Vector3 up = new Vector3(0, 0.1f, 0);

            yield return new WaitForSeconds(Waittime);

            GameObject warning =  Instantiate(warningPrefab, groundPos + up, Quaternion.identity);

            StartCoroutine(SpawnMonsterAfterDelay(groundPos, warning));
        }
    }
    IEnumerator SpawnMonsterAfterDelay(Vector3 groundPos, GameObject warning)
    {
        yield return new WaitForSeconds(warningTime);

        Vector3 spawnPos = groundPos + Vector3.up * spawnHeight;
        GameObject monster = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);

        Rigidbody rb = monster.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = monster.AddComponent<Rigidbody>();
        }
        Destroy(warning, 1.5f);
        rangePositions.Clear();
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 center = transform.position;

        Gizmos.DrawWireSphere(center, spawnRange );

    }
}
