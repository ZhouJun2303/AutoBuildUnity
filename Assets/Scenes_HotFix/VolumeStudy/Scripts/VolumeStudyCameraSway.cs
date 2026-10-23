using UnityEngine;

/// <summary>
/// Slides the camera sideways so Motion Blur has camera velocity to sample.
/// URP 14 only enables this effect in Camera Only mode.
/// </summary>
public class VolumeStudyCameraSway : MonoBehaviour
{
    public float amplitude = 1.8f;
    public float cyclesPerSecond = 0.28f;

    Vector3 origin;

    void OnEnable()
    {
        origin = transform.position;
    }

    void Update()
    {
        float angle = Time.time * cyclesPerSecond * Mathf.PI * 2f;
        Vector3 position = origin;
        position.x += Mathf.Sin(angle) * amplitude;
        transform.position = position;
    }
}
