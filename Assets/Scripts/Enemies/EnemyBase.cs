// Path: Assets/Scripts/Enemies/Core/EnemyBase.cs
using UnityEngine;
using UnityEngine.AI;

// BỎ REQUIRE COMPONENT RIGIDBODY VÌ NAVMESHAGENT SẼ TỰ XỬ LÝ
[RequireComponent(typeof(NavMeshAgent), typeof(EnemyHealth))]
public abstract class EnemyBase : MonoBehaviour
{
    protected enum EnemyState
    {
        Idle,
        Chasing,
        Attacking,
        Kiting,
        Dead
    }

    [SerializeField] protected EnemyData enemyData;
    [SerializeField] protected Transform projectileSpawnPoint;

    // PHẦN QUAN TRỌNG NHẤT: THAM CHIẾU TỚI OBJECT CON
    [Tooltip("Kéo object con chứa MeshRenderer và VAT_Animator vào đây.")]
    [SerializeField] private Transform visualModel;

    protected NavMeshAgent agent;
    protected VAT_Animator vatAnimator;
    protected EnemyHealth enemyHealth;
    protected Transform playerTarget;

    protected EnemyState currentState;
    protected float attackCooldownTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();

        if (visualModel == null)
        {
            Debug.LogError($"LỖI NGHIÊM TRỌNG: 'Visual Model' chưa được gán trên prefab {gameObject.name}! Kéo object con chứa model vào slot này.", this);
            this.enabled = false; // Vô hiệu hóa script để tránh lỗi liên tục
            return;
        }

        vatAnimator = visualModel.GetComponent<VAT_Animator>();
        if (vatAnimator == null)
        {
            Debug.LogError($"LỖI NGHIÊM TRỌNG: Object 'Visual Model' của prefab {gameObject.name} không chứa component VAT_Animator!", this);
            this.enabled = false;
        }
    }

    protected virtual void Start()
    {
        InitializeFromData();
        ApplyVisualOffset();
        enemyHealth.Initialize(enemyData.maxHealth, this);
        enemyHealth.OnDamaged += HandleDamageTaken;
        enemyHealth.OnDeath += HandleDeath;
        SwitchState(EnemyState.Idle);
    }

    private void ApplyVisualOffset()
    {
        // Hàm này giờ đã an toàn vì Awake() đã kiểm tra null
        visualModel.localEulerAngles = enemyData.visualRotationOffset;
    }

    // ... (Toàn bộ các hàm còn lại giữ nguyên, chúng đã được thiết kế đúng cho kiến trúc Cha-Con) ...
    protected virtual void Update()
    {
        if (currentState == EnemyState.Dead) return;

        attackCooldownTimer -= Time.deltaTime;
        playerTarget = FindPlayerTarget();

        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdleState();
                break;
            case EnemyState.Chasing:
                UpdateChasingState();
                break;
            case EnemyState.Attacking:
                UpdateAttackingState();
                break;
            case EnemyState.Kiting:
                UpdateKitingState();
                break;
        }
    }

    protected void SwitchState(EnemyState newState)
    {
        if (currentState == newState) return;
        currentState = newState;

        switch (currentState)
        {
            case EnemyState.Idle:
                vatAnimator.CrossFade(enemyData.animIdle, 0.2f);
                agent.ResetPath();
                break;
            case EnemyState.Chasing:
                vatAnimator.CrossFade(enemyData.animMove, 0.2f);
                break;
            case EnemyState.Attacking:
                agent.ResetPath();
                break;
            case EnemyState.Kiting:
                vatAnimator.CrossFade(enemyData.animMove, 0.2f);
                break;
            case EnemyState.Dead:
                vatAnimator.CrossFade(enemyData.animDie, 0.1f);
                agent.enabled = false;
                GetComponent<Collider>().enabled = false;
                Destroy(gameObject, 5f);
                break;
        }
    }

    protected virtual void UpdateIdleState()
    {
        if (playerTarget != null)
        {
            SwitchState(EnemyState.Chasing);
        }
    }

    protected virtual void UpdateChasingState()
    {
        if (playerTarget == null)
        {
            SwitchState(EnemyState.Idle);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (ShouldKite())
        {
            SwitchState(EnemyState.Kiting);
            return;
        }

        if (distanceToPlayer <= enemyData.attackRange)
        {
            SwitchState(EnemyState.Attacking);
        }
        else
        {
            agent.SetDestination(playerTarget.position);
        }
    }

    protected abstract void UpdateAttackingState();

    protected virtual void UpdateKitingState()
    {
        if (playerTarget == null)
        {
            SwitchState(EnemyState.Idle);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer > enemyData.kitingDistance + 1f)
        {
            SwitchState(EnemyState.Chasing);
            return;
        }

        Vector3 directionAwayFromPlayer = (transform.position - playerTarget.position).normalized;
        Vector3 kiteDestination = transform.position + directionAwayFromPlayer * 2f;
        agent.SetDestination(kiteDestination);
    }

    private void InitializeFromData()
    {
        agent.speed = enemyData.movementSpeed;
        agent.angularSpeed = enemyData.angularSpeed;
        agent.stoppingDistance = enemyData.stoppingDistance;
    }

    private Transform FindPlayerTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, enemyData.detectionRadius, enemyData.playerLayer);
        if (colliders.Length > 0)
        {
            return colliders[0].transform;
        }
        return null;
    }

    private void HandleDamageTaken(Vector3 damageSource)
    {
        if (currentState != EnemyState.Dead)
        {
            vatAnimator.CrossFade(enemyData.animTakeDamage, 0.1f);
        }
    }

    private void HandleDeath()
    {
        SwitchState(EnemyState.Dead);
    }

    protected void FaceTarget()
    {
        if (playerTarget == null) return;
        Vector3 direction = (playerTarget.position - transform.position).normalized;
        if (direction == Vector3.zero) return;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * agent.angularSpeed * 2);
    }

    protected bool IsReadyToAttack()
    {
        return attackCooldownTimer <= 0;
    }

    protected bool ShouldKite()
    {
        if (enemyData.projectilePrefab == null || playerTarget == null) return false;
        return Vector3.Distance(transform.position, playerTarget.position) < enemyData.kitingDistance;
    }
}