using UnityEngine;
using UnityEngine.AI;

namespace EscapeTheDark
{
    public sealed class EnemyHunter : MonoBehaviour
    {
        private enum HuntState { Patrol, Investigate, Chase }
        public bool IsChasing => state == HuntState.Chase;
        private HuntState state;
        private NavMeshAgent agent;
        private PlayerMotor player;
        private Vector3[] patrol;
        private Vector3 lastKnownPosition;
        private int patrolIndex;
        private float lastSenseTime;
        private float nextDestinationUpdate;
        private float revealUntil;
        private float nextStepTime;
        private AudioSource footstepSource;
        private AudioClip[] walkSteps;
        private AudioClip[] runSteps;
        private bool suspended;
        private float suspendedAt;

        public void SetSuspended(bool value)
        {
            if (suspended == value) return;
            suspended = value;
            if (value) suspendedAt = Time.time;
            else
            {
                float elapsed = Time.time - suspendedAt;
                revealUntil += elapsed;
                lastSenseTime += elapsed;
                nextDestinationUpdate += elapsed;
                nextStepTime += elapsed;
            }
            if (agent && agent.isOnNavMesh) agent.isStopped = value || Time.time < revealUntil;
        }

        public void Configure(NavMeshAgent navAgent, PlayerMotor target, Vector3[] patrolPoints, float revealSeconds = 0f)
        {
            agent = navAgent;
            player = target;
            patrol = patrolPoints;
            revealUntil = Time.time + revealSeconds;
            state = HuntState.Patrol;
            if (!agent.isOnNavMesh) return;
            agent.isStopped = revealSeconds > 0;
            if (!agent.isStopped) agent.SetDestination(patrol[0]);
        }

        public void SetFootsteps(AudioClip[] walking, AudioClip[] running)
        {
            walkSteps = walking;
            runSteps = running;
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.spatialBlend = 1f;
            footstepSource.minDistance = 1.2f;
            footstepSource.maxDistance = 12f;
            footstepSource.volume = 0.34f;
        }

        private void Update()
        {
            if (!agent || !player || !GameDirector.Instance || !GameDirector.Instance.IsPlaying) return;
            if (!agent.isOnNavMesh)
            {
                if (!NavMesh.SamplePosition(transform.position, out NavMeshHit nearest, 2f, NavMesh.AllAreas)) return;
                agent.Warp(nearest.position);
                if (!agent.isOnNavMesh) return;
            }
            if (Time.time < revealUntil) return;
            if (agent.isStopped) agent.isStopped = false;
            Vector3 delta = player.transform.position - transform.position;
            float distance = delta.magnitude;
            bool clearPath = HasLineOfSight();
            float sightRange = player.IsFlashlightOn ? 12f : 5.2f;
            bool facingPlayer = distance < 2.5f || Vector3.Dot(transform.forward, delta.normalized) > 0.54f;
            bool seesPlayer = distance < sightRange && facingPlayer && clearPath;
            bool hearsPlayer = (player.IsSprinting && distance < 8f) ||
                               (player.IsMoving && distance < 2.4f);
            if (seesPlayer)
            {
                state = HuntState.Chase;
                lastKnownPosition = player.transform.position;
                lastSenseTime = Time.time;
            }
            else if (hearsPlayer)
            {
                lastKnownPosition = player.transform.position;
                lastSenseTime = Time.time;
                if (state != HuntState.Chase) state = HuntState.Investigate;
            }
            else if (state == HuntState.Chase && Time.time - lastSenseTime > 2f)
                state = HuntState.Investigate;
            if (state == HuntState.Investigate && Time.time - lastSenseTime > 7f)
                state = HuntState.Patrol;

            agent.speed = state == HuntState.Chase ? 2.75f : state == HuntState.Investigate ? 1.85f : 1.35f;
            agent.acceleration = state == HuntState.Chase ? 10f : 5f;
            if (Time.time >= nextDestinationUpdate)
            {
                nextDestinationUpdate = Time.time + 0.35f;
                if (state == HuntState.Patrol)
                {
                    if (!agent.pathPending && agent.remainingDistance < 0.9f)
                        patrolIndex = (patrolIndex + 1) % patrol.Length;
                    agent.SetDestination(patrol[patrolIndex]);
                }
                else agent.SetDestination(lastKnownPosition);
            }

            if (footstepSource && agent.velocity.sqrMagnitude > 0.4f && Time.time >= nextStepTime)
            {
                AudioClip[] steps = state == HuntState.Chase ? runSteps : walkSteps;
                if (steps != null && steps.Length > 0)
                {
                    AudioClip step = steps[Random.Range(0, steps.Length)];
                    if (step) footstepSource.PlayOneShot(step);
                }
                nextStepTime = Time.time + (state == HuntState.Chase ? 0.34f : 0.58f);
            }

            if (distance < 1.05f && clearPath) GameDirector.Instance.Caught();
        }

        private bool HasLineOfSight()
        {
            Vector3 eye = transform.position + Vector3.up * 1.45f;
            Vector3 target = player.transform.position + Vector3.up * 1.35f;
            Vector3 direction = target - eye;
            if (!Physics.Raycast(eye, direction.normalized, out RaycastHit hit, direction.magnitude,
                    ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.collider.GetComponentInParent<PlayerMotor>() == player;
        }
    }
}
