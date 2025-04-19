using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSummoner : EnemyControl
{
    private int currentHealth;

    public bool isInvincible = true;
    private bool isSummoning = false;

    [System.Serializable]
    public class Wave
    {
        public List<GameObject> subWaves;        // 每一波内的多批次小怪
        public List<float> subWaveDurations;     // 每一批次持续时间（单位：秒）
    }

    public List<Wave> monsterWaves;
    private int currentWaveIndex = 0;

    void Start()
    {
        currentHealth = health;
        StartCoroutine(SummonWaveSequence(2));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(1);
        }
    }
    public override void Attack()
    {

    }
    public override void TakeDamage(int damage)
    {
        if (isInvincible || isSummoning || currentHealth <= 0)
            return;

        currentHealth -= damage;

        if (currentHealth > 0)
        {
            StartCoroutine(SummonWaveSequence(2));
        }
        else
        {
            ClearCurrentWave();
            Die();
        }
    }

    IEnumerator SummonWaveSequence(int waitFrames)
    {
        isSummoning = true;
        isInvincible = true;
        Debug.Log("现在不可以攻击");
        // 等待若干帧
        for (int i = 0; i < waitFrames; i++)
            yield return null;

        // 清除上一波
        ClearCurrentWave();

        if (currentWaveIndex < monsterWaves.Count)
        {
            Wave currentWave = monsterWaves[currentWaveIndex];

            for (int i = 0; i < currentWave.subWaves.Count; i++)
            {
                // 激活当前子批次
                if (currentWave.subWaves[i] != null)
                    currentWave.subWaves[i].SetActive(true);

                float duration = (i < currentWave.subWaveDurations.Count) ? currentWave.subWaveDurations[i] : 1f;
                yield return new WaitForSeconds(duration);

                // 隐藏当前子批次
                if (currentWave.subWaves[i] != null)
                    currentWave.subWaves[i].SetActive(false);
            }

            currentWaveIndex++;
        }

        // 小怪分批播放完成后，解除无敌
        isInvincible = false;
        Debug.Log("现在可以攻击");
        isSummoning = false;
    }

    void ClearCurrentWave()
    {
        if (currentWaveIndex > 0 && currentWaveIndex - 1 < monsterWaves.Count)
        {
            Wave prevWave = monsterWaves[currentWaveIndex - 1];
            foreach (var sub in prevWave.subWaves)
            {
                if (sub != null)
                    sub.SetActive(false);
            }
        }
    }
}