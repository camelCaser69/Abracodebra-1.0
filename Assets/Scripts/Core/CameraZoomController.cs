using UnityEngine;
using WegoSystem;

namespace WegoSystem {
    public class CameraZoomController : MonoBehaviour {
        [SerializeField] Camera targetCamera;
        [SerializeField] float zoomSpeed = 0.5f;
        [SerializeField] float minZoom = 0.5f;  // 2x closer
        [SerializeField] float maxZoom = 3.0f;  // 3x further
        [SerializeField] bool enableZoom = true;
        
        private float baseOrthographicSize;
        private float currentZoomLevel = 1f;
        
        void Start() {
            if (targetCamera == null) targetCamera = Camera.main;
            StoreBaseSize();
        }
        
        void Update() {
            if (!enableZoom) return;
            
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f) {
                ZoomCamera(-scroll * zoomSpeed);
            }
        }
        
        void ZoomCamera(float zoomDelta) {
            currentZoomLevel = Mathf.Clamp(currentZoomLevel + zoomDelta, minZoom, maxZoom);
            targetCamera.orthographicSize = baseOrthographicSize * currentZoomLevel;
        }
        
        public void StoreBaseSize() {
            if (targetCamera != null) {
                baseOrthographicSize = targetCamera.orthographicSize;
            }
        }
        
        public void ResetZoom() {
            currentZoomLevel = 1f;
            targetCamera.orthographicSize = baseOrthographicSize;
        }
    }
}