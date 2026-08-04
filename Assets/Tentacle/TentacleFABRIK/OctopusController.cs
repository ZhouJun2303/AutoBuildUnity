using UnityEngine;
using System.Collections.Generic;

public class OctopusController : MonoBehaviour
{
    public Rigidbody rb;
    public List<TentacleController> tentacles;
    public int minAnchored = 3;
    public float moveStrength = 10f;

    void FixedUpdate()
    {
        ApplyLocomotion();
    }

    void ApplyLocomotion()
    {
        Vector3 force = Vector3.zero;
        int count = 0;

        foreach (var t in tentacles)
        {
            if (t.IsAnchored)
            {
                Vector3 dir = (transform.position - t.AnchorPoint).normalized;
                force += dir;
                count++;
            }
        }

        if (count >= minAnchored)
        {
            rb.AddForce(force.normalized * moveStrength, ForceMode.Force);
        }
    }
}
