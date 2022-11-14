using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSim : MonoBehaviour
{
    [SerializeField] private GameObject particle;
    [SerializeField] private int maxParticles;
    [SerializeField] private Vector3Int particleNumbers;
    [SerializeField] private float smoothingLength;
    [SerializeField] private float timeInterval;
    [SerializeField] private Transform particleParent;

    private const float baseDensity = 1;
    private const float particleMass = 1;

    private Vector3 zero = new Vector3(0, 0, 0);
    private Vector3 center;
    private Vector3 corner;

    private float GasConstant = 20;
    private float DensityKernelConstant;
    private float PressureForceConstant;
    private float ViscousForceConstant;
    private Vector3 GravityForce = new Vector3(0, -9.8f, 0);

    private Transform[] particles;
    private Rigidbody[] particleRigidbodies;
    private ParticleCollision[] particleScripts;
    private float[] densities;
    private float[] pressures;
    private Vector3[] forces;
    private Vector3[] velocities;
    
    private int particleNum;
    private int particleNumX;
    private int particleNumY;
    private int particleNumZ;
    private float particleDistance = 0.1f;

    private List<int>[] adjacents;

    private void Initialize()
    {
        {
            DensityKernelConstant = (315 * particleMass) / (64 * Mathf.PI * Mathf.Pow(smoothingLength, 9));
            PressureForceConstant = (45 * particleMass) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));
            ViscousForceConstant = (45 * particleMass) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));
        }
        
        particleNumX = particleNumbers.x;
        particleNumY = particleNumbers.y;
        particleNumZ = particleNumbers.z;
        
        center = transform.position;
        corner = center - new Vector3(particleNumX * particleDistance / 2, particleNumY * particleDistance / 2, particleNumZ * particleDistance / 2);

        particleNum = particleNumbers.x * particleNumbers.y * particleNumbers.z;
        particles = new Transform[particleNum];
        particleRigidbodies = new Rigidbody[particleNum];
        particleScripts = new ParticleCollision[particleNum];

        densities = new float[particleNum];
        pressures = new float[particleNum];
        forces = new Vector3[particleNum];
        velocities = new Vector3[particleNum];

        adjacents = new List<int>[particleNum];

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
                }
            }
        }
    }

    private IEnumerator Initialize2()
    {
        {
            DensityKernelConstant = (315 * particleMass) / (64 * Mathf.PI * Mathf.Pow(smoothingLength, 9));
            PressureForceConstant = (45 * particleMass) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));
            ViscousForceConstant = (45 * particleMass) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));
        }

        particleNum = 0;
        particles = new Transform[maxParticles];
        particleRigidbodies = new Rigidbody[maxParticles];
        particleScripts = new ParticleCollision[maxParticles];

        densities = new float[maxParticles];
        pressures = new float[maxParticles];
        forces = new Vector3[maxParticles];
        velocities = new Vector3[maxParticles];

        adjacents = new List<int>[maxParticles];

        WaitForSeconds sec = new WaitForSeconds(0.05f);

        for(int i = 0; i < maxParticles; i+=1000)
        {
            for(int j = 0; j < 1000; j++)
            {
                int ind = i + j;

                adjacents[ind] = new List<int>();

                Vector3 direction = Random.insideUnitSphere;

                particles[ind] = Instantiate(particle, transform.position + direction, Quaternion.identity).GetComponent<Transform>();
                particles[ind].parent = particleParent;
                particles[ind].gameObject.name = ind + "";
                particleRigidbodies[ind] = particles[ind].gameObject.GetComponent<Rigidbody>();
                particleScripts[ind] = particles[ind].gameObject.GetComponent<ParticleCollision>();
                particleScripts[ind].index = ind;
                particleScripts[ind].smoothingLength = smoothingLength;

                densities[ind] = baseDensity;
                forces[ind] = zero;
                velocities[ind] = zero;

                particleNum++;
            }

            yield return sec;
        }
    }

    void Start()
    {
        Initialize();
        Time.timeScale = .05f;

        //StartCoroutine(Initialize2());
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
            for(int i = 0; i < particleNum; i++)
            {
                adjacents[i] = particleScripts[i].Adjacents();
                forces[i] = zero;
                densities[i] = 0;

                foreach(int j in adjacents[i])
                {
                    if(j == i) continue;

                    float distance = Vector3.Distance(particles[i].position, particles[j].position);

                    //if(distance > smoothingLength) continue;
                    
                    //adjacents[i].Add(j);
                    densities[i] += DensityKernelConstant * Mathf.Pow(Mathf.Pow(smoothingLength, 2) - Mathf.Pow(distance, 2), 3);
                }

                //pressures[i] = 4f * densities[i] * ((densities[i] / baseDensity) - 1);
                pressures[i] = GasConstant * (densities[i] - baseDensity);
            }

            for(int i = 0; i < particleNum; i++)
            {
                foreach(int j in adjacents[i])
                {
                    float distance = Vector3.Distance(particles[i].position, particles[j].position);
                    Vector3 direction = (particles[i].position - particles[j].position).normalized;

                    forces[i] += direction * PressureForceConstant * ((pressures[i] + pressures[j]) / (2*densities[j])) * Mathf.Pow(smoothingLength - distance, 2);
                    forces[i] += ViscousForceConstant * (velocities[j] - velocities[i]) / densities[j] * (smoothingLength - distance);
                }

                forces[i] = Vector3.ClampMagnitude(forces[i], 6f);
                forces[i] += GravityForce;
            }

            for(int i = 0; i < particleNum; i++)
            {
                //print(forces[i]);
                particleRigidbodies[i].AddForce(forces[i], ForceMode.Impulse);
                velocities[i] = particleRigidbodies[i].velocity;
                //particleRigidbodies[i].velocity += forces[i]/particleMass * timeInterval;
            }

            yield return sec;
        }
    }
}
