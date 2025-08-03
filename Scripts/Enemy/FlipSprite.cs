using UnityEngine;

public class FlipSprite : MonoBehaviour
{
    public SpriteRenderer sprite;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sprite.flipX = true;
    }
}
