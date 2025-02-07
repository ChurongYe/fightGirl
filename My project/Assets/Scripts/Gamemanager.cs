using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gamemanager : MonoBehaviour
{
    public List<EnemyControl> enemies = new List<EnemyControl>();
    private void Update()
    {
        EnemyControl[] foundEnemies = FindObjectsOfType<EnemyControl>();
        enemies.RemoveAll(enemy => enemy == null);

        foreach (EnemyControl enemy in foundEnemies)
        {
            if (!enemy.gameObject.CompareTag("Player") && !enemies.Contains(enemy))
            {
                enemies.Add(enemy);
            }
        }

        foreach (EnemyControl enemy in enemies)
        {
            if (enemy != null)  
            {
                enemy.Attack();
            }
        }
    }
}
