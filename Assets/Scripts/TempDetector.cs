using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TempDetector : MonoBehaviour
{
    [SerializeField] private int n;

    private float total;
    private int num;

    void Start()
    {
        StartCoroutine(ShowTemp());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateTemp(int numChange, float totalChange)
    {
        num += numChange;
        total += totalChange;
    }

    private IEnumerator ShowTemp()
    {
        WaitForSeconds sec = new WaitForSeconds(1);

        while(true)
        {
            print(n + ": " + (total/num) + ", " + Time.time);
            yield return sec;
        }
    }
}
