// Path: Assets/Scripts/Combat/Helpers/DynamicHover.cs
using UnityEngine;

public class DynamicHover : MonoBehaviour
{
    [Header("Hover (Bobbing)")]
    [SerializeField] private bool enableHover = true;
    [SerializeField] private float hoverAmplitude = 0.1f;
    [SerializeField] private float hoverSpeed = 1.5f;

    [Header("Orbit")]
    [SerializeField] private bool enableOrbit = false;
    [SerializeField] private Transform orbitCenter;
    [SerializeField] private float orbitRadius = 0.3f;
    [SerializeField] private float orbitSpeed = 2f;

    [Header("Spin")]
    [SerializeField] private bool enableSpin = true;
    [SerializeField] private Vector3 spinAxis = Vector3.up;
    [SerializeField] private float spinSpeed = 180f;

    private Vector3 _initialLocalPosition;
    private float _timeOffset;

    private void Awake()
    {
        _initialLocalPosition = transform.localPosition;
        _timeOffset = Random.Range(0f, 10f); // Đảm bảo các object không lơ lửng đồng bộ
    }

    // Sử dụng LateUpdate để không ảnh hưởng đến logic di chuyển trong Update
    private void LateUpdate()
    {
        Vector3 finalOffset = Vector3.zero;

        if (enableHover)
        {
            float hoverOffset = Mathf.Sin((Time.time + _timeOffset) * hoverSpeed) * hoverAmplitude;
            finalOffset.y = hoverOffset;
        }

        transform.localPosition = _initialLocalPosition + finalOffset;

        if (enableOrbit && orbitCenter != null)
        {
            transform.RotateAround(orbitCenter.position, spinAxis, orbitSpeed * Time.deltaTime);
        }

        if (enableSpin)
        {
            transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
        }
    }

    public void SetOrbitCenter(Transform center)
    {
        orbitCenter = center;
    }
}