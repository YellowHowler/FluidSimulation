using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FluidParticles : MonoBehaviour
{
    //for shader setup
    [SerializeField] private ComputeShader shader = null;
    protected int pressureHandle = -1;
    uint threadGroupSizeX;
    int groupSizeX;
    //
    
    //parameter input 
    [SerializeField] private GameObject particleObject;
    [SerializeField] private Vector3Int particleCounts;
    [SerializeField] private float smoothingLength;
    [SerializeField] private float timeInterval;
    [SerializeField] private int neighborCalculationStep = 4;
    //

    // for initializing particles
    private Vector3 zero = new Vector3(0, 0, 0);
    private Vector3 center;
    private Vector3 corner;
    private int particleCount;
    private int particleCountX;
    private int particleCountY;
    private int particleCountZ;
    private float particleDistance = 0.25f;
    //

    // basic fluid properties
    private const float baseDensity = 10000;
    private const float surfaceTension = 1f;
    private const float viscosity = 0.001f;
    private float particleMass;
    //

    // particle gameobjects
    private Transform[] particles;
    private Rigidbody[] particleRigidbodies;
    private ParticleCollision[] particleScripts;
    private List<int>[] adjacents;
    //

    //for iterating
    private int nStepCalcNum = 0;
    //
    
    struct Particle
    {
        public int ind;
        public float density;
        public float pressure;
        public Vector3 force;
        public Vector3 velocity;
        public Vector3 normal;
        public Vector3 position;
    }

    //paritcle buffer
    Particle[] particleData;
    ComputeBuffer particleBuffer;
    ComputeBuffer neighborBuffer;
    //

    protected virtual void InitShader()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError("It seems your target Hardware does not support Compute Shaders.");
            return;
        }

        if (!shader)
        {
            Debug.LogError("No shader");
            return;
        }

        pressureHandle = shader.FindKernel("GetPressure");
        shader.GetKernelThreadGroupSizes(pressureHandle, out threadGroupSizeX, out _, out _);
        groupSizeX = (int)((particleCount + threadGroupSizeX - 1) / threadGroupSizeX);

        int stride = sizeof(int) + (1+1+3+3+3+3)*sizeof(float);
        particleBuffer = new ComputeBuffer(particleCount, stride);
        particleBuffer.SetData(particleData);
        shader.SetBuffer(pressureHandle, "particleBuffer", particleBuffer);
    }

    private void DispatchPressureKernel(int count)
    {
    	shader.Dispatch(pressureHandle, groupSizeX, 1, 1);
        shader.SetFloat("time", Time.time);
    }

    protected virtual void OnEnable()
    {
        InitShader();
    }

    private void InitKernel()
    {
        Kernels.smoothingLength = smoothingLength;
        Kernels.particleMass = particleMass;
        Kernels.surfaceTension = surfaceTension;

        Kernels.Init();
    }

    private void InitParticles()
    {
        particleMass = baseDensity * Mathf.Pow(particleDistance, 3) / 1.17f;
        
        particleCountX = particleCounts.x;
        particleCountY = particleCounts.y;
        particleCountZ = particleCounts.z;
        
        center = transform.position;
        corner = center - new Vector3(particleCountX * particleDistance / 2, particleCountY * particleDistance / 2, particleCountZ * particleDistance / 2);

        particleCount = particleCounts.x * particleCounts.y * particleCounts.z;
        particles = new Transform[particleCount];
        particleRigidbodies = new Rigidbody[particleCount];
        particleScripts = new ParticleCollision[particleCount];

        adjacents = new List<int>[particleCount];

        particleData = new Particle[particleCount];

        for(int x = 0; x < particleCountX; x++)
        {
            for(int y = 0; y < particleCountY; y++)
            {
                for(int z = 0; z < particleCountZ; z++)
                {
                    int ind = x*particleCountY*particleCountZ + y*particleCountZ + z;
                    
                    adjacents[ind] = new List<int>();

                    //instantiate particles
                    particles[ind] = Instantiate(particleObject, corner + new Vector3(x*particleDistance, y*particleDistance, z*particleDistance), Quaternion.identity).GetComponent<Transform>();
                    particles[ind].parent = transform;
                    particles[ind].gameObject.name = ind + "";
                    particleRigidbodies[ind] = particles[ind].gameObject.GetComponent<Rigidbody>();
                    particleRigidbodies[ind].velocity = new Vector3(0, -3f, 0);
                    particleScripts[ind] = particles[ind].gameObject.GetComponent<ParticleCollision>();
                    particleScripts[ind].index = ind;
                    particleScripts[ind].smoothingLength = smoothingLength;

                    particles[ind].gameObject.GetComponent<Renderer>().material.color = new Color(0, 0.8f, 1, 1);
                    //

                    Particle particle = particleData[ind];
                    particle.ind = ind;
                    particle.density = baseDensity;
                    particle.pressure = 0;
                    particle.force = Vector3.zero;
                    particle.velocity = Vector3.zero;
                    particle.normal = Vector3.zero;
                    particle.position = particles[ind].position;
                    particleData[ind] = particle;
                }
            }
        }
    }

    void Start()
    {
        InitParticles();
        InitKernel();
        InitShader();

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
            for(int i = 0; i < particleCount; i++)
            {
                if(nStepCalcNum == 0) adjacents[i] = particleScripts[i].Adjacents();

                int stride = sizeof(int);
                neighborBuffer = new ComputeBuffer(adjacents[i].Count, stride);
                neighborBuffer.SetData(adjacents[i]);
                shader.SetBuffer(pressureHandle, "neighborBuffer", neighborBuffer);

                DispatchPressureKernel(adjacents[i].Count);
                

                yield return 0;
            }

            nStepCalcNum = (nStepCalcNum + 1) % neighborCalculationStep;
            yield return sec;
        }
    }
}
