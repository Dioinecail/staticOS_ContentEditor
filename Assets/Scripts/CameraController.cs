using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float m_CameraSpeed = 5f;
    [SerializeField] private float m_CameraScrollIncrement = 2f;

    private Transform m_Transform;
    private Vector3 m_TargetPosition;



    private void OnEnable()
    {
        m_Transform = transform;
        m_TargetPosition = m_Transform.position;
    }

    private void Update()
    {
        var scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) < 0.01f)
            return;

        m_TargetPosition += Vector3.up * scroll * m_CameraScrollIncrement;
    }

    private void FixedUpdate()
    {
        m_Transform.position = Vector3.Lerp(m_Transform.position, m_TargetPosition, m_CameraSpeed * Time.deltaTime);
    }
}
