using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireTest : MonoBehaviour
{
    [SerializeField] Vector3[] fireSource;
    [SerializeField] float timeInterval;
    [SerializeField] GameObject particle;
    [SerializeField] Gradient particleColor;

    void Start()
    {
        StartCoroutine(RunSim());
    }

    void Update()
    {
        
    }

    private IEnumerator RunSim()
    {
        WaitForSeconds sec = new WaitForSeconds(timeInterval);

        while(true)
        {
            for(int i = 0; i < fireSource.Length; i++)
            {
                for(int j = 0; j < 7; j++)
                    Instantiate(particle, transform.position + Random.insideUnitSphere * 0.9f, Quaternion.identity).GetComponent<FireParticle>().grad = particleColor;
            }

            yield return sec;
        }
    }
}
