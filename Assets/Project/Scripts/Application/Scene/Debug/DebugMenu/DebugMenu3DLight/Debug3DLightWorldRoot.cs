#if _DEBUG
using UnityEngine;
using UnityEngine.Rendering;

public sealed class Debug3DLightWorldRoot : MonoBehaviour
{
    [Header("Lights")]
    [SerializeField] private Light m_directionalLight;
    [SerializeField] private Light m_lampLight;

    [Header("Post Process")]
    [SerializeField] private Volume m_globalVolume;

    [Header("Preview Camera")]
    [SerializeField] private Camera m_previewCamera;

    public Light DirectionalLight => m_directionalLight;
    public Light LampLight => m_lampLight;
    public Volume GlobalVolume => m_globalVolume;

    public Camera PreviewCamera => m_previewCamera;


    private void Reset()
    {
        // とりあえずの補助。基本はインスペクタで刺す想定。
        if (!m_directionalLight)
        {
            var go = GameObject.Find("Directional Light");
            if (go) m_directionalLight = go.GetComponent<Light>();
        }

        if (!m_lampLight)
        {
            var go = GameObject.Find("LampLight");
            if (go) m_lampLight = go.GetComponent<Light>();
        }

        if (!m_globalVolume)
        {
            var go = GameObject.Find("GlobalVolume");
            if (go) m_globalVolume = go.GetComponent<Volume>();
        }
    }
}
#endif
