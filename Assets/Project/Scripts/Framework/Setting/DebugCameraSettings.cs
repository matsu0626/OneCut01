#if _DEBUG
using UnityEngine;

[CreateAssetMenu(
    fileName = "DebugCameraSettings",
    menuName = "Debug/Debug Camera Settings")]
public sealed class DebugCameraSettings : ScriptableObject
{
    [Header("Orbit (degrees per pixel)")]
    [Tooltip("マウス1px移動あたりの回転量（度）")]
    public float orbitRotateSpeed = 0.25f;

    [Header("Zoom (world units per scroll unit)")]
    [Tooltip("スクロール1単位あたりの距離変化量（ワールド単位）")]
    public float orbitZoomSpeed = 0.5f;

    [Header("Pan (world units per pixel * distance)")]
    [Tooltip("マウス1px移動あたりのパン量（距離に比例）")]
    public float panSpeed = 0.003f;

    [Header("Distance Limits")]
    public float minDistance = 0.5f;
    public float maxDistance = 30f;
}
#endif
