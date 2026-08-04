using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 测试 StructLayout(LayoutKind.Auto) 的自动对齐功能
/// 这个结构体使用自动布局，CLR 会自动优化字段排列以提高内存效率
/// 
/// 重要说明：
/// 1. Auto 布局是托管布局，不能使用 Marshal.SizeOf（只能用于非托管布局）
/// 2. 必须使用 unsafe sizeof 来获取结构体大小
/// 3. 需要在 GameAOT.asmdef 中设置 "allowUnsafeCode": true
/// 4. 如果编译报错，请确保 Unity 已重新编译程序集定义文件
/// 
/// 注意：这个结构体的字段声明顺序故意设计得非常糟糕！
///       - byte字段分散在long字段之间，会产生大量内存填充
///       - Auto布局会将long字段放在一起，byte字段填充空隙，节省内存
/// </summary>
[StructLayout(LayoutKind.Auto)]
public struct StructLayoutAuto
{
    public byte byte1;      // 1 byte
    public long long1;      // 8 bytes - 需要8字节对齐
    public byte byte2;      // 1 byte - 在long后面会浪费7字节padding！
    public long long2;      // 8 bytes
    public byte byte3;      // 1 byte - 又会浪费7字节padding！
    public long long3;      // 8 bytes
    public byte byte4;      // 1 byte - 再次浪费7字节padding！

    // 使用 Auto 布局时，CLR 会自动重新排列字段顺序：
    // 会将所有long字段放在一起，所有byte字段放在一起填充空隙
    // 这样可以大幅减少内存浪费

    public unsafe void LogSize()
    {
        // Auto 布局必须使用 unsafe sizeof，不能使用 Marshal.SizeOf
        int size = sizeof(StructLayoutAuto);
        UnityEngine.Debug.Log($"[Auto布局] StructLayoutAuto 大小: {size} 字节");
        UnityEngine.Debug.Log("\n字段声明顺序和内存偏移:");

        // 使用 unsafe 指针获取字段偏移
        fixed (StructLayoutAuto* p = &this)
        {
            byte* basePtr = (byte*)p;

            // 按声明顺序显示
            UnityEngine.Debug.Log($"  1. byte1 (声明顺序)  -> 偏移: {(long)&p->byte1 - (long)basePtr}");
            UnityEngine.Debug.Log($"  2. long1 (声明顺序)  -> 偏移: {(long)&p->long1 - (long)basePtr}");
            UnityEngine.Debug.Log($"  3. byte2 (声明顺序)  -> 偏移: {(long)&p->byte2 - (long)basePtr}");
            UnityEngine.Debug.Log($"  4. long2 (声明顺序)  -> 偏移: {(long)&p->long2 - (long)basePtr}");
            UnityEngine.Debug.Log($"  5. byte3 (声明顺序)  -> 偏移: {(long)&p->byte3 - (long)basePtr}");
            UnityEngine.Debug.Log($"  6. long3 (声明顺序)  -> 偏移: {(long)&p->long3 - (long)basePtr}");
            UnityEngine.Debug.Log($"  7. byte4 (声明顺序)  -> 偏移: {(long)&p->byte4 - (long)basePtr}");

            // 按内存偏移排序显示（实际内存排列顺序）
            UnityEngine.Debug.Log("\n实际内存排列顺序（按偏移从小到大）:");
            var fields = new[]
            {
                (name: "byte1", offset: (long)&p->byte1 - (long)basePtr),
                (name: "long1", offset: (long)&p->long1 - (long)basePtr),
                (name: "byte2", offset: (long)&p->byte2 - (long)basePtr),
                (name: "long2", offset: (long)&p->long2 - (long)basePtr),
                (name: "byte3", offset: (long)&p->byte3 - (long)basePtr),
                (name: "long3", offset: (long)&p->long3 - (long)basePtr),
                (name: "byte4", offset: (long)&p->byte4 - (long)basePtr)
            };

            Array.Sort(fields, (a, b) => a.offset.CompareTo(b.offset));
            for (int i = 0; i < fields.Length; i++)
            {
                UnityEngine.Debug.Log($"  {i + 1}. {fields[i].name} -> 偏移: {fields[i].offset}");
            }

            // 检查是否重排了
            bool isReordered = false;
            for (int i = 0; i < fields.Length; i++)
            {
                string[] declaredOrder = { "byte1", "long1", "byte2", "long2", "byte3", "long3", "byte4" };
                if (fields[i].name != declaredOrder[i])
                {
                    isReordered = true;
                    break;
                }
            }

            // 显示字段排列的详细信息
            UnityEngine.Debug.Log("\n详细分析:");
            string declaredStr = "声明顺序: byte1, long1, byte2, long2, byte3, long3, byte4";
            string memoryStr = "内存顺序: " + string.Join(", ", System.Array.ConvertAll(fields, f => f.name));
            UnityEngine.Debug.Log($"  {declaredStr}");
            UnityEngine.Debug.Log($"  {memoryStr}");

            UnityEngine.Debug.Log($"\n字段是否被重排: {(isReordered ? "是 ✓" : "否（已是最优顺序）")}");
        }
    }
}

/// <summary>
/// 测试结构体布局的 MonoBehaviour
/// </summary>
public class StructLayoutAutoTest : MonoBehaviour
{
    void Start()
    {
        UnityEngine.Debug.Log("========== 测试 StructLayout(LayoutKind.Auto) ==========");

        StructLayoutAuto autoStruct = new StructLayoutAuto
        {
            byte1 = 1,
            long1 = 123456789012345,
            byte2 = 2,
            long2 = 987654321098765,
            byte3 = 3,
            long3 = 555555555555555,
            byte4 = 4
        };

        autoStruct.LogSize();

        UnityEngine.Debug.Log("\n说明: Auto 布局由 CLR 自动优化字段排列顺序，");
        UnityEngine.Debug.Log("      可以看到字段偏移可能与声明顺序不同！");
        UnityEngine.Debug.Log("======================================================");
    }
}