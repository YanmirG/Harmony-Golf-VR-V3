using System;
using System.Collections.Generic;
using Convai.Scripts.Runtime.Attributes;
using Convai.Scripts.Runtime.Features;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEngine;

namespace Convai.Scripts.Runtime.Core
{
    [DefaultExecutionOrder(-101)]
    public class ConvaiNPCManager : MonoBehaviour
    {
        private static readonly RaycastHit[] RaycastHits = new RaycastHit[1];

        [Tooltip("Length of the ray used for detecting NPCs.")] [SerializeField]
        private float rayLength = 2.0f;

        [Tooltip("Angle from the ray's direction to keep the NPC active, even if not directly hit by the ray.")] [SerializeField]
        private float visionConeAngle = 45f;

        [Tooltip("Reference to the currently active NPC.")] [ReadOnly]
        public ConvaiNPC activeConvaiNPC;

        [Tooltip("Reference to the NPC that is currently near the player.")] [ReadOnly]
        public ConvaiNPC nearbyNPC;

        // Cache used to store NPC references and avoid redundant GetComponent calls.
        private readonly Dictionary<GameObject, ConvaiNPC> _convaiNPCCache = new();

        // Reference to the NPC that was last hit by the raycast.
        private ConvaiNPC _lastHitNpc;

        // Reference to the main camera used for ray casting.
        private Camera _mainCamera;

        // Singleton instance of the NPC manager.
        public static ConvaiNPCManager Instance { get; private set; }

        private void Awake()
        {
            // Singleton pattern to ensure only one instance exists
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            Ray ray = new(_mainCamera.transform.position, _mainCamera.transform.forward);
            bool foundConvaiNPC = false;

            if (Physics.RaycastNonAlloc(ray, RaycastHits, rayLength) > 0)
            {
                RaycastHit hit = RaycastHits[0];

                nearbyNPC = GetConvaiNPC(hit.transform.gameObject);

                if (nearbyNPC != null)
                {
                    foundConvaiNPC = true;

                    if (_lastHitNpc != nearbyNPC && !CheckForNPCToNPCConversation(nearbyNPC))
                    {
                        UpdateActiveNPC(nearbyNPC);
                    }
                }
            }

            if (!foundConvaiNPC && _lastHitNpc != null)
            {
                Vector3 toLastHitNPC = _lastHitNpc.transform.position - ray.origin;
                float angleToLastHitNPC = Vector3.Angle(ray.direction, toLastHitNPC.normalized);
                float distanceToLastHitNPC = toLastHitNPC.magnitude;

                if (angleToLastHitNPC > visionConeAngle || distanceToLastHitNPC > rayLength * 1.2f)
                {
                    ConvaiLogger.DebugLog($"Player left {_lastHitNpc.gameObject.name}", ConvaiLogger.LogCategory.Character);
                    UpdateActiveNPC(null);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera == null)
                return;

            Transform cameraTransform = _mainCamera.transform;
            Vector3 rayOrigin = cameraTransform.position;
            Vector3 rayDirection = cameraTransform.forward;

            // Drawing the main ray
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(rayOrigin, rayDirection.normalized * rayLength);

            if (_lastHitNpc != null) DrawVisionConeArc(rayOrigin, rayDirection, cameraTransform.up);
        }

        private void DrawVisionConeArc(Vector3 rayOrigin, Vector3 rayDirection, Vector3 up)
        {
            const int arcResolution = 50; // number of segments to use for arc
            float angleStep = 2 * visionConeAngle / arcResolution; // angle between each segment

            Vector3 previousPoint = Quaternion.AngleAxis(-visionConeAngle, up) * rayDirection * rayLength;

            for (int i = 1; i <= arcResolution; i++)
            {
                Vector3 nextPoint = Quaternion.AngleAxis(-visionConeAngle + angleStep * i, up) * rayDirection * rayLength;
                Gizmos.DrawLine(rayOrigin + previousPoint, rayOrigin + nextPoint);
                previousPoint = nextPoint;
            }

            Quaternion leftRotation = Quaternion.AngleAxis(-visionConeAngle, up);
            Quaternion rightRotation = Quaternion.AngleAxis(visionConeAngle, up);

            Vector3 leftDirection = leftRotation * rayDirection;
            Vector3 rightDirection = rightRotation * rayDirection;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(rayOrigin, rayOrigin + leftDirection.normalized * rayLength);
            Gizmos.DrawLine(rayOrigin, rayOrigin + rightDirection.normalized * rayLength);
        }

        /// <summary>
        ///     Checks if the specified NPC is in conversation with another NPC.
        /// </summary>
        /// <param name="npc">The NPC to check.</param>
        /// <returns>True if the NPC is in conversation with another NPC; otherwise, false.</returns>
        public bool CheckForNPCToNPCConversation(ConvaiNPC npc)
        {
            return npc.TryGetComponent(out ConvaiGroupNPCController convaiGroupNPC) && convaiGroupNPC.IsInConversationWithAnotherNPC;
        }

        private void UpdateActiveNPC(ConvaiNPC newActiveNPC)
        {
            // Check if the new active NPC is different from the current active NPC.
            if (activeConvaiNPC != newActiveNPC)
            {
                // Deactivate the currently active NPC, if any.
                if (activeConvaiNPC != null) activeConvaiNPC.isCharacterActive = false;

                // Update the reference to the new active NPC.
                activeConvaiNPC = newActiveNPC;
                _lastHitNpc = newActiveNPC; // Ensure the _lastHitNpc reference is updated accordingly.

                // Activate the new NPC, if any.
                if (newActiveNPC != null)
                {
                    newActiveNPC.isCharacterActive = true;
                    ConvaiLogger.DebugLog($"Active NPC changed to {newActiveNPC.gameObject.name}", ConvaiLogger.LogCategory.Character);
                }

                // Invoke the OnActiveNPCChanged event, notifying other parts of the system of the change.
                OnActiveNPCChanged?.Invoke(newActiveNPC);
            }
        }

        /// <summary>
        ///     Sets the active NPC to the specified NPC.
        /// </summary>
        /// <param name="newActiveNPC">The NPC to set as active.</param>
        /// <param name="updateLastHitNPC"> Whether to update the last hit NPC reference.</param>
        public void SetActiveConvaiNPC(ConvaiNPC newActiveNPC, bool updateLastHitNPC = true)
        {
            if (activeConvaiNPC != newActiveNPC)
            {
                if (activeConvaiNPC != null)
                    // Deactivate the previous NPC
                    activeConvaiNPC.isCharacterActive = false;

                activeConvaiNPC = newActiveNPC;
                if (updateLastHitNPC)
                    _lastHitNpc = newActiveNPC;

                if (newActiveNPC != null)
                {
                    // Activate the new NPC
                    newActiveNPC.isCharacterActive = true;
                    ConvaiLogger.DebugLog($"Active NPC changed to {newActiveNPC.gameObject.name}", ConvaiLogger.LogCategory.Character);
                }

                OnActiveNPCChanged?.Invoke(newActiveNPC);
            }
        }

        /// <summary>
        ///     Event that's triggered when the active NPC changes.
        /// </summary>
        public event Action<ConvaiNPC> OnActiveNPCChanged;

        private ConvaiNPC GetConvaiNPC(GameObject obj)
        {
            if (!_convaiNPCCache.TryGetValue(obj, out ConvaiNPC npc))
            {
                npc = obj.GetComponent<ConvaiNPC>();
                if (npc != null)
                    _convaiNPCCache[obj] = npc;
            }

            return npc;
        }

        /// <summary>
        ///     Gets the currently active ConvaiNPC.
        /// </summary>
        /// <returns>The currently active ConvaiNPC.</returns>
        public ConvaiNPC GetActiveConvaiNPC()
        {
            return activeConvaiNPC;
        }
    }
}