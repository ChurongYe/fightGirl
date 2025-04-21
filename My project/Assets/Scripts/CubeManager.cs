using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeManager : MonoBehaviour
{
    [Header("按顺序拖入 Cube")]
    public List<GameObject> cubes = new List<GameObject>();

    public float delayBetweenCubes = 0.5f; // 每个 Cube 出现的时间间隔

    void Start()
    {
        // 全部隐藏
        foreach (GameObject cube in cubes)
        {
            if (cube != null)
                cube.SetActive(false);
        }

        // 开始依次显示
        StartCoroutine(ShowCubesOneByOne());
    }

    IEnumerator ShowCubesOneByOne()
    {
        foreach (GameObject cube in cubes)
        {
            if (cube != null)
            {
                cube.SetActive(true);
                yield return new WaitForSeconds(delayBetweenCubes);
            }
        }
    }
}