using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyMeleeHitbox meleeHitbox;
    private Health healthScript;
    private AudioSource audioSource;

    [Header("Patrulha")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 0.75f;
    private int _currentPatrolIndex;

    [Header("IA de Combate")]
    [SerializeField] private float sightRange = 15f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2.5f;
    [SerializeField] private float chaseSpeed = 3f;
    [Tooltip("Por quantos segundos a IA vai perseguir um alvo que a atacou de longe, mesmo que ele esteja fora do sightRange.")]
    [SerializeField] private float forcedAggroDuration = 10f;

    [Header("Feedback Sonoro")]
    [SerializeField] private AudioClip attackSound;

    private Transform _targetPlayer;
    private Health _targetPlayerHealth;
    private float _timeOfLastAttack = -10f;
    private PhotonView _photonView;
    
    private enum AIState { Patrolling, Chasing, ReturningToPatrol }
    private AIState _currentState;

    private bool _isStunned = false;
    private bool _isInitialized = false;
    private float _forceAggroUntilTime = 0f;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        agent = GetComponent<NavMeshAgent>();
        healthScript = GetComponent<Health>();
        audioSource = GetComponent<AudioSource>();
    }

    public void Initialize(Transform[] points)
    {
        patrolPoints = points;
        if (PhotonNetwork.IsMasterClient)
        {
            agent.enabled = true;
            enabled = true;
            ChangeState(AIState.Patrolling);
            _isInitialized = true;
        }
        else
        {
            agent.enabled = false;
            enabled = false;
        }
    }

    private void Update()
    {
        if (!_isInitialized) return;

        if (healthScript != null && healthScript.IsDead())
        {
            if (agent.enabled) agent.velocity = Vector3.zero;
            return;
        }

        if (!PhotonNetwork.IsMasterClient || _isStunned) return;

        switch (_currentState)
        {
            case AIState.Patrolling:
                UpdatePatrolState();
                break;
            case AIState.Chasing:
                UpdateChaseAndAttackState();
                break;
            case AIState.ReturningToPatrol:
                UpdateReturningState();
                break;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    private void UpdatePatrolState()
    {
        // Se temos pontos de patrulha, patrulhamos. Senão, apenas procuramos o jogador.
        if (patrolPoints != null && patrolPoints.Length > 0 && !agent.pathPending && agent.remainingDistance < 0.5f)
            GoToNextPatrolPoint();
        
        LookForPlayer();
    }

    private void UpdateChaseAndAttackState()
    {
        if (_targetPlayer == null || (_targetPlayerHealth != null && _targetPlayerHealth.IsDead()))
        {
            ChangeState(AIState.ReturningToPatrol);
            return;
        }

        float distance = Vector3.Distance(transform.position, _targetPlayer.position);
        
        bool isAggroForced = Time.time < _forceAggroUntilTime;
        if (distance > sightRange && !isAggroForced)
        {
            ChangeState(AIState.ReturningToPatrol);
            return;
        }
        
        if (distance <= attackRange && Time.time >= _timeOfLastAttack + attackCooldown)
        {
            agent.isStopped = true;
            transform.LookAt(new Vector3(_targetPlayer.position.x, transform.position.y, _targetPlayer.position.z));
            animator.SetTrigger("Attack");
            if (audioSource != null && attackSound != null) audioSource.PlayOneShot(attackSound);
            _timeOfLastAttack = Time.time;
        }
        else if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(_targetPlayer.position);
        }
        else
        {
            agent.isStopped = true;
        }
    }

    private void UpdateReturningState()
    {
        // Se chegamos perto do nosso ponto de retorno (ou se não temos um), voltamos a patrulhar.
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            ChangeState(AIState.Patrolling);
        }
    }

    private void ChangeState(AIState newState)
    {
        if (_currentState == newState && _isInitialized) return;
        _currentState = newState;

        switch (_currentState)
        {
            case AIState.Patrolling:
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                _targetPlayer = null;
                _targetPlayerHealth = null;
                _forceAggroUntilTime = 0f;
                if (healthScript != null) healthScript.SetOverheadBarActive(false);
                if (patrolPoints != null && patrolPoints.Length > 0) GoToNextPatrolPoint();
                break;

            case AIState.Chasing:
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                if (healthScript != null) healthScript.SetOverheadBarActive(true);
                break;
            
            // --- A GRANDE MUDANÇA ESTÁ AQUI ---
            case AIState.ReturningToPatrol:
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                _targetPlayer = null;
                _targetPlayerHealth = null;
                if (healthScript != null) healthScript.SetOverheadBarActive(false);

                // Se temos um lugar para retornar, vamos para lá.
                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    GoToNextPatrolPoint();
                }
                // Se NÃO temos (caso dos esqueletos), pulamos direto para o estado de patrulha.
                else
                {
                    ChangeState(AIState.Patrolling);
                }
                break;
        }
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        
        if (_currentState != AIState.ReturningToPatrol)
        {
            _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
        }
        agent.destination = patrolPoints[_currentPatrolIndex].position;
    }

    private void LookForPlayer()
    {
        if (_targetPlayer != null) return;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, sightRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                Health playerHealth = hitCollider.GetComponent<Health>();
                if (playerHealth != null && !playerHealth.IsDead())
                {
                    _targetPlayer = hitCollider.transform;
                    _targetPlayerHealth = playerHealth;
                    ChangeState(AIState.Chasing);
                    return;
                }
            }
        }
    }

    public void OnAttackedBy(Photon.Realtime.Player attacker)
    {
        if (!PhotonNetwork.IsMasterClient || _isStunned) return;
        if (_targetPlayer != null && _targetPlayer.GetComponent<PhotonView>().Owner == attacker) return;
        
        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (pc.GetComponent<PhotonView>().Owner == attacker)
            {
                _targetPlayer = pc.transform;
                _targetPlayerHealth = pc.GetComponent<Health>();
                _forceAggroUntilTime = Time.time + forcedAggroDuration;
                ChangeState(AIState.Chasing);
                return;
            }
        }
    }
    
    public void AnimEvent_EnableHitbox() { if (meleeHitbox != null) meleeHitbox.EnableHitbox(); }
    public void AnimEvent_DisableHitbox() { if (meleeHitbox != null) meleeHitbox.DisableHitbox(); }
    public void StunForDuration(float duration) { if (PhotonNetwork.IsMasterClient) StartCoroutine(StunCoroutine(duration)); }
    private IEnumerator StunCoroutine(float duration)
    {
        _isStunned = true;
        agent.isStopped = true;
        if (animator != null) animator.SetFloat("Speed", 0f);
        yield return new WaitForSeconds(duration);
        agent.isStopped = false;
        _isStunned = false;
    }

    public void OnDeath()
    {
        this.enabled = false;
        if (agent != null) agent.enabled = false;
        if (GetComponent<Collider>() != null) GetComponent<Collider>().enabled = false;
    }
}