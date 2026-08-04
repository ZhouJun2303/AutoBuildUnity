using UnityEngine;
public enum TentacleState
{
    Searching,   // 摆动 / 找落点
    Anchoring,   // 吸盘贴地
    Pushing,     // 推身体
    Releasing    // 松开
}


public class TentacleController : MonoBehaviour
{
    public TentacleState state;

    public Transform target;
    public Transform body;
    public float searchRadius = 1.5f;
    public float anchorOffset = 0.05f;
    public float pushForce = 5f;

    public bool IsAnchored => state == TentacleState.Anchoring || state == TentacleState.Pushing;
    public Vector3 AnchorPoint { get; private set; }

    float timer;

    void Update()
    {
        timer += Time.deltaTime;

        switch (state)
        {
            case TentacleState.Searching:
                Searching();
                break;
            case TentacleState.Anchoring:
                Anchoring();
                break;
            case TentacleState.Pushing:
                Pushing();
                break;
            case TentacleState.Releasing:
                Releasing();
                break;
        }
    }

    void Searching()
    {
        Vector3 origin = body.position + Random.insideUnitSphere * searchRadius;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 3f))
        {
            target.position = Vector3.Lerp(target.position, hit.point, Time.deltaTime * 5f);

            if (Vector3.Distance(target.position, hit.point) < 0.05f)
            {
                AnchorPoint = hit.point + Vector3.up * anchorOffset;
                state = TentacleState.Anchoring;
                timer = 0;
            }
        }
    }

    void Anchoring()
    {
        target.position = AnchorPoint;

        if (timer > 0.2f)
        {
            state = TentacleState.Pushing;
            timer = 0;
        }
    }

    void Pushing()
    {
        target.position = AnchorPoint;

        if (timer > 0.3f)
        {
            state = TentacleState.Releasing;
            timer = 0;
        }
    }

    void Releasing()
    {
        if (timer > 0.15f)
        {
            state = TentacleState.Searching;
            timer = 0;
        }
    }
}
