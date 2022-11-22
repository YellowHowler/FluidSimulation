using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirConditioning : MonoBehaviour
{
    [SerializeField] private Vector3 direction;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision other) 
    {
        if(other.gameObject.tag == "Particle") 
        {
            other.gameObject.GetComponent<Rigidbody>().AddForce(direction * 3, ForceMode.VelocityChange);
            print("cld");
        }
    }
}
