using UnityEngine;

public class SimpleOrbit : MonoBehaviour
{
    [SerializeField] private Transform targetPivot;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private bool selfRotation = false;

    private void Update()
    {
        if (selfRotation)
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
        }
        else if (targetPivot != null)
        {
            transform.RotateAround(targetPivot.position, rotationAxis, rotationSpeed * Time.deltaTime);
        }
    }
}