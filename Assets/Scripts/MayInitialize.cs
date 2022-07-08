using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MayInitialize : MonoBehaviour
{
    private bool initialized;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (!initialized)
        {
            OnInitialize();
            initialized = true;
        }
    }

    protected abstract void OnInitialize();
}
