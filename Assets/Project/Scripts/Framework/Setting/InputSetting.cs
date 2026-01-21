using UnityEngine;

[CreateAssetMenu(fileName = "InputSetting", menuName = "Scriptable Objects/InputSetting")]
public class InputSetting : ScriptableObject
{
    [Header("キーリピート設定")]
    public float firstDelay = 0.3f;         // 押してから最初にリピート開始するまでの時間
    public float repeatInterval = 0.05f;    // リピート間隔
}
