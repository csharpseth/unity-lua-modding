using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tester : MonoBehaviour
{
    public float delay = 1f;
    public float value;
    public float percent;
    public float from = -10f;
    public float to = 10f;

    float timer = 0f;
    private void Update()
    {
        timer += Time.deltaTime;
        percent = timer / delay;
        if (percent >= 1f) { timer = 0f; }

        value = CustomTween.ping_pong(from, to, percent);

        Vector3 pos = transform.position;
        pos.x = value;
        transform.position = pos;
    }
}
