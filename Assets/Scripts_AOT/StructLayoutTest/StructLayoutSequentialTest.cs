using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 测试 StructLayout(LayoutKind.Sequential) 的顺序布局功能（用于对比）
/// 这个结构体使用顺序布局，字段按照声明的顺序排列，不做自动优化
/// 
/// 注意：与 Auto 布局使用完全相同的字段声明顺序，用于对比
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct StructLayoutSequential
{
    public byte byte1;      // 1 byte
    public long long1;      // 8 bytes - 需要8字节对齐，前面会有7字节padding
    public byte byte2;      // 1 byte - 在long后面会浪费7字节padding！
    public long long2;      // 8 bytes
    public byte byte3;      // 1 byte - 又会浪费7字节padding！
    public long long3;      // 8 bytes
    public byte byte4;      // 1 byte - 再次浪费7字节padding！

    // 使用 Sequential 布局时，字段严格按照声明顺序排列，不做任何优化
    // 每个byte在long后面都会产生7字节的padding浪费！

    public void LogSize()
    {
        int size = Marshal.SizeOf(typeof(StructLayoutSequential));
        UnityEngine.Debug.Log($"[Sequential布局] StructLayoutSequential 大小: {size} 字节");
        UnityEngine.Debug.Log("\n字段声明顺序和内存偏移（Sequential布局严格按声明顺序，不做优化）:");

        long byte1Offset = Marshal.OffsetOf(typeof(StructLayoutSequential), nameof(byte1)).ToInt64();
        long long1Offset = Marshal.OffsetOf(typeof(StructLayoutSequential), nameof(long1)).ToInt64();
        long byte2Offset = Marshal.OffsetOf(typeof(StructLayoutSequential), nameof(byte2)).ToInt64();
        long long2Offset = Marshal.OffsetOf(typeof(StructLayoutSequential), nameof(long2)).ToInt64();
        long byte3Offset = Marshal.OffsetOf(typeof(StructLayoutSequential), nameof(byte3)).ToInt64();
        long long3Offset = Marshal.OffsetOf(typeof(StructLayoutSequential), nameof(long3)).ToInt64();
        long byte4Offset = Marshal.OffsetOf(typeof(StructLayoutSequential), nameof(byte4)).ToInt64();

        UnityEngine.Debug.Log($"  1. byte1 -> 偏移: {byte1Offset} (占用1字节)");
        UnityEngine.Debug.Log($"  2. long1 -> 偏移: {long1Offset} (占用8字节, 前面填充{long1Offset - byte1Offset - 1}字节)");
        UnityEngine.Debug.Log($"  3. byte2 -> 偏移: {byte2Offset} (占用1字节, 前面填充{byte2Offset - long1Offset - 8}字节)");
        UnityEngine.Debug.Log($"  4. long2 -> 偏移: {long2Offset} (占用8字节, 前面填充{long2Offset - byte2Offset - 1}字节)");
        UnityEngine.Debug.Log($"  5. byte3 -> 偏移: {byte3Offset} (占用1字节, 前面填充{byte3Offset - long2Offset - 8}字节)");
        UnityEngine.Debug.Log($"  6. long3 -> 偏移: {long3Offset} (占用8字节, 前面填充{long3Offset - byte3Offset - 1}字节)");
        UnityEngine.Debug.Log($"  7. byte4 -> 偏移: {byte4Offset} (占用1字节, 前面填充{byte4Offset - long3Offset - 8}字节)");

        long totalPadding = (long1Offset - byte1Offset - 1) +
                           (byte2Offset - long1Offset - 8) +
                           (long2Offset - byte2Offset - 1) +
                           (byte3Offset - long2Offset - 8) +
                           (long3Offset - byte3Offset - 1) +
                           (byte4Offset - long3Offset - 8);

        UnityEngine.Debug.Log($"\n字段是否被重排: 否（Sequential布局不重排字段，保持声明顺序）");
        UnityEngine.Debug.Log($"总填充字节数: {totalPadding} 字节（内存浪费！）");
    }
}

/// <summary>
/// 对比测试结构体布局的 MonoBehaviour
/// </summary>
public class StructLayoutSequentialTest : MonoBehaviour
{
    void Start()
    {
        UnityEngine.Debug.Log("========== 测试 StructLayout(LayoutKind.Sequential) ==========");

        StructLayoutSequential sequentialStruct = new StructLayoutSequential
        {
            byte1 = 1,
            long1 = 123456789012345,
            byte2 = 2,
            long2 = 987654321098765,
            byte3 = 3,
            long3 = 555555555555555,
            byte4 = 4
        };

        sequentialStruct.LogSize();

        UnityEngine.Debug.Log("===========================================================");

        // 对比两种布局的内存大小
        // Auto 布局必须使用 unsafe sizeof，Sequential 可以使用 Marshal.SizeOf
        // 这是因为 Auto 布局是托管布局，不能进行非托管编组
        unsafe
        {
            int autoSize = sizeof(StructLayoutAuto);
            int sequentialSize = Marshal.SizeOf(typeof(StructLayoutSequential));

            UnityEngine.Debug.Log($"\n========== 对比结果 ==========");
            UnityEngine.Debug.Log($"  Auto布局大小: {autoSize} 字节");
            UnityEngine.Debug.Log($"  Sequential布局大小: {sequentialSize} 字节");

            if (sequentialSize >= autoSize)
            {
                UnityEngine.Debug.Log($"  内存节省: {sequentialSize - autoSize} 字节");
            }
            else
            {
                UnityEngine.Debug.Log($"  Auto布局比Sequential多: {autoSize - sequentialSize} 字节");
            }

            // 计算理论最小大小（所有字段的实际大小）
            int theoreticalMinSize = 1 + 8 + 1 + 8 + 1 + 8 + 1; // 4个byte + 3个long = 4 + 24 = 28字节

            UnityEngine.Debug.Log($"\n========== 详细分析 ==========");
            UnityEngine.Debug.Log($"理论最小大小（无填充）: {theoreticalMinSize} 字节");
            UnityEngine.Debug.Log($"Auto布局实际大小: {autoSize} 字节 (填充: {autoSize - theoreticalMinSize} 字节)");
            UnityEngine.Debug.Log($"Sequential布局实际大小: {sequentialSize} 字节 (填充: {sequentialSize - theoreticalMinSize} 字节)");
            UnityEngine.Debug.Log($"\nAuto布局节省内存: {sequentialSize - autoSize} 字节");
            UnityEngine.Debug.Log($"\n结论:");
            UnityEngine.Debug.Log($"  ✓ Auto布局：CLR自动优化字段排列，将相同大小的字段放在一起");
            UnityEngine.Debug.Log($"    例如：所有long字段连续排列，所有byte字段填充空隙");
            UnityEngine.Debug.Log($"  ✗ Sequential布局：严格按声明顺序排列，每个byte在long后都产生7字节填充");
            UnityEngine.Debug.Log($"  ⚠ Auto布局不能使用Marshal.SizeOf，必须使用unsafe sizeof");
            UnityEngine.Debug.Log($"=====================================");
        }
    }
}