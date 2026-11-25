using UnityEngine;

namespace WeirdWar.Spike1.Presentation
{
    /// <summary>
    /// Pan (WASD or arrows, or drag with the right or middle mouse button), zoom (scroll wheel) and clamping
    /// for an orthographic camera looking at the map. Presentation only.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class MapCameraController : MonoBehaviour
    {
        public float KeyPanSpeed = 1.2f;       // screen heights per second at any zoom
        public float ZoomStep = 0.15f;         // fraction of the size per scroll notch
        public float MinSize = 3f;
        public float FitMargin = 1f;          // spare world units around the map when fully zoomed out
        public float Margin = 3f;              // how far past the map edge the view centre may go

        /// <summary>
        /// Largest orthographic size: the smallest that shows the whole map with FitMargin to spare, for the
        /// camera's current aspect ratio. Derived from the map bounds, so it follows the map and the window shape.
        /// </summary>
        public float MaxSize
        {
            get
            {
                var forHeight = _map.extents.y + FitMargin;
                var forWidth = (_map.extents.x + FitMargin) / Mathf.Max(Cam.aspect, 0.01f);
                return Mathf.Max(MinSize, forHeight, forWidth);
            }
        }

        private Camera _camera;
        private Bounds _map;
        private Vector3 _dragOriginWorld;

        public void SetMapBounds(Bounds map)
        {
            _map = map;
            Clamp();
        }

        /// <summary>Zooms out as far as allowed and centres on the map.</summary>
        public void ZoomOutFully()
        {
            FitToMap();
        }

        public void FitToMap()
        {
            Cam.orthographicSize = MaxSize;
            var p = transform.position;
            transform.position = new Vector3(_map.center.x, _map.center.y, p.z);
            Clamp();
        }

        private Camera Cam => _camera != null ? _camera : (_camera = GetComponent<Camera>());

        private void Update()
        {
            var move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (move != Vector2.zero)
            {
                var step = move.normalized * (KeyPanSpeed * 2f * Cam.orthographicSize * Time.unscaledDeltaTime);
                transform.position += new Vector3(step.x, step.y, 0f);
            }

            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                _dragOriginWorld = Cam.ScreenToWorldPoint(Input.mousePosition);
            }
            else if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                var now = Cam.ScreenToWorldPoint(Input.mousePosition);
                var delta = _dragOriginWorld - now;
                transform.position += new Vector3(delta.x, delta.y, 0f);
            }

            var scroll = Input.mouseScrollDelta.y;
            if (scroll != 0f)
            {
                // Zoom towards the point under the cursor.
                var before = Cam.ScreenToWorldPoint(Input.mousePosition);
                Cam.orthographicSize = Mathf.Clamp(Cam.orthographicSize * (1f - scroll * ZoomStep), MinSize, MaxSize);
                var after = Cam.ScreenToWorldPoint(Input.mousePosition);
                var shift = before - after;
                transform.position += new Vector3(shift.x, shift.y, 0f);
            }

            Clamp();
        }

        /// <summary>Keeps the view centre within the map plus a margin.</summary>
        public void Clamp()
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, _map.min.x - Margin, _map.max.x + Margin);
            p.y = Mathf.Clamp(p.y, _map.min.y - Margin, _map.max.y + Margin);
            transform.position = p;
        }
    }
}
