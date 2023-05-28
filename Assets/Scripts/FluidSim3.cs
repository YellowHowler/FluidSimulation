using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FluidSim3 : MonoBehaviour
{
    [SerializeField] private GameObject particle;
    [SerializeField] private int neighborCalculationStep = 4;
    [SerializeField] private int maxSmokeParticles;
    [SerializeField] private Vector3Int particleNumbers;
    [SerializeField] private float smoothingLength;
    [SerializeField] private float timeInterval;
    [SerializeField] private Transform particleParent;
    [SerializeField] private Transform smokeSource;

    private const float baseDensity = 10000;
    private const float surfaceTension = 1f;
    private float particleMass;
    private const float viscosity = 0.001f;

    private Vector3 zero = new Vector3(0, 0, 0);
    private Vector3 center;
    private Vector3 corner;

    private float Gamma = 1/7f;
    private float GasConstant = 20f;
    private float PressureConstant = 1f;
    private float ExponentialConstant = 4f;
    private float Poly6KernelConstant;
    private float Poly6GradKernelConstant;
    private float SpikyKernelConstant;
    private float SpikyGradKernelConstant;
    private float SpikyGradSquaredKernelConstant;
    private float ViscosityLaplaceKernelConstant;
    private float SurfaceTensionConstant;
    private float SurfaceTensionOffset;
    private Vector3 gravityForce;
    
    private Transform[] particles;
    private Rigidbody[] particleRigidbodies;
    private ParticleCollision[] particleScripts;
    private float[] densities;
    private float[] temperatures;
    private float[] pressures;
    private Vector3[] forces;
    private Vector3[] velocities;
    private Vector3[] normals;

    private Vector3 pressureForce;
    private Vector3 viscosityForce;
    private Vector3 cohesionForce;
    private Vector3 curvatureForce;
    
    private int particleNum;
    private int particleNumX;
    private int particleNumY;
    private int particleNumZ;
    private float particleDistance = 0.2f;

    private int curParticleNum;
    private int nStepCalcNum = 0;

    private List<int>[] adjacents;

    private void Initialize()
    {
        {
            Poly6KernelConstant = (365) / (64 * Mathf.PI * Mathf.Pow(smoothingLength, 9));
            Poly6GradKernelConstant = -945 / (32 * Mathf.PI * Mathf.Pow(smoothingLength, 9));
            SpikyKernelConstant = (15) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));
            SpikyGradKernelConstant = (-45) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));
            SpikyGradSquaredKernelConstant = -90/(Mathf.PI * Mathf.Pow(smoothingLength, 6));

            SurfaceTensionConstant = 32 / (Mathf.PI * Mathf.Pow(smoothingLength, 6)) * surfaceTension * Mathf.Sqrt(particleMass);
            SurfaceTensionOffset = Mathf.Pow(smoothingLength, 6) / 64;

            gravityForce = new Vector3(0, -9.8f, 0);
        }

        particleMass = baseDensity * Mathf.Pow(particleDistance, 3) / 1.17f;
        PressureConstant = baseDensity;
        
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
        normals = new Vector3[particleNum];

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
                    particleRigidbodies[ind].velocity = new Vector3(0, -3f, 0);
                    particleScripts[ind] = particles[ind].gameObject.GetComponent<ParticleCollision>();
                    particleScripts[ind].index = ind;
                    particleScripts[ind].smoothingLength = smoothingLength;

                    densities[ind] = baseDensity;
                    forces[ind] = new Vector3(0, 0, -10);
                    velocities[ind] = zero;

                    particleRigidbodies[ind].velocity = new Vector3(0, 0, -10);
                    
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
        Time.timeScale = 0.05f;

        StartCoroutine(RunSim());
    }

    void Update()
    {
        
    }

    private float Poly6(float distance)
    {
        return Mathf.Pow(Mathf.Pow(smoothingLength, 2) - Mathf.Pow(distance, 2), 3);
    }

    private Vector3 Poly6Grad(Vector3 distance)
    {
        return Mathf.Pow(Mathf.Pow(smoothingLength, 2) - Vector3.Dot(distance, distance), 2) * distance;
    }

    private Vector3 SpikyGrad(Vector3 distance)
    {
        return Mathf.Pow(smoothingLength - distance.magnitude, 2) * distance / distance.magnitude;
    }

    private float SpikyGradSquared(float distance)
    {
        //return SpikyKernelConstant * (smoothingLength - distance);
        return SpikyGradSquaredKernelConstant * 1/distance * Mathf.Pow(smoothingLength-distance, 2) - (smoothingLength-distance);
    }    

    private float ViscosityLaplace(float distance)
    {
        return (smoothingLength - distance);
    }

    private float SurfaceTension(float distance)
    {
        if(distance < smoothingLength / 2)
            return 2 * Mathf.Pow(smoothingLength - distance, 3) * Mathf.Pow(distance, 3) + SurfaceTensionOffset;
        else
            return Mathf.Pow(smoothingLength - distance, 3) * Mathf.Pow(distance, 3);
    }

    private IEnumerator RunSim()
    {
        WaitForSeconds sec = new WaitForSeconds(timeInterval);

        while(true)
        {
            for(int i = 0; i < curParticleNum; i++)
            {
                if(nStepCalcNum == 0) adjacents[i] = particleScripts[i].Adjacents();

                forces[i] = zero;
                densities[i] = 0;

                float density = 0;

                foreach(int j in adjacents[i])
                {
                    if(j == i) continue;

                    Vector3 distance = particles[i].position - particles[j].position;

                    density += Poly6(Vector3.Dot(distance, distance));
                }

                densities[i] = Poly6KernelConstant * particleMass * baseDensity;

                print(densities[i]);

                //pressures[i] = 4f * densities[i] * ((densities[i] / baseDensity) - 1);
                //pressures[i] = 20 * (densities[i] - baseDensity);
                //pressures[i] = (baseDensity * Mathf.Pow(343, 2)) / (Gamma * Mathf.Pow(densities[i]/baseDensity, Gamma - 1));
                //pressures[i] = PressureConstant * ( Mathf.Pow(densities[i] / baseDensity, ExponentialConstant) -1 );

                pressures[i] = PressureConstant * (Mathf.Pow(densities[i] / baseDensity, 7) - 1);
            }

            for(int i = 0; i < curParticleNum; i++)
            {
                normals[i] = zero;
                Vector3 normal = zero;

                foreach(int j in adjacents[i])
                {
                    normal += Poly6Grad(particles[i].position - particles[j].position) / densities[j];
                }

                normal *= smoothingLength * particleMass * Poly6GradKernelConstant;
                normals[i] = normal;
            }

            for(int i = 0; i < curParticleNum; i++)
            {
                forces[i] = zero;

                if(densities[i] > 0.001f)
                {
                    pressureForce = zero;
                    viscosityForce = zero;

                    foreach(int j in adjacents[i])
                    {
                        float distance = Vector3.Distance(particles[i].position, particles[j].position);
                        Vector3 distanceVect = (particles[i].position - particles[j].position);

                        //forces[i] -= direction * ((pressures[i] + pressures[j]) / (2*densities[j])) * particleMass * SpikyGrad(distance); // pressure force

                        //forces[i] -= direction * particleMass * pressures[j] / densities[j] * SpikyGrad(distance) / densities[i];
                        if (densities[j] > 0.001f) viscosityForce -= (velocities[j] - velocities[i]) / densities[j] * ViscosityLaplace(distance); // viscosity force

                        pressureForce -= (pressures[i] / Mathf.Pow(densities[i], 2) + pressures[j] / Mathf.Pow(densities[j], 2)) * SpikyGrad(particles[i].position - particles[j].position);
                        
                        float correctionFactor = 2 * baseDensity / (densities[i] + densities[j]);
                        cohesionForce += correctionFactor * (distanceVect / distance) * SurfaceTension(distance);
                        curvatureForce += correctionFactor * (normals[i] - normals[j]);

                        //forces[i] += baseDensity / (densities[i] + densities[j]) * direction * particleMass * SurfaceTension(distance); // surface tension force
                    }

                    
                    //forces[i] += gravityForce * baseDensity;

                    pressureForce *= Mathf.Pow(particleMass, 2) * SpikyGradKernelConstant;
                    viscosityForce *= viscosity * particleMass * ViscosityLaplaceKernelConstant;
                    cohesionForce *= -surfaceTension * Mathf.Pow(particleMass, 2) * SurfaceTensionConstant;
                    curvatureForce *= -surfaceTension * particleMass;

                    forces[i] += pressureForce + viscosityForce + cohesionForce;// + curvatureForce;

                    //forces[i] *= 0.001f;

                    if(float.IsNaN(forces[i].x)) forces[i] = zero;
                    forces[i] = Vector3.ClampMagnitude(forces[i], 40);
                }

                forces[i] += gravityForce * particleMass;
            }

            for(int i = 0; i < curParticleNum; i++)
            {
                if(densities[i] != 0) particleRigidbodies[i].velocity += forces[i]/particleMass * timeInterval;
                print(particleMass);
                velocities[i] = particleRigidbodies[i].velocity;
            }

            nStepCalcNum = (nStepCalcNum + 1) % neighborCalculationStep;
            yield return sec;
        }
    }
}
