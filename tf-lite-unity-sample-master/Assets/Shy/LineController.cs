using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineController : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.tag == "Fruit")
        {
            if (gameObject.GetComponent<LineRenderer>().positionCount < 2)
                return;
            col.GetComponent<FruitObject>().DestroyObject();
            GetComponentInParent<PoseNetSample>().score++;
        }
    }
}
