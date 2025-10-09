using BrushHit;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using System.Text;
using System.Linq;

public class PlayerPivotController : SerializedMonoBehaviour
{
    [Title("Sự kiện")]
    [InfoBox("Kéo các đối tượng lắng nghe sự kiện thay đổi Pivot vào đây.")]
    public UnityEvent<Transform> OnPivotSwitched;

    [Title("Thiết lập cốt lõe")]
    [Required][SerializeField] private Transform modelToRotate;
    [Required][SerializeField] private Transform pivotA;
    [Required][SerializeField] private Transform pivotB;

    [Title("Thông số di chuyển")]
    [Range(50f, 500f)][SerializeField] private float rotationSpeed = 200f;
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;

    [Title("Thiết lập kiểm tra mặt đất")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask dangerAreaLayer;
    [SerializeField] private float groundCheckDistance = 100f;

    private Transform currentPivot;
    private bool isMovementActive = true;

    // --- THÊM MỚI ---
    // Thuộc tính công khai để các script khác có thể truy cập an toàn
    public Transform CurrentPivot => currentPivot;

    private enum PivotSafetyStatus { Safe, Unsafe, NoSurface }

    private void Awake()
    {
        if (!AreCoreReferencesValid())
        {
            isMovementActive = false;
            enabled = false;
            return;
        }
        InitializePivot();
    }

    private void Start()
    {
        BroadcastCurrentPivot();
    }

    private void Update()
    {
        if (!isMovementActive || GameManager.Instance.CurrentState != GameState.Playing) return;

        HandleInput();
        ApplyRotation();

#if UNITY_EDITOR
        UpdateGizmoData();
#endif
    }

    private bool AreCoreReferencesValid()
    {
        if (modelToRotate != null && pivotA != null && pivotB != null) return true;
        Debug.LogError("Một hoặc nhiều Transform tham chiếu cốt lõe chưa được gán.", this);
        return false;
    }

    private void InitializePivot()
    {
        currentPivot = pivotA;
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            AttemptPivotSwitch();
        }
    }

    private void AttemptPivotSwitch()
    {
        Transform targetPivot = GetNextPivot();
        PivotSafetyStatus safetyStatus = PerformGroundCheck(targetPivot.position);

        if (safetyStatus == PivotSafetyStatus.Safe)
        {
            currentPivot = targetPivot;
            BroadcastCurrentPivot();
            AudioManager.Instance?.PlaySound("PivotSwitch");
        }
        else
        {
            var debugReport = new StringBuilder();
            debugReport.AppendLine($"<b>[PivotCheck]</b> Attempting switch to '{targetPivot.name}' at {targetPivot.position}.");
            debugReport.AppendLine($"<color=cyan>Safety Status -> {safetyStatus}</color>");
            debugReport.AppendLine("<color=red>Decision: Switch is UNSAFE. Triggering Game Over.</color>");
            string reason = safetyStatus == PivotSafetyStatus.NoSurface
                ? "<b>Reason: No 'Ground' or 'Danger' layer was detected beneath the target pivot.</b>"
                : "<b>Reason: The closest surface detected was on the 'Danger' layer.</b>";
            debugReport.AppendLine(reason);
            Debug.LogError(debugReport.ToString(), this);

            GameManager.Instance?.TriggerGameOver();
        }
    }

    private PivotSafetyStatus PerformGroundCheck(Vector3 origin)
    {
        LayerMask combinedMask = groundLayer | dangerAreaLayer;
        RaycastHit[] allHits = Physics.RaycastAll(origin, Vector3.down, groundCheckDistance, combinedMask)
                                      .OrderBy(h => h.distance)
                                      .ToArray();

        if (allHits.Length == 0) return PivotSafetyStatus.NoSurface;

        RaycastHit closestHit = allHits[0];
        int closestHitLayer = closestHit.collider.gameObject.layer;

        return IsLayerInMask(closestHitLayer, groundLayer) ? PivotSafetyStatus.Safe : PivotSafetyStatus.Unsafe;
    }

    private Transform GetNextPivot()
    {
        return (currentPivot == pivotB) ? pivotA : pivotB;
    }

    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) > 0;
    }

    private void BroadcastCurrentPivot()
    {
        OnPivotSwitched?.Invoke(currentPivot);
    }

    private void ApplyRotation()
    {
        modelToRotate.RotateAround(currentPivot.position, rotationAxis, rotationSpeed * Time.deltaTime);
    }

    public void StopMovement() => isMovementActive = false;
    public void ResumeMovement() => isMovementActive = true;
    public void SetSpeed(float newSpeed) => rotationSpeed = Mathf.Max(0, newSpeed);

#if UNITY_EDITOR
    [Title("Debug Visuals")]
    [SerializeField] private bool enableGizmos = true;
    [ShowIf("enableGizmos")][SerializeField] private Color pivotAColor = Color.cyan;
    [ShowIf("enableGizmos")][SerializeField] private Color pivotBColor = Color.magenta;
    [ShowIf("enableGizmos")][SerializeField] private Color activePivotColor = Color.yellow;
    [ShowIf("enableGizmos")][SerializeField] private Color safeRaycastColor = Color.green;
    [ShowIf("enableGizmos")][SerializeField] private Color unsafeRaycastColor = Color.red;

    private bool isNextPivotSafeForGizmo;

    private void UpdateGizmoData()
    {
        if (!enableGizmos || !Application.isPlaying || currentPivot == null) return;
        Transform nextPivot = GetNextPivot();
        if (nextPivot != null)
        {
            isNextPivotSafeForGizmo = PerformGroundCheck(nextPivot.position) == PivotSafetyStatus.Safe;
        }
    }

    private void OnDrawGizmos()
    {
        if (!enableGizmos) return;

        if (AreCoreReferencesValid())
        {
            DrawPivotGizmo(pivotA, pivotAColor, "A");
            DrawPivotGizmo(pivotB, pivotBColor, "B");
            if (currentPivot != null)
            {
                Gizmos.color = activePivotColor;
                float activeRadius = Vector3.Distance(modelToRotate.position, currentPivot.position);
                DrawWireDisk(currentPivot.position, rotationAxis, activeRadius);
            }
        }

        if (Application.isPlaying)
        {
            DrawDebugRaycastForNextPivot();
        }
    }

    private void DrawDebugRaycastForNextPivot()
    {
        if (currentPivot == null) return;
        Transform nextPivot = GetNextPivot();
        if (nextPivot == null) return;

        Gizmos.color = isNextPivotSafeForGizmo ? safeRaycastColor : unsafeRaycastColor;
        Gizmos.DrawLine(nextPivot.position, nextPivot.position + Vector3.down * groundCheckDistance);
    }

    private void DrawPivotGizmo(Transform pivot, Color color, string label)
    {
        if (pivot == null) return;
        Gizmos.color = color;
        Gizmos.DrawWireSphere(pivot.position, 0.2f);
        UnityEditor.Handles.Label(pivot.position + Vector3.up * 0.3f, $"Pivot {label}");
        if (modelToRotate != null)
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.1f);
            float radius = Vector3.Distance(modelToRotate.position, pivot.position);
            DrawWireDisk(pivot.position, rotationAxis, radius);
        }
    }

    private void DrawWireDisk(Vector3 position, Vector3 axis, float radius)
    {
        UnityEditor.Handles.color = Gizmos.color;
        UnityEditor.Handles.DrawWireDisc(position, axis, radius);
    }
#endif
}