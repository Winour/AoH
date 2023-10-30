using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class CanvasCameraSetter : MonoBehaviour
{
    private void OnEnable()
    {
        this.GetComponent<Canvas>().worldCamera = Camera.main;
    }
}
