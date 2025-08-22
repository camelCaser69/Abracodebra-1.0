using UnityEngine;
using UnityEngine.U2D;
using WegoSystem;

namespace WegoSystem
{
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class CameraController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private MapConfiguration mapConfig;
        [SerializeField] public Transform followTarget;
        [SerializeField] private Camera cam;

        [Header("Follow Settings")]
        [Tooltip("If enabled, the camera will smoothly follow the target. Disabled by default.")]
        [SerializeField] private bool enableFollow = false;
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private Vector2 offset = Vector2.zero;

        [Header("Boundaries")]
        [SerializeField] private bool constrainToMap = true;
        [SerializeField] private float boundaryPadding = 2f; // Tiles from edge

        private PixelPerfectCamera pixelPerfectCam;
        private Vector2 minBounds, maxBounds;

        private void Awake()
        {
            if (cam == null) cam = GetComponent<Camera>();
            pixelPerfectCam = GetComponent<PixelPerfectCamera>();

            if (mapConfig == null)
            {
                Debug.LogError("[CameraController] MapConfiguration is not assigned! Camera cannot calculate bounds.", this);
                enabled = false;
            }
        }

        private void Start()
        {
            CalculateCameraBounds();

            // Auto-find player if no target set
            if (followTarget == null)
            {
                var player = FindFirstObjectByType<GardenerController>();
                if (player != null)
                {
                    followTarget = player.transform;
                    Debug.Log("[CameraController] Follow target not set, automatically assigned to GardenerController.", this);
                }
            }
        }

        private void LateUpdate()
        {
            if (!enableFollow || followTarget == null || mapConfig == null) return;

            Vector3 desiredPosition = followTarget.position + (Vector3)offset;
            desiredPosition.z = transform.position.z; // Keep Z unchanged

            // Constrain to map bounds
            if (constrainToMap)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
            }

            // Smooth movement
            Vector3 smoothedPosition = Vector3.Lerp(
                transform.position,
                desiredPosition,
                smoothSpeed * Time.deltaTime
            );

            // Pixel-perfect snapping (if enabled)
            if (pixelPerfectCam != null && pixelPerfectCam.enabled)
            {
                float pixelSize = 1f / pixelPerfectCam.assetsPPU;
                smoothedPosition.x = Mathf.Round(smoothedPosition.x / pixelSize) * pixelSize;
                smoothedPosition.y = Mathf.Round(smoothedPosition.y / pixelSize) * pixelSize;
            }

            transform.position = smoothedPosition;
        }

        private void CalculateCameraBounds()
        {
            if (mapConfig == null) return;

            float height = cam.orthographicSize * 2;
            float width = height * cam.aspect;

            // Calculate bounds that keep entire camera view within map
            minBounds.x = (width / 2f) + boundaryPadding;
            maxBounds.x = mapConfig.mapSize.x - (width / 2f) - boundaryPadding;
            minBounds.y = (height / 2f) + boundaryPadding;
            maxBounds.y = mapConfig.mapSize.y - (height / 2f) - boundaryPadding;

            // Handle small maps (smaller than camera view)
            if (maxBounds.x < minBounds.x)
            {
                float center = mapConfig.mapSize.x / 2f;
                minBounds.x = maxBounds.x = center;
            }
            if (maxBounds.y < minBounds.y)
            {
                float center = mapConfig.mapSize.y / 2f;
                minBounds.y = maxBounds.y = center;
            }
        }

        /// <summary>
        /// Instantly moves the camera to the follow target's position.
        /// </summary>
        public void SnapToTarget()
        {
            if (followTarget == null) return;
            Vector3 targetPos = followTarget.position + (Vector3)offset;
            targetPos.z = transform.position.z;
            transform.position = targetPos;
        }

        /// <summary>
        /// Public method to be called when screen resolution or camera settings change.
        /// </summary>
        public void OnResolutionChanged()
        {
            CalculateCameraBounds();
        }
    }
}