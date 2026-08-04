using UnityEngine;
using System.Collections.Generic;

public class TentacleFABRIK : MonoBehaviour
{
    [Header("Bones")]
    public Transform root;          // 触手根部
    public Transform target;        // 末端目标
    public int iterations = 3;      // FABRIK 迭代次数
    public float tolerance = 0.001f;

    public Transform[] bones;
    private Vector3[] positions;
    private float[] lengths;
    private float totalLength;

    void Start()
    {
        Init();
    }

    void Init()
    {
        // 收集骨骼链
        List<Transform> chain = new List<Transform>();
        Transform current = root;
        chain.Add(current);

        while (current.childCount > 0)
        {
            current = current.GetChild(0);
            chain.Add(current);
        }

        bones = chain.ToArray();
        int count = bones.Length;

        positions = new Vector3[count];
        lengths = new float[count - 1];
        totalLength = 0f;

        for (int i = 0; i < count - 1; i++)
        {
            lengths[i] = Vector3.Distance(
                bones[i].position,
                bones[i + 1].position
            );
            totalLength += lengths[i];
        }
    }

    void LateUpdate()
    {
        SolveIK();
        ApplyRotation();
    }

    void SolveIK()
    {
        int count = bones.Length;

        // 缓存当前位置
        for (int i = 0; i < count; i++)
            positions[i] = bones[i].position;

        // 目标超出最大长度 → 直接拉直
        if ((target.position - positions[0]).sqrMagnitude > totalLength * totalLength)
        {
            Vector3 dir = (target.position - positions[0]).normalized;
            for (int i = 1; i < count; i++)
                positions[i] = positions[i - 1] + dir * lengths[i - 1];
        }
        else
        {
            for (int iter = 0; iter < iterations; iter++)
            {
                // Backward
                positions[count - 1] = target.position;
                for (int i = count - 2; i >= 0; i--)
                {
                    Vector3 dir = (positions[i] - positions[i + 1]).normalized;
                    positions[i] = positions[i + 1] + dir * lengths[i];
                }

                // Forward
                positions[0] = bones[0].position;
                for (int i = 1; i < count; i++)
                {
                    Vector3 dir = (positions[i] - positions[i - 1]).normalized;
                    positions[i] = positions[i - 1] + dir * lengths[i - 1];
                }

                if ((positions[count - 1] - target.position).sqrMagnitude < tolerance)
                    break;
            }
        }

        // 写回位置
        for (int i = 0; i < count; i++)
            bones[i].position = positions[i];
    }

    void ApplyRotation()
    {
        // 根据子节点方向设置旋转（非常重要）
        for (int i = 0; i < bones.Length - 1; i++)
        {
            Vector3 dir = bones[i + 1].position - bones[i].position;
            if (dir != Vector3.zero)
                bones[i].rotation = Quaternion.LookRotation(dir);
        }
    }
}
