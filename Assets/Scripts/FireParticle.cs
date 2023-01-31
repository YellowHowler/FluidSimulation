using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireParticle : MonoBehaviour
{
    private float Poly6KernelConstant;
    private float Poly6GradKernelConstant;
    private float SpikyKernelConstant;
    private float SpikyGradKernelConstant;

    [HideInInspector] public Gradient grad;
    private Rigidbody rb;
    private Material mt;

    private float timeInterval;
    private float smoothingLength = 7f;
    private float lifeSpan = 1.5f;
    [HideInInspector] public float temperature = 1;
    private Vector3 initialSpeed;

    [HideInInspector] public float density = 1;
    private float baseDensity = 1;
    [HideInInspector] public float pressure;
    [HideInInspector] public Vector3 velocity;
    private Vector3 force;

    private Collider[] hitColliders;
    private List<Transform> adjacents;

    private float nextTemp;

    FireParticle o;

    void Start()
    {
        Poly6KernelConstant = (315) / (64 * Mathf.PI * Mathf.Pow(smoothingLength, 9));
        Poly6GradKernelConstant = -945 / (32 * Mathf.PI * Mathf.Pow(smoothingLength, 9));
        SpikyKernelConstant = (15) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));
        SpikyGradKernelConstant = (-45) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));

        rb = GetComponent<Rigidbody>();
        mt = GetComponent<MeshRenderer>().material;

        timeInterval = Random.Range(0.1f, 0.2f);

        temperature = Random.Range(0.95f, 1f);
        nextTemp = temperature;

        initialSpeed = new Vector3(0, 0, 0) + Random.insideUnitSphere * 0.3f;
        rb.velocity = initialSpeed;
        velocity = rb.velocity;

        GetAdjacents();

        StartCoroutine(Simulate());
    }

    private IEnumerator Simulate()
    {
        WaitForSeconds sec = new WaitForSeconds(timeInterval);

        yield return new WaitForSeconds(Random.Range(0, 0.05f));

        while(temperature > 0)
        {
            GetAdjacents();
            print(adjacents.Count);

            mt.color = grad.Evaluate(1 - temperature);

            float size = (temperature+0.5f)*0.6f;
            transform.localScale = new Vector3(1, 1, 1) * size;

            density = 0;

            foreach(Transform i in adjacents)
            {
                float distance = Vector3.Distance(i.position, transform.position);

                density += Poly6(distance);
            }

            if(density == 0) density = baseDensity;

            pressure = 20 * (density - baseDensity);
            
            float otherDensity;
            float otherPressure;
            Vector3 otherVelocity;

            force = Vector3.zero;

            foreach(Transform i in adjacents)
            {
                otherDensity = i.gameObject.GetComponent<FireParticle>().density;
                otherPressure = i.gameObject.GetComponent<FireParticle>().pressure;
                otherVelocity = i.gameObject.GetComponent<FireParticle>().velocity;

                float distance = Vector3.Distance(i.position, transform.position);
                Vector3 direction = (transform.position - i.position).normalized;

                force += direction * ((pressure + otherPressure) / (2*otherDensity)) * SpikyGrad(distance) * 4; // pressure force
                //force += (otherVelocity - velocity) / otherDensity * SpikyGradSquared(distance) * 7; // viscosity force
            }


            rb.velocity += (new Vector3(0, temperature * 5, 0) + force) * timeInterval;
            velocity = rb.velocity;

            temperature -= timeInterval/lifeSpan + Random.Range(-0.02f, 0.02f);

            yield return sec;
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision col)
    {
        o = col.gameObject.GetComponent<FireParticle>();

        if(o != null)
        {
            nextTemp = Mathf.Lerp(temperature, o.temperature, 0.2f);
        }
    }

    public void GetAdjacents()
    {
        hitColliders = Physics.OverlapSphere(transform.position, smoothingLength);
        adjacents = new List<Transform>();

        foreach(Collider col in hitColliders)
        {
            if(col == gameObject.GetComponent<Collider>() || !col.gameObject.CompareTag("Particle")) continue; 
            adjacents.Add(col.gameObject.transform);
        }
    }

    private float Poly6(float distance)
    {
        return Poly6KernelConstant * Mathf.Pow(Mathf.Pow(smoothingLength, 2) - Mathf.Pow(distance, 2), 3);
    }

    private float SpikyGrad(float distance)
    {
        return SpikyKernelConstant * Mathf.Pow(smoothingLength - distance, 2);
    }

    private float SpikyGradSquared(float distance)
    {
        return SpikyKernelConstant * (smoothingLength - distance);
    }    
}
