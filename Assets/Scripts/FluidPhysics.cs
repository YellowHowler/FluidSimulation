using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidPhysics : MonoBehaviour
{
    //kg, m^3

    [HideInInspector] public List<Particle> parts;

    [SerializeField] private float distBtwnPart = 0.1f;
    [SerializeField] private Vector3 boundStart = new Vector3(-3, 0, -3);
    [SerializeField] private Vector3 boundSize = new Vector3(6, 3, 6);
    [SerializeField] private float regionSize = 0.5f; // make sure each x, y, z of boundSize is divisible by regionSize

    [SerializeField] private float cutOffRadius = 0.5f;
    [SerializeField] private float timeInterval = 0.5f;

    [SerializeField] private GameObject cube; // temp

    private int partNum;
    private float partMass = 1f;
    private float baseDens = 1;

    private HashSet<int>[ , , ] regions;

    int regSizeX;
    int regSizeY;
    int regSizeZ;

    float kernelConstant;
    float gradKernelConstant;
    float gasConstant = 20f;

    GameObject[] partMeshes;

    public struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3Int region;
        public float mass;
        public float density;
        public float pressure;
        public Color color;

        public Particle(Vector3 position, float mass, float density, float pressure)
        {
            this.position = position;
            this.velocity = new Vector3(0, 0, 0);
            this.region = new Vector3Int(-1, -1, -1);
            this.mass = mass; 
            this.density = density;
            this.pressure = pressure;
            this.color = new Color(255, 255, 255);
        }

        public void ChangeReg(Vector3Int newReg)
        {
            this.region = newReg;
        }

        public void ChangeDensAndPres(float newDensity, float newPressure)
        {
            this.density = newDensity;
            this.pressure = newPressure;
        }

        public void ChangeVelocity(Vector3 acceleration, float timeInterval)
        {
            this.velocity = this.velocity + timeInterval * acceleration;
        }

        public void ChangePosition(float timeInterval, Vector3 velocity)
        {
            this.position = this.position + 0.5f * velocity;
            print(timeInterval * velocity);
        }

        public void ChangePosition(Vector3 newPosition)
        {
            this.position = newPosition;
        }
    }

    public void Initialize()
    {
        kernelConstant = 315 / 64 / Mathf.PI / Mathf.Pow(cutOffRadius, 9);
        gradKernelConstant = -1 * 45 / Mathf.PI / Mathf.Pow(cutOffRadius, 6);

        regSizeX = (int)(Mathf.Round(boundSize.x / regionSize)) + 1;
        regSizeY = (int)(Mathf.Round(boundSize.y / regionSize)) + 1;
        regSizeZ = (int)(Mathf.Round(boundSize.z / regionSize)) + 1;

        regions = new HashSet<int>[regSizeX, regSizeY, regSizeZ];

        for(int i = 0; i < regSizeX; i++) 
        {
            for(int j = 0; j < regSizeY; j++) 
            {
                for(int k = 0; k < regSizeZ; k++) 
                {
                    regions[i, j, k] = new HashSet<int>();
                }
            }
        }

        int partNumSideX = (int)(boundSize.x / distBtwnPart);
        int partNumSideY = (int)(boundSize.y / distBtwnPart);
        int partNumSideZ = (int)(boundSize.z / distBtwnPart);

        partNum = partNumSideX * partNumSideY * partNumSideZ;
    
        parts = new List<Particle>();
        partMeshes = new GameObject[partNum];

        for(int i = 0; i < partNumSideX; i++) 
        {
            for(int j = 0; j < partNumSideY; j++) 
            {
                for(int k = 0; k < partNumSideZ; k++) 
                {
                    int ind = i*partNumSideY*partNumSideZ + j*partNumSideZ + k;

                    Vector3 partPos = new Vector3(boundStart.x + i*distBtwnPart, boundStart.y + j*distBtwnPart, boundStart.z + k*distBtwnPart);
                    parts.Add(new Particle(partPos, partMass, baseDens, 0));
                    parts[ind].ChangeReg(ChangeRegion(ind, partPos, parts[ind].region));

                    partMeshes[ind] = Instantiate(cube, partPos, Quaternion.identity);
                }
            }
        }
    }

    private Vector3Int ChangeRegion(int ind, Vector3 pos, Vector3Int curReg)
    {
        int x = (int)((pos.x - boundStart.x)/regionSize);
        int y = (int)((pos.y - boundStart.y)/regionSize);
        int z = (int)((pos.z - boundStart.z)/regionSize);

        if(curReg.x != -1 && (curReg.x == x && curReg.y == y && curReg.z == z))
        {
            regions[curReg.x, curReg.y, curReg.z].Remove(ind);
            regions[x, y, z].Add(ind);
        }
        if(curReg.x == -1)
        {
            regions[x, y, z].Add(ind);
        }

        return new Vector3Int(x, y, z);
    }

    private float Kernel(float distance)
    {
        if(distance > cutOffRadius) return 0;
        
        return Mathf.Pow(Mathf.Pow(cutOffRadius, 2) - Mathf.Pow(distance, 2), 3);
    }

    private float GradKernel(float distance)
    {
        if(distance > cutOffRadius) return 0;
        
        return (cutOffRadius - distance);
    }

    void Start()
    {
        Initialize();

        StartCoroutine(UpdateParticles());
    }

    void Update()
    {

    }

    IEnumerator UpdateParticles() //coroutine 으로 옮기기
    {
        WaitForSeconds sec = new WaitForSeconds(timeInterval);

        while(true)
        {
            for(int i = 0; i < partNum; i++) // updating density of each particle
            {
                //ρ_i = ∑ m_j * W(r − r_j, h)

                Particle partI = parts[i];
                Vector3Int reg = partI.region;

                float newDensity = 0;

                for(int x = -1; x <= 1; x++) // checking all nearby regions
                {
                    if(reg.x + x < 0 || reg.x + x >= regSizeX) continue;

                    for(int y = -1; y <= 1; y++)
                    {
                        if(reg.y + y < 0 || reg.y + y >= regSizeY) continue;

                        for(int z = -1; z <= 1; z++)
                        {
                            if(reg.z + z < 0 || reg.z + z >= regSizeZ) continue;

                            foreach(int ind in regions[x, y, z]) // checking all other particles in the nearby regions
                            {
                                if(ind == i) continue;
                                
                                Particle partJ = parts[ind];
                                float distance = Vector3.Distance(partI.position, partJ.position);
                                newDensity += Kernel(distance);
                            }
                        }
                    }
                }
                
                partI.ChangeDensAndPres(newDensity, gasConstant * (newDensity - baseDens));
            }

            for(int i = 0; i < partNum; i++) // updating all particle accelerations
            {
                Particle partI = parts[i];
                Vector3Int reg = partI.region;

                Vector3 totalAcc;

                Vector3 pressureAcc = new Vector3(0, 0, 0);
                Vector3 viscosityAcc = new Vector3(0, 0, 0);
                Vector3 gravityAcc = new Vector3(0, -1, 0);
                Vector3 externalAcc = new Vector3(0, 0, 0);

                for(int x = -1; x <= 1; x++) // checking all nearby regions
                {
                    if(reg.x + x < 0 || reg.x + x >= regSizeX) continue;

                    for(int y = -1; y <= 1; y++)
                    {
                        if(reg.y + y < 0 || reg.y + y >= regSizeY) continue;

                        for(int z = -1; z <= 1; z++)
                        {
                            if(reg.z + z < 0 || reg.z + z >= regSizeZ) continue;

                            foreach(int ind in regions[x, y, z]) // checking all other particles in the nearby regions
                            {
                                if(ind == i) continue;
                                
                                Particle partJ = parts[ind];
                                float distance = Vector3.Distance(partI.position, partJ.position);
                                pressureAcc += ((partI.pressure + partJ.pressure) / (2 * partJ.density) * Mathf.Pow(GradKernel(distance), 2)) * (partJ.position - partI.position).normalized;
                                viscosityAcc += ((partJ.velocity - partI.velocity) / partJ.density * GradKernel(distance));

                            }
                        }
                    }
                }
                
                pressureAcc *= gradKernelConstant;
                viscosityAcc *= gradKernelConstant;

                totalAcc = pressureAcc + viscosityAcc + gravityAcc + externalAcc;

                partI.ChangeVelocity(totalAcc, timeInterval);

                Vector3 temp = partI.position;

                partI.ChangePosition(timeInterval, partI.velocity);
                partI.ChangePosition(new Vector3(Mathf.Clamp(partI.position.x, boundStart.x, boundStart.x + boundSize.x), Mathf.Clamp(partI.position.y, boundStart.y, boundStart.y + boundSize.y), Mathf.Clamp(partI.position.z, boundStart.z, boundStart.z + boundSize.z)));          
                ChangeRegion(i, partI.position, partI.region);

                print(partI.position - temp);
                
                partMeshes[i].transform.position = partI.position;
            }

            yield return sec;
        }
    }
}
