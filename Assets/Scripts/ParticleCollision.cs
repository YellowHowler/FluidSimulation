using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleCollision : MonoBehaviour
{
    [HideInInspector]public int index;
    [HideInInspector]public float smoothingLength;
    //[HideInInspector]public float temperature;

    private Rigidbody rb;

    private Vector3 zero;
     private Vector3 collisionPoint;
    private Vector3 collisionNormal;

    private Collider[] hitColliders;
    private List<int> adjacents;

    void Start()
    {
        zero = new Vector3(0, 0, 0);
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider col)
    {
        //col.gameObject.GetComponent<TempDetector>().UpdateTemp(1, temperature);

        print("hi");

        //collisionPoint = col.ClosestPoint(transform.position);
        //collisionNormal = (transform.position - collisionPoint).normalized;
        //rb.velocity = 0.7f * Vector3.Reflect(rb.velocity.normalized, collisionNormal) * rb.velocity.magnitude;

    }

    private void OnTriggerExit(Collider col)
    {
        //col.gameObject.GetComponent<TempDetector>().UpdateTemp(-1, -temperature);
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
