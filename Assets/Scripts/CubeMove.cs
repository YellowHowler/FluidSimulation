using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeMove : MonoBehaviour
{
    private Vector3 pos1 = new Vector3(-5,-3f,-5.5f);
    private Vector3 pos2 = new Vector3(13,-3,-5.5f);
    public float speed = 0.05f;
 
    void Update() 
    {
         transform.position = Vector3.Lerp (pos1, pos2, Mathf.PingPong(Time.time*speed, 1.0f));
    }
}
