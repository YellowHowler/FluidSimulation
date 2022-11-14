using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeMove2 : MonoBehaviour
{
    [SerializeField] Transform Init;
    
    private Vector3 initialPosition;
    private Vector3 direction;
    private Rigidbody rb;

    void Start()
    {
        initialPosition = Init.position;
        direction = new Vector3(0, 0, 10);
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator Push()
    {
        WaitForSeconds sec = new WaitForSeconds(2);

        while(true)
        {
            transform.position = initialPosition;

            
            //rb.AddForce(direction, ForceMode.Impulse);
            yield return sec;
        }
    }
}
