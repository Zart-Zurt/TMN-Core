using UnityEngine;

namespace Core.Utils
{
    [AddComponentMenu("Core/Utils/Ray Test")]
    public class RayTest : MonoBehaviour
    {
        [SerializeField] private Camera cam;

        private void Start()
        {
            if (cam == null)
            {
                cam = Camera.main;
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var worldPoint = cam.ScreenToWorldPoint(Input.mousePosition);
                var rayOrigin = new Vector2(worldPoint.x, worldPoint.y);
                var hits = Physics2D.RaycastAll(rayOrigin, Vector2.zero);
                foreach (var hit in hits)
                {
                    Debug.Log("Hit object: " + hit.collider.name);
                }
            }
        }
    }
}
