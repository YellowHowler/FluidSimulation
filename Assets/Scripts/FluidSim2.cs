using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSim2 : MonoBehaviour
{
    [SerializeField] private GameObject particle;
    [SerializeField] private Vector3Int particleNumbers;
    [SerializeField] private float smoothingLength;
    [SerializeField] private float timeInterval;
    [SerializeField] private Transform particleParent;
    [SerializeField] private int solverIterations;

    private const float baseDensity = 1;
    private const float particleMass = 1;

    private Vector3 zero = new Vector3(0, 0, 0);

    private float GasConstant = 20;
    private float Poly6KernelConstant;
    private float PressureAccelerationConstant;
    private float ViscousAccelerationConstant;
    private Vector3 GravityAcceleration = new Vector3(0, -9.8f, 0);

    private Transform[] particles;
    private Rigidbody[] particleRigidbodies;
    private ParticleCollision[] particleScripts;
    private float[] densities;
    private float[] pressures;
    private Vector3[] forces;
    private Vector3[] velocities;
    private Vector3[] predictedPositions;
    
    private int particleNum;
    private int particleNumX;
    private int particleNumY;
    private int particleNumZ;

    private int iteration;

    private List<int>[] adjacents;

    private void Initialize()
    {
        {
            Poly6KernelConstant = (315 * particleMass) / (64 * Mathf.PI * Mathf.Pow(smoothingLength, 9));
            PressureAccelerationConstant = (45 * particleMass) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));
            ViscousAccelerationConstant = (45 * particleMass) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));
        }

        particleNumX = particleNumbers.x;
        particleNumY = particleNumbers.y;
        particleNumZ = particleNumbers.z;

        particleNum = particleNumbers.x * particleNumbers.y * particleNumbers.z;
        particles = new Transform[particleNum];
        particleRigidbodies = new Rigidbody[particleNum];
        particleScripts = new ParticleCollision[particleNum];

        densities = new float[particleNum];
        pressures = new float[particleNum];
        forces = new Vector3[particleNum];
        velocities = new Vector3[particleNum];
        predictedPositions = new Vector3[particleNum];

        adjacents = new List<int>[particleNum];

        for(int x = 0; x < particleNumX; x++)
        {
            for(int y = 0; y < particleNumY; y++)
            {
                for(int z = 0; z < particleNumZ; z++)
                {
                    int ind = x*particleNumY*particleNumZ + y*particleNumZ + z;
                    
                    adjacents[ind] = new List<int>();

                    particles[ind] = Instantiate(particle, new Vector3(x*1f, y*1f, z*1f), Quaternion.identity).GetComponent<Transform>();
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

    void Start()
    {
        Initialize();
        //Time.timeScale = .5f;

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
                //particleRigidbodies[i].AddForce(GravityAcceleration, ForceMode.VelocityChange);
                velocities[i] += GravityAcceleration * timeInterval;
                predictedPositions[i] = particles[i].position + timeInterval * velocities[i];
            }
            

            for(int i = 0; i < particleNum; i++)
            {
                adjacents[i] = particleScripts[i].Adjacents();
            }

            iteration = 0;

            while(iteration < solverIterations)
            {
                for(int i = 0; i < particleNum; i++)
                {
                    foreach(int j in adjacents[i])
                    {
                        if(j == i) continue;

                        float distance = Vector3.Distance(particles[i].position, particles[j].position);

                        densities[i] += Poly6KernelConstant * Mathf.Pow(Mathf.Pow(smoothingLength, 2) - Mathf.Pow(distance, 2), 3);
                    }

                    
                }

                iteration++;
            }

            yield return sec;
        }
    }
}
