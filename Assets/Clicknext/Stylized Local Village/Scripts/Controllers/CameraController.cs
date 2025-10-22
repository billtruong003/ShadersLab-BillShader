using UnityEngine;

namespace Clicknext.StylizedLocalVillage.Controllers
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] float minX;
        [SerializeField] float maxX;
        [SerializeField] float minZ;
        [SerializeField] float maxZ;
        [SerializeField] Transform followTarget;

        void Update()
        {
            var position = Vector3.Lerp(transform.position, followTarget.position, Time.deltaTime * 1.5f);
            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.z = Mathf.Clamp(position.z, minZ, maxZ);
            transform.position = position;
        }
    }
}
