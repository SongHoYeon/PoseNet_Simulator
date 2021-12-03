using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    [SerializeField]
    private Transform[] spawnPoints;
    [SerializeField]
    private GameObject fruit;
    public bool isPause = false;
    private int fruitCount = 0;

    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("SpawnFruit", 3f, 1f);
    }

    public void SetFindUser(bool flag)
    {
        if (flag)
        {
            isPause = false;
        }

        else
        {
            isPause = true;
        }

    }
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.tag == "Fruit")
        {
            Destroy(col.gameObject);
        }
    }

    public int GetFruitCount()
    {
        return fruitCount;
    }

    public void StopFruitSpawn()
    {
        CancelInvoke();
    }

    private void SpawnFruit()
    {
        fruitCount++;
        GameObject fruitRig = Instantiate(fruit, spawnPoints[Random.Range(0, spawnPoints.Length)].position, Quaternion.Euler(new Vector3(0, 0, Random.Range(-20f, 20f)))) as GameObject;
    }

}
