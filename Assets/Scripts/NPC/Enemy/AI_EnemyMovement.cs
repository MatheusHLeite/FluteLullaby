using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class AI_EnemyMovement : NetworkBehaviour { 
    [Header("Wandering")]
    public float wanderRadius = 10f;
    public float idleTime = 2f;

    private EnemyState currentState;

    private AI_FOV fov;
    private NavMeshAgent agent;
    private float idleTimer;

    private void Awake() {
        fov = GetComponent<AI_FOV>();
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkSpawn() {
        if (!IsServer) return;

        ChangeState(EnemyState.Idle);
    }

    private void ChangeState(EnemyState newState) {
        currentState = newState;
        idleTimer = 0f;
    }

    private void UpdateIdle() {
        idleTimer += Time.deltaTime;

        if (fov.CurrentTarget != null) {
            ChangeState(EnemyState.Chasing);
            return;
        }

        if (idleTimer >= idleTime)        
            ChangeState(EnemyState.Wandering);        
    }

    private void UpdateWandering() {
        if (fov.CurrentTarget != null) {
            ChangeState(EnemyState.Chasing);
            return;
        }

        if (!agent.hasPath) {
            Vector3 randomPoint = RandomNavSphere(transform.position, wanderRadius);
            agent.SetDestination(randomPoint);
        }
    }

    private void UpdateChasing() {
        if (fov.CurrentTarget == null) {
            ChangeState(EnemyState.Searching);
            agent.SetDestination(fov.LastSeenPosition);
            return;
        }

        agent.SetDestination(fov.CurrentTarget.position);
    }

    private void UpdateSearching()  {
        if (fov.CurrentTarget != null) {
            ChangeState(EnemyState.Chasing);
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance) 
            ChangeState(EnemyState.Idle);        
    }

    Vector3 RandomNavSphere(Vector3 origin, float dist) {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, NavMesh.AllAreas);

        return navHit.position;
    }

    private void Update()  {
        if (!IsServer) return;

        switch (currentState) {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Wandering:
                UpdateWandering();
                break;
            case EnemyState.Chasing:
                UpdateChasing();
                break;
            case EnemyState.Searching:
                UpdateSearching();
                break;
        }
    }
}
