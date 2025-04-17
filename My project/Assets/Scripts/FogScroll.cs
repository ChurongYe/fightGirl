using UnityEngine;

public class FogScroll : MonoBehaviour
{
    public float scrollSpeedX = 0.01f;
    public float scrollSpeedY = 0.005f;
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        float x = Time.time * scrollSpeedX;
        float y = Time.time * scrollSpeedY;
        rend.material.mainTextureOffset = new Vector2(x, y);
    }
}
