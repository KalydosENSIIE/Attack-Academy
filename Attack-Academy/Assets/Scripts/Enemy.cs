using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NavMeshAgent))]

public class Enemy : Entity
{
    private NavMeshAgent agent;

    [SerializeField]
    float attackTimer;
    [SerializeField]
    float minDistancePathfinding;
    [SerializeField]
    float minTargetDistance;
    [SerializeField]
    float maxTargetDistance;
    [SerializeField]
    float attackDistance;
    [SerializeField]
    float obstructionDistance;
    [SerializeField]
    float maxRayCastDistance;
    [SerializeField]
    int numberCircleCast;

    private float lastAttackTime;
    Utility.EnemyState currentState;
    private Transform playerTransform;
    private float distanceToPlayer;
    private Vector2 lastMovedDirection;
    private bool firstTimeCircling = true;
    private Dictionary<Vector2, float> directionScore = new Dictionary<Vector2, float>();

    private void Awake()
    {
        lastAttackTime = Time.time;
        playerTransform = Player.Instance?.transform;
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    private void Update()
    {
        if(playerTransform == null)
        {
            playerTransform = Player.Instance?.transform;
            if (playerTransform == null)
                return;
        }

        distanceToPlayer = Vector2.Distance(playerTransform.position, this.transform.position);

        if (Time.time > lastAttackTime + attackTimer && distanceToPlayer < attackDistance)
        {
            currentState = Utility.EnemyState.Attack;
        }
        else if(Time.time > lastAttackTime + attackTimer && distanceToPlayer > attackDistance)
        {
            currentState = Utility.EnemyState.ApprochToAttack;
        }
        else
        {
            currentState = Utility.EnemyState.StayAtDistance;
        }

        if(currentState == Utility.EnemyState.Attack)
        {
            firstTimeCircling = true;
            Attack();
        }
        else if(currentState == Utility.EnemyState.ApprochToAttack)
        {
            firstTimeCircling = true;
            if(distanceToPlayer > minDistancePathfinding)
            {
                MoveWithPathFinding();
            }
            else
            {
                MoveWithDesiredDirection((playerTransform.position - this.transform.position).normalized);
            }
        }
        else if(currentState == Utility.EnemyState.StayAtDistance)
        {
            if (distanceToPlayer > minDistancePathfinding)
            {
                MoveWithPathFinding();
                firstTimeCircling = true;
            }
            else if(distanceToPlayer > maxTargetDistance)
            {
                MoveWithDesiredDirection((playerTransform.position - this.transform.position).normalized);
                firstTimeCircling = true;
            }
            else if (distanceToPlayer < minTargetDistance)
            {
                MoveWithDesiredDirection((this.transform.position - playerTransform.position).normalized);
                firstTimeCircling = true;
            }
            else
            {
                if (firstTimeCircling)
                {
                    int sign = Random.Range(1, 3);
                    if(sign % 2 == 0)
                    {
                        MoveWithDesiredDirection(Quaternion.Euler(0, 0, 90) * (playerTransform.position - this.transform.position).normalized);
                    }
                    else if(sign % 2 == 1)
                    {
                        MoveWithDesiredDirection(Quaternion.Euler(0, 0, -90) * (playerTransform.position - this.transform.position).normalized);
                    }
                    firstTimeCircling = false;
                }
                else
                {
                    if(Vector3.Dot(Quaternion.Euler(0, 0, 90) * (playerTransform.position - this.transform.position).normalized, lastMovedDirection.normalized) >= 0)
                    {
                        MoveWithDesiredDirection(Quaternion.Euler(0, 0, 90) * (playerTransform.position - this.transform.position).normalized);
                    }
                    else
                    {
                        MoveWithDesiredDirection(Quaternion.Euler(0, 0, -90) * (playerTransform.position - this.transform.position).normalized);
                    }
                }
            }
        }
    }

    private void Attack()
    {
        lastAttackTime = Time.time;
    }

    private void MoveWithPathFinding()
    {
        agent.SetDestination(Player.Instance.transform.position);
    }

    private void MoveWithDesiredDirection(Vector2 desiredDirection)
    {
        directionScore.Clear();
        for(int i = 0; i < numberCircleCast; i++)
        {
            Vector2 vectorToAdd = Quaternion.Euler(0, 0, 360f * i / numberCircleCast) * desiredDirection;
            float scoreToAdd = Mathf.Cos(360f * i / numberCircleCast);

            RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.5f, vectorToAdd, maxRayCastDistance);
            Debug.DrawLine(transform.position, transform.position + (new Vector3(vectorToAdd.x, vectorToAdd.y, 0) * maxRayCastDistance));

            if(hit.collider == null || hit.distance > obstructionDistance)
            {
                directionScore.Add(vectorToAdd, scoreToAdd);
            }
        }

        Vector2 bestDirection = desiredDirection;
        float bestScore = -1;
        foreach(KeyValuePair<Vector2, float> pair in directionScore)
        {
            if(pair.Value > bestScore)
            {
                bestDirection = pair.Key;
                bestScore = pair.Value;
            }
        }

        this.transform.position = this.transform.position + (new Vector3(bestDirection.x, bestDirection.y, 0) * Time.deltaTime * movementSpeed);
        lastMovedDirection = bestDirection;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, minDistancePathfinding);
    }
}
