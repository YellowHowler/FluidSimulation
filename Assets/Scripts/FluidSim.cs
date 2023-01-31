using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FluidSim : MonoBehaviour
{
    [SerializeField] private GameObject particle;
    [SerializeField] private int maxSmokeParticles;
    [SerializeField] private Vector3Int particleNumbers;
    [SerializeField] private float smoothingLength;
    [SerializeField] private float timeInterval;
    [SerializeField] private Transform particleParent;
    [SerializeField] private Transform smokeSource;

    private const float baseDensity = 1f;
    private const float surfaceTension = 10f;
    private const float particleMass = 1;

    private Vector3 zero = new Vector3(0, 0, 0);
    private Vector3 center;
    private Vector3 corner;

    private float Gamma = 1/7f;
    private float GasConstant = 20f;
    private float Poly6KernelConstant;
    private float Poly6GradKernelConstant;
    private float SpikyKernelConstant;
    private float SpikyGradKernelConstant;
    private float SurfaceTensionConstant;
    private float SurfaceTensionOffset;
    private Vector3 GravityForce;
    
    private Transform[] particles;
    private Rigidbody[] particleRigidbodies;
    private ParticleCollision[] particleScripts;
    private float[] densities;
    private float[] temperatures;
    private float[] pressures;
    private Vector3[] forces;
    private Vector3[] velocities;
    
    private int particleNum;
    private int particleNumX;
    private int particleNumY;
    private int particleNumZ;
    private float particleDistance = 0.4f;

    private int curParticleNum;

    private List<int>[] adjacents;

    private void Initialize()
    {
        {
            Poly6KernelConstant = (315) / (64 * Mathf.PI * Mathf.Pow(smoothingLength, 9));
            Poly6GradKernelConstant = -945 / (32 * Mathf.PI * Mathf.Pow(smoothingLength, 9));
            SpikyKernelConstant = (15) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));
            SpikyGradKernelConstant = (-45) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));
            SurfaceTensionConstant = 32 / (Mathf.PI * Mathf.Pow(smoothingLength, 6)) * surfaceTension * Mathf.Sqrt(particleMass);
            SurfaceTensionOffset = Mathf.Pow(smoothingLength, 6) / 64;

            //GravityForce = new Vector3(0, -9.8f, 0);
        }
        
        particleNumX = particleNumbers.x;
        particleNumY = particleNumbers.y;
        particleNumZ = particleNumbers.z;
        
        center = transform.position;
        corner = center - new Vector3(particleNumX * particleDistance / 2, particleNumY * particleDistance / 2, particleNumZ * particleDistance / 2);

        particleNum = particleNumbers.x * particleNumbers.y * particleNumbers.z + maxSmokeParticles;
        particles = new Transform[particleNum];
        particleRigidbodies = new Rigidbody[particleNum];
        particleScripts = new ParticleCollision[particleNum];

        densities = new float[particleNum];
        temperatures = new float[particleNum];
        pressures = new float[particleNum];
        forces = new Vector3[particleNum];
        velocities = new Vector3[particleNum];

        adjacents = new List<int>[particleNum];
    }

    private void InitializeParticles()
    {
        Initialize();

        curParticleNum = particleNum - maxSmokeParticles;

        for(int x = 0; x < particleNumX; x++)
        {
            for(int y = 0; y < particleNumY; y++)
            {
                for(int z = 0; z < particleNumZ; z++)
                {
                    int ind = x*particleNumY*particleNumZ + y*particleNumZ + z;
                    
                    adjacents[ind] = new List<int>();

                    particles[ind] = Instantiate(particle, corner + new Vector3(x*particleDistance, y*particleDistance, z*particleDistance), Quaternion.identity).GetComponent<Transform>();
                    particles[ind].parent = particleParent;
                    particles[ind].gameObject.name = ind + "";
                    particleRigidbodies[ind] = particles[ind].gameObject.GetComponent<Rigidbody>();
                    particleScripts[ind] = particles[ind].gameObject.GetComponent<ParticleCollision>();
                    particleScripts[ind].index = ind;
                    particleScripts[ind].smoothingLength = smoothingLength;

                    densities[ind] = baseDensity;
                    forces[ind] = zero;
                    velocities[ind] = zero;
                    
                    temperatures[ind] = 25;

                    particles[ind].gameObject.GetComponent<Renderer>().material.color = new Color(0, 0.8f, 1, 1);
                }
            }
        }
    }

    private IEnumerator InitializeSmoke()
    {
        yield return new WaitForSeconds(4);

        WaitForSeconds sec = new WaitForSeconds(0.5f);

        int temp = curParticleNum;

        for(int i = 0; i < maxSmokeParticles; i+=40)
        {
            for(int j = 0; j < 40; j++)
            {
                int ind = temp + i + j;

                adjacents[ind] = new List<int>();

                Vector3 direction = Random.insideUnitSphere;

                particles[ind] = Instantiate(particle, smokeSource.position + direction, Quaternion.identity).GetComponent<Transform>();
                particles[ind].parent = smokeSource;
                particles[ind].gameObject.name = ind + "";
                particleRigidbodies[ind] = particles[ind].gameObject.GetComponent<Rigidbody>();
                particleScripts[ind] = particles[ind].gameObject.GetComponent<ParticleCollision>();
                particleScripts[ind].index = ind;
                particleScripts[ind].smoothingLength = smoothingLength;

                particles[ind].gameObject.GetComponent<Renderer>().material.color = new Color(0, 0, 0, 1);

                temperatures[ind] = 90;
                densities[ind] = baseDensity;
                forces[ind] = zero;
                velocities[ind] = zero;

                curParticleNum++;
            }

            yield return sec;
        }
    }

    void Start()
    {
        InitializeParticles();
        Time.timeScale = 0.5f;

        StartCoroutine(RunSim());
    }

    void Update()
    {
        
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

    private float SurfaceTension(float distance)
    {
        if(distance < smoothingLength / 2)
            return SurfaceTensionConstant * (2 * Mathf.Pow(smoothingLength - distance, 3) * Mathf.Pow(distance, 3) + SurfaceTensionOffset);
        else
            return SurfaceTensionConstant * Mathf.Pow(smoothingLength - distance, 3) * Mathf.Pow(distance, 3);
    }

    private IEnumerator RunSim()
    {
        WaitForSeconds sec = new WaitForSeconds(timeInterval);

        while(true)
        {
            for(int i = 0; i < curParticleNum; i++)
            {
                adjacents[i] = particleScripts[i].Adjacents();
                forces[i] = zero;
                densities[i] = 0;

                foreach(int j in adjacents[i])
                {
                    if(j == i) continue;

                    float distance = Vector3.Distance(particles[i].position, particles[j].position);

                    densities[i] += particleMass * Poly6(distance);
                }

                //pressures[i] = 4f * densities[i] * ((densities[i] / baseDensity) - 1);
                pressures[i] = 20 * (densities[i] - baseDensity);
                //pressures[i] = (baseDensity * Mathf.Pow(343, 2)) / (Gamma * Mathf.Pow(densities[i]/baseDensity, Gamma - 1));
            }

            for(int i = 0; i < curParticleNum; i++)
            {
                foreach(int j in adjacents[i])
                {
                    float distance = Vector3.Distance(particles[i].position, particles[j].position);
                    Vector3 direction = (particles[i].position - particles[j].position).normalized;

                    forces[i] += direction * ((pressures[i] + pressures[j]) / (2*densities[j])) * particleMass * SpikyGrad(distance); // pressure force
                    forces[i] += (velocities[j] - velocities[i]) / densities[j] * particleMass * SpikyGradSquared(distance); // viscosity force
                    //forces[i] += baseDensity / (densities[i] + densities[j]) * direction * particleMass * SurfaceTension(distance); // surface tension force
                }

                forces[i] = Vector3.ClampMagnitude(forces[i], 6f);
                //forces[i] += GravityForce;
            }

            for(int i = 0; i < curParticleNum; i++)
            {
                if(densities[i] != 0) particleRigidbodies[i].velocity += forces[i] / densities[i] * timeInterval;
                velocities[i] = particleRigidbodies[i].velocity;
            }

            yield return sec;
        }
    }
}
