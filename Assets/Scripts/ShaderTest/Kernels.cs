using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Kernels
{
    public static float smoothingLength{get;set;}

    public static float particleMass{get;set;}
    public static float surfaceTension{get;set;}

    public static float Poly6KernelConstant;
    public static float Poly6GradKernelConstant;
    public static float SpikyKernelConstant;
    public static float SpikyGradKernelConstant;
    public static float SpikyGradSquaredKernelConstant;
    public static float ViscosityLaplaceKernelConstant;
    public static float SurfaceTensionConstant;
    public static float SurfaceTensionOffset;

    public static void Init()
    {
        Poly6KernelConstant = (365) / (64 * Mathf.PI * Mathf.Pow(smoothingLength, 9));
        Poly6GradKernelConstant = -945 / (32 * Mathf.PI * Mathf.Pow(smoothingLength, 9));
        SpikyKernelConstant = (15) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));
        SpikyGradKernelConstant = (-45) / (Mathf.PI * Mathf.Pow(smoothingLength, 6));
        SpikyGradSquaredKernelConstant = -90/(Mathf.PI * Mathf.Pow(smoothingLength, 6));

        SurfaceTensionConstant = 32 / (Mathf.PI * Mathf.Pow(smoothingLength, 6)) * surfaceTension * Mathf.Sqrt(particleMass);
        SurfaceTensionOffset = Mathf.Pow(smoothingLength, 6) / 64;
    }

    public static float Poly6(float distance)
    {
        return Mathf.Pow(Mathf.Pow(smoothingLength, 2) - Mathf.Pow(distance, 2), 3);
    }

    public static Vector3 Poly6Grad(Vector3 distance)
    {
        return Mathf.Pow(Mathf.Pow(smoothingLength, 2) - Vector3.Dot(distance, distance), 2) * distance;
    }

    public static Vector3 SpikyGrad(Vector3 distance)
    {
        return Mathf.Pow(smoothingLength - distance.magnitude, 2) * distance / distance.magnitude;
    }

    public static float SpikyGradSquared(float distance)
    {
        //return SpikyKernelConstant * (smoothingLength - distance);
        return SpikyGradSquaredKernelConstant * 1/distance * Mathf.Pow(smoothingLength-distance, 2) - (smoothingLength-distance);
    }    

    public static float ViscosityLaplace(float distance)
    {
        return (smoothingLength - distance);
    }

    public static float SurfaceTension(float distance)
    {
        if(distance < smoothingLength / 2)
            return 2 * Mathf.Pow(smoothingLength - distance, 3) * Mathf.Pow(distance, 3) + SurfaceTensionOffset;
        else
            return Mathf.Pow(smoothingLength - distance, 3) * Mathf.Pow(distance, 3);
    }
}
