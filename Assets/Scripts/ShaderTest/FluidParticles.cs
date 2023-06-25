using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FluidParticles : MonoBehaviour
{
    [SerializeField] private GameObject particleObject;

    //for shader setup
    [SerializeField] private ComputeShader shader = null;
    protected int pressureHandle = -1;
    protected int normalHandle = -1;
    protected int forceHandle = -1;
    protected int applyHandle = -1;
    protected int colliderHandle = -1;
    uint threadGroupSizeX;
    int groupSizeX;
    //
    
    //parameter input 
    [SerializeField] private Vector3Int particleCounts;
    [SerializeField] private float smoothingLength;
    [SerializeField] private float timeInterval;
    //

    //particle mesh
    [SerializeField] private Mesh particleMesh = null;
    [SerializeField] private Material material;
    private float radius = 0.2f;
    //

    // for initializing particles
    private Vector3 zero = new Vector3(0, 0, 0);
    private Vector3 center;
    private Vector3 corner;
    private int particleCount;
    private int particleCountX;
    private int particleCountY;
    private int particleCountZ;
    private float particleDistance = 0.5f;
    //

    // basic fluid properties
    private const float baseDensity = 10000;
    private const float surfaceTension = 1f;
    private const float viscosity = 0.001f;
    private const float particleDrag = 0.025f;
    private float particleMass;
    //

    //constants
    private const float BOUND_DAMPING = -0.5f;
    private const float DT = 0.0008f;
    //

    private Transform[] particles;

    Bounds bounds;
    
    struct Particle
    {
        public int ind;
        public float density;
        public float pressure;
        public Vector3 force;
        public Vector3 velocity;
        public Vector3 normal;
        public Vector3 position;

        public Particle(int index, Vector3 pos)
        {
            ind = index;
            position = pos;
            velocity = Vector3.zero;
            force = Vector3.zero;
            normal = Vector3.zero;
            density = 0f;
            pressure = 0f;
        }
    }

    private struct SPHCollider
    {
        public Vector3 position;
        public Vector3 right;
        public Vector3 up;
        public Vector2 scale;

        public SPHCollider(Transform _transform)
        {
            position = _transform.position;
            right = _transform.right;
            up = _transform.up;
            scale = new Vector2(_transform.lossyScale.x / 2f, _transform.lossyScale.y / 2f);     
        }
    }

    //colliders
    SPHCollider[] collidersArray;
    ComputeBuffer collidersBuffer;
    int SIZE_SPHCOLLIDER = 11 * sizeof(float);        
    //

    //particle buffer
    Particle[] particlesArray;
    ComputeBuffer particleBuffer;
    uint[] argsArray = { 0, 0, 0, 0, 0 };
    ComputeBuffer argsBuffer;
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

        pressureHandle = shader.FindKernel("CalculatePressure");
        normalHandle = shader.FindKernel("CalculateNormal");
        forceHandle = shader.FindKernel("CalculateForces");
        applyHandle = shader.FindKernel("ApplyForces");
        colliderHandle = shader.FindKernel("ComputeColliders");

        shader.GetKernelThreadGroupSizes(pressureHandle, out threadGroupSizeX, out _, out _);
        groupSizeX = (int)((particleCount + threadGroupSizeX - 1) / threadGroupSizeX);

        int stride = sizeof(int) + (1+1+3+3+3+3)*sizeof(float);
        print(particleCount);
        particleBuffer = new ComputeBuffer(particleCount, stride);
        particleBuffer.SetData(particlesArray);

        UpdateColliders();

        argsArray[0] = particleMesh.GetIndexCount(0);
        argsArray[1] = (uint)particleCount;
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsArray);

        shader.SetInt("particleCount", particleCount);
        shader.SetFloat("timeInterval", timeInterval);
        shader.SetFloat("SmoothingLength", smoothingLength);

        shader.SetFloat("particleMass", particleMass);
        shader.SetFloat("radius", radius);

        shader.SetFloat("viscosity", viscosity);
        shader.SetFloat("baseDensity", baseDensity);
        shader.SetFloat("surfaceTension", surfaceTension);

        shader.SetFloat("damping", BOUND_DAMPING);
        shader.SetFloat("particleDrag", particleDrag);

        shader.SetFloat("Poly6KernelConstant", Kernels.Poly6KernelConstant);
        shader.SetFloat("Poly6GradKernelConstant", Kernels.Poly6GradKernelConstant);
        shader.SetFloat("SpikyKernelConstant", Kernels.SpikyKernelConstant);
        shader.SetFloat("SpikyGradKernelConstant", Kernels.SpikyGradKernelConstant);
        shader.SetFloat("SpikyGradSquaredKernelConstant", Kernels.SpikyGradSquaredKernelConstant);
        shader.SetFloat("ViscosityLaplaceKernelConstant", Kernels.ViscosityLaplaceKernelConstant);
        shader.SetFloat("SurfaceTensionConstant", Kernels.SurfaceTensionConstant);
        shader.SetFloat("SurfaceTensionOffset", Kernels.SurfaceTensionOffset);

        shader.SetBuffer(pressureHandle, "particleBuffer", particleBuffer);
        shader.SetBuffer(normalHandle, "particleBuffer", particleBuffer);
        shader.SetBuffer(forceHandle, "particleBuffer", particleBuffer);
        shader.SetBuffer(applyHandle, "particleBuffer", particleBuffer);
        shader.SetBuffer(colliderHandle, "particleBuffer", particleBuffer);
        shader.SetBuffer(colliderHandle, "colliders", collidersBuffer);

        material.SetBuffer("particleBuffer", particleBuffer);
        material.SetFloat("_Radius", radius);
    }

    private void DispatchKernels(int count)
    {
        UpdateColliders();

    	shader.Dispatch(pressureHandle, groupSizeX, 1, 1);
        shader.Dispatch(normalHandle, groupSizeX, 1, 1);
        shader.Dispatch(forceHandle, groupSizeX, 1, 1);
        shader.Dispatch(applyHandle, groupSizeX, 1, 1);
        shader.Dispatch(colliderHandle, groupSizeX, 1, 1);

        particleBuffer.GetData(particlesArray);

        //Graphics.DrawMeshInstancedIndirect(particleMesh, 0, material, bounds, argsBuffer);

        for(int i = 0; i < particleCount; i++)
        {
            particles[i].position = particlesArray[i].position;
        }

        print(particlesArray[0].position);
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

        bounds = new Bounds(center, Vector3.one * 0);

        particleCount = particleCountX * particleCountY * particleCountZ;

        particlesArray = new Particle[particleCount];

        particles = new Transform[particleCount];

        for(int x = 0; x < particleCountX; x++)
        {
            for(int y = 0; y < particleCountY; y++)
            {
                for(int z = 0; z < particleCountZ; z++)
                {
                    int ind = x*particleCountY*particleCountZ + y*particleCountZ + z;
                    Vector3 pos = corner + new Vector3(x*particleDistance, y*particleDistance, z*particleDistance);

                    particlesArray[ind] = new Particle(ind, pos);
                    particles[ind] = Instantiate(particleObject, pos, Quaternion.identity).GetComponent<Transform>();
                    particles[ind].parent = transform;
                    //print(pos);
                }
            }
        }
    }

    private void 

    void UpdateColliders()
    {
        GameObject[] collidersGO = GameObject.FindGameObjectsWithTag("SPHCollider");
        if (collidersArray == null || collidersArray.Length != collidersGO.Length)
        {
            collidersArray = new SPHCollider[collidersGO.Length];
            if (collidersBuffer != null)
            {
                collidersBuffer.Dispose();
            }
            collidersBuffer = new ComputeBuffer(collidersArray.Length, SIZE_SPHCOLLIDER);
        }
        for (int i = 0; i < collidersArray.Length; i++)
        {
            collidersArray[i] = new SPHCollider(collidersGO[i].transform);
        }
        collidersBuffer.SetData(collidersArray);
        shader.SetBuffer(colliderHandle, "colliders", collidersBuffer);      
    }

    void Start()
    {
        InitParticles();
        InitKernel();
        InitShader();

        //StartCoroutine(RunSim());
    }

    void Update()
    {
        DispatchKernels(particleCount);
    }

/*
    private IEnumerator RunSim()
    {
        WaitForSeconds sec = new WaitForSeconds(timeInterval);

        while(true)
        {
            

            yield return sec;
        }
    }
    */

    private void OnDestroy()
    {
        particleBuffer.Dispose();
        collidersBuffer.Dispose();
        argsBuffer.Dispose();
    }
}
