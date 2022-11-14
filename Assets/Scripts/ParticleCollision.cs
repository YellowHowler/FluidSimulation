using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleCollision : MonoBehaviour
{
    [HideInInspector]public int index;
    [HideInInspector]public float smoothingLength;
    [HideInInspector]public Vector3 contactPos;
    private Vector3 zero;

    private Collider[] hitColliders;
    private List<int> adjacents;

    void Start()
    {
        zero = new Vector3(0, 0, 0);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision col)
    {
        //if(!col.gameObject.CompareTag("Particle"))
            //contactPos = col.contacts[0].point;
    }

    private void OnCollisionExit(Collision col)
    {
        //contactPos = zero;
    }

    public List<int> Adjacents()
    {
        hitColliders = Physics.OverlapSphere(transform.position, smoothingLength);
        adjacents = new List<int>();

        foreach(Collider col in hitColliders)
        {
            if(col == gameObject.GetComponent<Collider>() || !col.gameObject.CompareTag("Particle")) continue; 
            adjacents.Add(int.Parse(col.gameObject.name));
        }

        //print(adjacents.Count);
        return adjacents;
    }
}
