using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform _cameraTransform;

    private void Awake()
    {
        _cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        this.transform.LookAt(this.transform.position - (_cameraTransform.position - this.transform.position - (_cameraTransform.transform.forward * 500f)));
    }
}
