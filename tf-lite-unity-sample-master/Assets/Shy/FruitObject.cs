using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitObject : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Sprite[] fruitSprites;
    Rigidbody2D rbd;
    public Sprite explosionSprite;

    void Awake()
    {
        spriteRenderer.sprite = fruitSprites[Random.Range(0, fruitSprites.Length)];

        rbd = GetComponent<Rigidbody2D>();
        rbd.AddRelativeForce(Vector2.up * Random.Range(450f, 650f));
    }

    public void DestroyObject() {
        spriteRenderer.sprite = explosionSprite;
        Destroy(gameObject.GetComponent<Rigidbody2D>());
        StartCoroutine("Explosion");
    }

     IEnumerator Explosion()
    {
        while (spriteRenderer.color.a >= 0)
        {
            spriteRenderer.color -= new Color(0, 0, 0, .05f);

            yield return null;
        }
        Destroy(gameObject);
    }
}
