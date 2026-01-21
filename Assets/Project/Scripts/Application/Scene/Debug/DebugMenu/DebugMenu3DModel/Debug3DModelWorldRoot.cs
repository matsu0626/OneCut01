using UnityEngine;

public class Debug3DModelWorldRoot : MonoBehaviour
{
    [SerializeField] private Transform m_spawnRoot;
    [SerializeField] private Camera m_previewCamera;
    [SerializeField] private Light m_keyLight;

    public Transform SpawnRoot => m_spawnRoot;
    public Camera PreviewCamera => m_previewCamera;
    public Light KeyLight => m_keyLight;
}
