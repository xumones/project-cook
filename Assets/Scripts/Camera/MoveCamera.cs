using UnityEngine;

namespace ProjectCook.CameraControl
{
    public class MoveCamera : MonoBehaviour
    {
        [SerializeField] private Transform headPos;

        private void Update()
        {
            if (headPos != null)
            {
                transform.position = headPos.position;
            }
        }
    }
}
