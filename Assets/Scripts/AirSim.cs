using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AirSim : MonoBehaviour
{
    [SerializeField] private GameObject particle;
    [SerializeField] private int maxSmokeParticles;
    [SerializeField] private Vector3Int particleNumbers;
    [SerializeField] private float smoothingLength;
    [SerializeField] private float timeInterval;
    [SerializeField] private Transform particleParent;
    [SerializeField] private Transform smokeSource;
    [SerializeField] private Transform smokeSource2;
    [SerializeField] private Text timerText;

    private const float coldAirDensity = 1/50f;
    private const float hotAirDensity = 1/100f;
    private const float soundSpeed = 343;
    private const float surfaceTension = 1f;

    private Vector3 zero = new Vector3(0, 0, 0);
    private Vector3 center;
    private Vector3 corner;

    private float Gamma = 1/7f;
    private float ViscosityConstant = 0.548f;
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
    private int[] particleType; // 0:air, 1:smoke
    private float[] particleMass;
    private float[] densities;
    private float[] temperatures;
    private float[] pressures;
    private Vector3[] forces;
    private Vector3[] velocities;
    
    private int particleNum;
    private int particleNumX;
    private int particleNumY;
    private int particleNumZ;
    private float particleDistance = 0.15f;

    private int curParticleNum;

    private List<int>[] adjacents;

    private void Initialize()
    {
        {
            Poly6KernelConstant = (315) / (64 * Mathf.PI * Mathf.Pow(smoothingLength, 9));
            Poly6GradKernelConstant = -945 / (32 * Mathf.PI * Mathf.Pow(smoothingLength, 9));
            SpikyKernelConstant = (15) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));
            SpikyGradKernelConstant = (-45) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));
            //SurfaceTensionConstant = 32 / (Mathf.PI * Mathf.Pow(smoothingLength, 6)) * surfaceTension * Mathf.Sqrt(particleMass);
            SurfaceTensionOffset = Mathf.Pow(smoothingLength, 6) / 64;

            GravityForce = new Vector3(0, -9.8f, 0);
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

        particleType = new int[particleNum];
        densities = new float[particleNum];
        temperatures = new float[particleNum];
        pressures = new float[particleNum];
        particleMass = new float[particleNum];
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

                    temperatures[ind] = 28f;

                    particles[ind] = Instantiate(particle, corner + new Vector3(x*particleDistance, y*particleDistance, z*particleDistance), Quaternion.identity).GetComponent<Transform>();
                    particles[ind].parent = particleParent;
                    particles[ind].gameObject.name = ind + "";
                    particleRigidbodies[ind] = particles[ind].gameObject.GetComponent<Rigidbody>();
                    particleScripts[ind] = particles[ind].gameObject.GetComponent<ParticleCollision>();
                    particleScripts[ind].index = ind;
                    particleScripts[ind].smoothingLength = smoothingLength;
                    particleScripts[ind].temperature = temperatures[ind];
                    
                    particleType[ind] = 0;
                    particleMass[ind] = 1f;
                    forces[ind] = zero;
                    velocities[ind] = zero;

                    particles[ind].gameObject.GetComponent<Renderer>().material.color = new Color(1, 0f, 0, 0.7f);
                }
            }
        }
    }

    private IEnumerator InitializeSmoke()
    {
        yield return new WaitForSeconds(4);

        WaitForSeconds sec = new WaitForSeconds(0.5f);

        int temp = curParticleNum;

        for(int i = 0; i < maxSmokeParticles; i+=4)
        {
            for(int j = 0; j < 4; j++)
            {
                int ind = temp + i + j;

                adjacents[ind] = new List<int>();

                Vector3 direction = Random.insideUnitSphere * 0.2f;

                temperatures[ind] = 20;

                if(j%2 == 0)
                {
                    particles[ind] = Instantiate(particle, smokeSource.position + direction, Quaternion.identity).GetComponent<Transform>();
                    particles[ind].parent = smokeSource;
                    velocities[ind] = new Vector3(0.5f, -0.6f, 0f);
                    
                }
                else 
                {
                    particles[ind] = Instantiate(particle, smokeSource2.position + direction, Quaternion.identity).GetComponent<Transform>();
                    particles[ind].parent = smokeSource2;
                    velocities[ind] = new Vector3(-0.5f, -0.6f, 0f);
                }
                
                particles[ind].gameObject.name = ind + "";
                particleRigidbodies[ind] = particles[ind].gameObject.GetComponent<Rigidbody>();
                particleScripts[ind] = particles[ind].gameObject.GetComponent<ParticleCollision>();
                particleScripts[ind].index = ind;
                particleScripts[ind].smoothingLength = smoothingLength;
                particleScripts[ind].temperature = temperatures[ind];

                particles[ind].gameObject.GetComponent<Renderer>().material.color = new Color(0, 0, 1, 0.7f);

                particleType[ind] = 1;
                particleMass[ind] = 1f;
                
                forces[ind] = zero;

                curParticleNum++;
            }

            yield return sec;
        }
    }

    void Start()
    {
        InitializeParticles();
        StartCoroutine(InitializeSmoke());
        //Time.timeScale = 0.1f;

        //StartCoroutine(Initialize2());
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

    private IEnumerator RunSim()
    {
        WaitForSeconds sec = new WaitForSeconds(timeInterval);

        while(true)
        {
            for(int i = 0; i < curParticleNum; i++)
            {
                adjacents[i] = particleScripts[i].Adjacents();
                forces[i] = zero;
                if(particleType[i] == 0) densities[i] = hotAirDensity;
                else densities[i] = coldAirDensity;

                foreach(int j in adjacents[i])
                {
                    if(j == i) continue;

                    float distance = Vector3.Distance(particles[i].position, particles[j].position);

                    densities[i] += particleMass[j] * Poly6(distance);
                }

                if(particleType[i] == 0) pressures[i] = (coldAirDensity * Mathf.Pow(343, 2)) / (Gamma * Mathf.Pow(densities[i]/coldAirDensity, Gamma - 1));
                else pressures[i] = (hotAirDensity * Mathf.Pow(343, 2)) / (Gamma * Mathf.Pow(densities[i]/hotAirDensity, Gamma - 1));

                pressures[i] /= 3;
            }

            for(int i = 0; i < curParticleNum; i++)
            {
                foreach(int j in adjacents[i])
                {
                    float distance = Vector3.Distance(particles[i].position, particles[j].position);
                    Vector3 direction = ((particles[i].position - particles[j].position).normalized + Random.insideUnitSphere*0.2f).normalized;

                    forces[i] += direction * ((pressures[i] + pressures[j]) / (2*densities[j])) * particleMass[j] * SpikyGrad(distance); // pressure force
                    //forces[i] += 10 * (velocities[j] - velocities[i]) / densities[j] * particleMass[j] * SpikyGradSquared(distance); // viscosity force
                }

                forces[i] += GravityForce * densities[i];
                if(particleType[i] == 1) forces[i] += 100 * GravityForce * densities[i];
            }

            for(int i = 0; i < curParticleNum; i++)
            {
                try
                {
                    particleRigidbodies[i].velocity += forces[i] / 20000 / densities[i] * timeInterval;
                }
                catch (System.Exception)
                {
                    print(densities[i]);
                    throw;
                }
                
                //else particleRigidbodies[i].velocity += forces[i] / coldAirDensity * timeInterval;
            }

            timerText.text = (Time.time*10) + "";
            yield return sec;
        }
    }
}
