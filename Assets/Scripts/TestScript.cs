using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public struct Test{
        public int x;

        public Test(int x)
        {
            this.x = x;
        }

        public void TestFunc()
        {
            this.x = 4;
        }
    };

    Test t;

    void Start()
    {
        t.TestFunc();
        print(t.x);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
