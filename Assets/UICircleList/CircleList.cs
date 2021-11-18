using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CircleList : MonoBehaviour
{
    public float radius = 3.0f;
    public float rotate;
    public float offset = 0f;
    public GameObject root;
    public GameObject template;
    public Sprite[] sprites;
    private float roll = 0f;
    private GameObject[] images;

    private Vector3 pos_in_circle(float radius, float degree)
    {
        float x = radius * Mathf.Sin(degree);
        float y = radius * Mathf.Cos(degree);
        Vector3 pos = Quaternion.AngleAxis(rotate, Vector3.right) * new Vector3(x, y, 0);
        return new Vector3(pos.x, pos.y, 0);
    }

    public void SyncPos()
    {
        float degree = 360f / images.Length;
        for (int i = 0; i < images.Length; i++) {
            float scale = 1f;
            var go = images[i];
            var angle = (degree * i + roll) % 360f;
            if (angle <= 90) 
                scale = Mathf.Lerp(1f, 2f, angle / 90f);
            else if (angle >= 270)
                scale = Mathf.Lerp(2f, 1f, Mathf.Abs(270f - angle) / 90f);
            else
                scale = Mathf.Lerp(3f, 2f, Mathf.Abs(angle - 180f) / 90f);
            go.transform.localPosition = pos_in_circle(radius, angle/360f * 2 * Mathf.PI);
            go.transform.localScale = new Vector3(scale, scale, scale);
        }
    }

    public void Awake()
    {
        images = new GameObject[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) {
            GameObject go = Instantiate(template, root.transform);
            images[i] = go;
            go.GetComponent<Image>().sprite = sprites[i];            
        }
        SyncPos();
    }
    // Update is called once per frame
    void Update()
    {
        roll += 10f * Time.deltaTime;
        SyncPos();
     }
}
