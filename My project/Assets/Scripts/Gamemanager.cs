using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gamemanager : MonoBehaviour
{
    public List<EnemyControl> enemiesscripts = new List<EnemyControl>();

    private void Update()
    {
        EnemyControl[] foundEnemies = FindObjectsOfType<EnemyControl>();
        enemiesscripts.RemoveAll(enemy => enemy == null);

        foreach (EnemyControl enemy in foundEnemies)
        {
            if (!enemy.gameObject.CompareTag("Player") && !enemiesscripts.Contains(enemy))
            {
                enemiesscripts.Add(enemy);
            }
        }

        foreach (EnemyControl enemy in enemiesscripts)
        {
            if (enemy != null)  
            {
                enemy.Attack();
            }
        }
    
    }
}
