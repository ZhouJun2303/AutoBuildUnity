using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 极端测试：确保能触发 Auto 布局的字段重排
/// 使用更复杂的字段组合和反射来验证
/// </summary>
public class StructLayoutAutoExtremeTest : MonoBehaviour
{
    unsafe void Start()
    {
        UnityEngine.Debug.Log("\n╔════════════════════════════════════════════════════════════════╗");
        UnityEngine.Debug.Log("║   极端测试：为什么 Auto 布局没有重排字段？                   ║");
        UnityEngine.Debug.Log("╚════════════════════════════════════════════════════════════════╝\n");

        // 测试1：使用反射查看字段定义顺序
        UnityEngine.Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        UnityEngine.Debug.Log("📋 测试1: 使用反射查看字段定义顺序");
        var autoFields = typeof(StructLayoutAuto).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        UnityEngine.Debug.Log("StructLayoutAuto 字段定义顺序:");
        for (int i = 0; i < autoFields.Length; i++)
        {
            UnityEngine.Debug.Log($"  {i + 1}. {autoFields[i].Name} ({autoFields[i].FieldType.Name})");
        }

        // 测试2：创建极端例子 - 使用很多小的byte字段穿插在大的字段之间
        UnityEngine.Debug.Log("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        UnityEngine.Debug.Log("📋 测试2: 极端例子 - 更多小字段穿插");
        TestExtremeLayout();

        // 测试3：解释为什么可能没有重排
        UnityEngine.Debug.Log("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        UnityEngine.Debug.Log("📋 测试3: 为什么 Auto 布局可能没有重排？");
        UnityEngine.Debug.Log("\n可能的原因：");
        UnityEngine.Debug.Log("  1. CLR 的 Auto 布局策略可能比较保守");
        UnityEngine.Debug.Log("     - 在某些情况下，CLR 可能认为当前顺序已经足够好");
        UnityEngine.Debug.Log("     - 重排可能带来的收益不足以触发优化");
        UnityEngine.Debug.Log("\n  2. Unity/IL2CPP 的特殊行为");
        UnityEngine.Debug.Log("     - Unity 使用 IL2CPP 时，布局行为可能与标准 .NET 不同");
        UnityEngine.Debug.Log("     - Mono 和 IL2CPP 的实现可能不同");
        UnityEngine.Debug.Log("\n  3. 平台特定的对齐要求");
        UnityEngine.Debug.Log("     - 不同平台的内存对齐规则可能影响布局决策");
        UnityEngine.Debug.Log("\n  4. Auto 布局的实际行为");
        UnityEngine.Debug.Log("     - Auto 布局不一定总是重排字段");
        UnityEngine.Debug.Log("     - 它主要用于优化内存对齐，而不是强制重排");
        UnityEngine.Debug.Log("     - 在某些情况下，即使声明顺序不是最优，也可能不重排");

        UnityEngine.Debug.Log("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        UnityEngine.Debug.Log("📌 关键结论：");
        UnityEngine.Debug.Log("  • Auto 布局的目的是优化内存对齐，而不是保证字段重排");
        UnityEngine.Debug.Log("  • 如果你的代码需要特定的字段顺序，应该使用 Sequential 或 Explicit");
        UnityEngine.Debug.Log("  • Auto 布局的优势在于 CLR 可以自由优化，但不保证会重排");
        UnityEngine.Debug.Log("  • 对于与非托管代码互操作，必须使用 Sequential 或 Explicit");
        UnityEngine.Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
    }

    unsafe void TestExtremeLayout()
    {
        // 创建一个更极端的结构体
        ExtremeAutoStruct extremeAuto = new ExtremeAutoStruct
        {
            b1 = 1,
            l1 = 100,
            b2 = 2,
            b3 = 3,
            b4 = 4,
            l2 = 200,
            b5 = 5,
            b6 = 6,
            l3 = 300,
            b7 = 7
        };

        int autoSize = sizeof(ExtremeAutoStruct);
        UnityEngine.Debug.Log($"ExtremeAutoStruct 大小: {autoSize} 字节");

        ExtremeAutoStruct* pAuto = stackalloc ExtremeAutoStruct[1];
        *pAuto = extremeAuto;
        byte* basePtr = (byte*)pAuto;

        var fields = new[]
        {
            (name: "b1", offset: (long)&pAuto->b1 - (long)basePtr),
            (name: "l1", offset: (long)&pAuto->l1 - (long)basePtr),
            (name: "b2", offset: (long)&pAuto->b2 - (long)basePtr),
            (name: "b3", offset: (long)&pAuto->b3 - (long)basePtr),
            (name: "b4", offset: (long)&pAuto->b4 - (long)basePtr),
            (name: "l2", offset: (long)&pAuto->l2 - (long)basePtr),
            (name: "b5", offset: (long)&pAuto->b5 - (long)basePtr),
            (name: "b6", offset: (long)&pAuto->b6 - (long)basePtr),
            (name: "l3", offset: (long)&pAuto->l3 - (long)basePtr),
            (name: "b7", offset: (long)&pAuto->b7 - (long)basePtr)
        };

        Array.Sort(fields, (a, b) => a.offset.CompareTo(b.offset));

        UnityEngine.Debug.Log("\n声明顺序: b1, l1, b2, b3, b4, l2, b5, b6, l3, b7");
        UnityEngine.Debug.Log("实际内存顺序:");
        for (int i = 0; i < fields.Length; i++)
        {
            UnityEngine.Debug.Log($"  {i + 1}. {fields[i].name} @ 偏移 {fields[i].offset}");
        }

        // 检查是否重排
        string[] declared = { "b1", "l1", "b2", "b3", "b4", "l2", "b5", "b6", "l3", "b7" };
        bool reordered = false;
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i].name != declared[i])
            {
                reordered = true;
                break;
            }
        }

        if (reordered)
        {
            UnityEngine.Debug.Log("\n✅ 字段已被重排！");
        }
        else
        {
            UnityEngine.Debug.Log("\n⚠️  字段未被重排（即使在极端例子中）");
            UnityEngine.Debug.Log("   这说明在当前环境下，CLR 选择保持字段顺序");
        }
    }
}

/// <summary>
/// 极端测试结构体：使用很多小的byte字段穿插在long字段之间
/// </summary>
[StructLayout(LayoutKind.Auto)]
public struct ExtremeAutoStruct
{
    public byte b1;   // 1 byte
    public long l1;   // 8 bytes
    public byte b2;   // 1 byte
    public byte b3;   // 1 byte
    public byte b4;   // 1 byte
    public long l2;   // 8 bytes
    public byte b5;   // 1 byte
    public byte b6;   // 1 byte
    public long l3;   // 8 bytes
    public byte b7;   // 1 byte
}
