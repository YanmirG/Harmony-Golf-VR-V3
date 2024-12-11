using System;
using Convai.Scripts.Runtime.Core;
using UnityEngine;

namespace Convai.Demo.Scripts
{
    public class NPCBehaviorController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed;
        [SerializeField] private float targetDistanceThreshold = 0.1f;
        [SerializeField] private float rotationSpeed = 5f; // Speed at which the NPC turns
        private Animator _animator;
        private ConvaiHeadTracking _convaiHeadTracking;

        private State _currentState;
        private Transform _currentTarget;
        private Quaternion _originalRotation;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _convaiHeadTracking = GetComponent<ConvaiHeadTracking>();
        }

        private void Start()
        {
            _currentState = State.Idle;
            _originalRotation = transform.rotation; // Store the original rotation
        }

        private void Update()
        {
            switch (_currentState)
            {
                case State.Move:
                    Move();
                    break;
                case State.Idle:
                    break;
            }
        }

        public void UpdateCurrentTarget(Transform newTarget)
        {
            _currentTarget = newTarget;
        }

        public void UpdateState(int newState)
        {
            UpdateState((State)newState);
        }

        private void UpdateState(State newState)
        {
            _currentState = newState;
            PerformPostUpdateStateActions();
        }

        private void PerformPostUpdateStateActions()
        {
            switch (_currentState)
            {
                case State.Move:
                    _animator.CrossFade(Animator.StringToHash("Walking"), 0.01f);
                    _convaiHeadTracking.SetActionRunning(true);
                    break;
                case State.Idle:
                    _animator.CrossFade(Animator.StringToHash("Idle"), 0.01f);
                    _convaiHeadTracking.SetActionRunning(false);
                    transform.rotation = Quaternion.Slerp(transform.rotation, _originalRotation, Time.deltaTime * rotationSpeed); // Rotate back to original direction
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Move()
        {
            if (Vector3.Distance(transform.position, _currentTarget.position) < targetDistanceThreshold)
            {
                UpdateState(State.Idle);
                return;
            }

            Vector3 direction = (_currentTarget.position - transform.position).normalized;
            direction.y = 0;

            // Rotate towards the movement direction
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }

            transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);
        }

        private enum State
        {
            Move = 0,
            Idle = 1
        }
    }
}