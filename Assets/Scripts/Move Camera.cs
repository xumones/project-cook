using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    [SerializeField] private Transform headPos;
    // Update is called once per frame
    void Update()
    {
        transform.position = headPos.position;
    }
}
