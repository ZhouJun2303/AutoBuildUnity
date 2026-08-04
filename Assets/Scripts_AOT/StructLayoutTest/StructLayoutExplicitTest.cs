using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 测试 StructLayout(LayoutKind.Explicit) 的显式布局功能
/// Explicit 布局允许手动指定每个字段的精确内存偏移量
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct StructLayoutExplicit
{
    [FieldOffset(0)]
    public byte byte1;      // 偏移 0
    
    [FieldOffset(8)]        // 跳过前8字节，直接对齐到8字节边界
    public long long1;      // 偏移 8
    
    [FieldOffset(1)]        // 放在 byte1 后面（填充空隙）
    public byte byte2;      // 偏移 1
    
    [FieldOffset(16)]       // 放在 long1 后面（8字节对齐）
    public long long2;      // 偏移 16
    
    [FieldOffset(2)]        // 放在 byte2 后面（继续填充空隙）
    public byte byte3;      // 偏移 2
    
    [FieldOffset(24)]       // 放在 long2 后面（8字节对齐）
    public long long3;      // 偏移 24
    
    [FieldOffset(3)]        // 放在 byte3 后面（继续填充空隙）
    public byte byte4;      // 偏移 3

    // Explicit 布局的特点：
    // 1. 必须手动为每个字段指定 FieldOffset
    // 2. 可以精确控制字段的内存位置
    // 3. 可以手动优化内存布局
    // 4. 可以与非托管代码互操作（Marshal）
    // 5. 字段可以重叠（但要注意数据安全）

    public void LogSize()
    {
        int size = Marshal.SizeOf(typeof(StructLayoutExplicit));
        UnityEngine.Debug.Log($"[Explicit布局] StructLayoutExplicit 大小: {size} 字节");
        UnityEngine.Debug.Log("\n字段偏移（手动指定）:");
        UnityEngine.Debug.Log($"  byte1  -> 偏移: {Marshal.OffsetOf(typeof(StructLayoutExplicit), nameof(byte1))}");
        UnityEngine.Debug.Log($"  long1  -> 偏移: {Marshal.OffsetOf(typeof(StructLayoutExplicit), nameof(long1))}");
        UnityEngine.Debug.Log($"  byte2  -> 偏移: {Marshal.OffsetOf(typeof(StructLayoutExplicit), nameof(byte2))}");
        UnityEngine.Debug.Log($"  long2  -> 偏移: {Marshal.OffsetOf(typeof(StructLayoutExplicit), nameof(long2))}");
        UnityEngine.Debug.Log($"  byte3  -> 偏移: {Marshal.OffsetOf(typeof(StructLayoutExplicit), nameof(byte3))}");
        UnityEngine.Debug.Log($"  long3  -> 偏移: {Marshal.OffsetOf(typeof(StructLayoutExplicit), nameof(long3))}");
        UnityEngine.Debug.Log($"  byte4  -> 偏移: {Marshal.OffsetOf(typeof(StructLayoutExplicit), nameof(byte4))}");
        
        UnityEngine.Debug.Log("\n布局说明:");
        UnityEngine.Debug.Log($"  • 所有 byte 字段（byte1, byte2, byte3, byte4）被放置在偏移 0-3");
        UnityEngine.Debug.Log($"  • 所有 long 字段（long1, long2, long3）按8字节对齐（偏移 8, 16, 24）");
        UnityEngine.Debug.Log($"  • 这样可以最小化内存浪费（总大小约 32 字节）");
    }
}

/// <summary>
/// 优化版本的 Explicit 布局：更紧凑的内存布局
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct StructLayoutExplicitOptimized
{
    // 将所有 long 字段放在前面，byte 字段填充空隙
    [FieldOffset(0)]
    public long long1;      // 偏移 0
    
    [FieldOffset(8)]
    public long long2;      // 偏移 8
    
    [FieldOffset(16)]
    public long long3;      // 偏移 16
    
    [FieldOffset(24)]
    public byte byte1;      // 偏移 24（放在 long 字段后面填充空隙）
    
    [FieldOffset(25)]
    public byte byte2;      // 偏移 25
    
    [FieldOffset(26)]
    public byte byte3;      // 偏移 26
    
    [FieldOffset(27)]
    public byte byte4;      // 偏移 27

    public void LogSize()
    {
        int size = Marshal.SizeOf(typeof(StructLayoutExplicitOptimized));
        UnityEngine.Debug.Log($"[Explicit布局-优化版] StructLayoutExplicitOptimized 大小: {size} 字节");
        UnityEngine.Debug.Log("\n优化后的字段偏移:");
        UnityEngine.Debug.Log($"  long1  -> 偏移: {Marshal.OffsetOf(typeof(StructLayoutExplicitOptimized), nameof(long1))}");
        UnityEngine.Debug.Log($"  long2  -> 偏移: {Marshal.OffsetOf(typeof(StructLayoutExplicitOptimized), nameof(long2))}");
        UnityEngine.Debug.Log($"  long3  -> 偏移: {Marshal.OffsetOf(typeof(StructLayoutExplicitOptimized), nameof(long3))}");
        UnityEngine.Debug.Log($"  byte1  -> 偏移: {Marshal.OffsetOf(typeof(StructLayoutExplicitOptimized), nameof(byte1))}");
        UnityEngine.Debug.Log($"  byte2  -> 偏移: {Marshal.OffsetOf(typeof(StructLayoutExplicitOptimized), nameof(byte2))}");
        UnityEngine.Debug.Log($"  byte3  -> 偏移: {Marshal.OffsetOf(typeof(StructLayoutExplicitOptimized), nameof(byte3))}");
        UnityEngine.Debug.Log($"  byte4  -> 偏移: {Marshal.OffsetOf(typeof(StructLayoutExplicitOptimized), nameof(byte4))}");
        
        UnityEngine.Debug.Log("\n优化说明:");
        UnityEngine.Debug.Log($"  • 先放置所有需要对齐的大字段（long）");
        UnityEngine.Debug.Log($"  • 然后用小字段（byte）填充空隙");
        UnityEngine.Debug.Log($"  • 这样可以得到最小的内存占用（28字节，无填充）");
    }
}

/// <summary>
/// 测试 Explicit 布局的 MonoBehaviour
/// </summary>
public class StructLayoutExplicitTest : MonoBehaviour
{
    void Start()
    {
        UnityEngine.Debug.Log("\n╔════════════════════════════════════════════════════════════════╗");
        UnityEngine.Debug.Log("║   测试 StructLayout(LayoutKind.Explicit) 显式布局            ║");
        UnityEngine.Debug.Log("╚════════════════════════════════════════════════════════════════╝\n");

        // 测试标准 Explicit 布局
        UnityEngine.Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        UnityEngine.Debug.Log("📋 测试1: 标准 Explicit 布局");
        
        StructLayoutExplicit explicitStruct = new StructLayoutExplicit
        {
            byte1 = 1,
            long1 = 123456789012345,
            byte2 = 2,
            long2 = 987654321098765,
            byte3 = 3,
            long3 = 555555555555555,
            byte4 = 4
        };
        
        explicitStruct.LogSize();

        // 测试优化版 Explicit 布局
        UnityEngine.Debug.Log("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        UnityEngine.Debug.Log("📋 测试2: 优化版 Explicit 布局");
        
        StructLayoutExplicitOptimized optimizedStruct = new StructLayoutExplicitOptimized
        {
            long1 = 123456789012345,
            long2 = 987654321098765,
            long3 = 555555555555555,
            byte1 = 1,
            byte2 = 2,
            byte3 = 3,
            byte4 = 4
        };
        
        optimizedStruct.LogSize();

        // 对比三种布局
        unsafe
        {
            int autoSize = sizeof(StructLayoutAuto);
            int sequentialSize = Marshal.SizeOf(typeof(StructLayoutSequential));
            int explicitSize = Marshal.SizeOf(typeof(StructLayoutExplicit));
            int explicitOptimizedSize = Marshal.SizeOf(typeof(StructLayoutExplicitOptimized));

            UnityEngine.Debug.Log("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            UnityEngine.Debug.Log("📊 三种布局方式对比:");
            UnityEngine.Debug.Log($"   Auto布局:              {autoSize} 字节");
            UnityEngine.Debug.Log($"   Sequential布局:        {sequentialSize} 字节");
            UnityEngine.Debug.Log($"   Explicit布局（标准）:   {explicitSize} 字节");
            UnityEngine.Debug.Log($"   Explicit布局（优化）:   {explicitOptimizedSize} 字节");

            // 计算理论最小大小
            int theoreticalMin = 4 + 24; // 4个byte + 3个long = 28字节

            UnityEngine.Debug.Log($"\n理论最小大小（无填充）: {theoreticalMin} 字节");
            UnityEngine.Debug.Log($"\n内存效率对比:");
            UnityEngine.Debug.Log($"   Auto布局效率:          {100 * theoreticalMin / autoSize}% (填充: {autoSize - theoreticalMin} 字节)");
            UnityEngine.Debug.Log($"   Sequential布局效率:    {100 * theoreticalMin / sequentialSize}% (填充: {sequentialSize - theoreticalMin} 字节)");
            UnityEngine.Debug.Log($"   Explicit布局（标准）效率: {100 * theoreticalMin / explicitSize}% (填充: {explicitSize - theoreticalMin} 字节)");
            UnityEngine.Debug.Log($"   Explicit布局（优化）效率: {100 * theoreticalMin / explicitOptimizedSize}% (填充: {explicitOptimizedSize - theoreticalMin} 字节)");

            UnityEngine.Debug.Log($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            UnityEngine.Debug.Log("📌 三种布局方式的特点总结:");
            UnityEngine.Debug.Log("\n1. Auto 布局 (LayoutKind.Auto):");
            UnityEngine.Debug.Log("   ✓ CLR自动优化内存对齐");
            UnityEngine.Debug.Log("   ✗ 不能与非托管代码互操作");
            UnityEngine.Debug.Log("   ✗ 不保证字段顺序");
            UnityEngine.Debug.Log("   ✓ 最简单，让CLR决定");

            UnityEngine.Debug.Log("\n2. Sequential 布局 (LayoutKind.Sequential):");
            UnityEngine.Debug.Log("   ✓ 保持字段声明顺序");
            UnityEngine.Debug.Log("   ✓ 可以与非托管代码互操作");
            UnityEngine.Debug.Log("   ✗ 可能产生内存填充浪费");
            UnityEngine.Debug.Log("   ✓ 适合需要特定字段顺序的场景");

            UnityEngine.Debug.Log("\n3. Explicit 布局 (LayoutKind.Explicit):");
            UnityEngine.Debug.Log("   ✓ 完全控制字段内存位置");
            UnityEngine.Debug.Log("   ✓ 可以手动优化到最小内存占用");
            UnityEngine.Debug.Log("   ✓ 可以与非托管代码互操作");
            UnityEngine.Debug.Log("   ✗ 需要手动指定每个字段的偏移");
            UnityEngine.Debug.Log("   ✗ 容易出错（字段可能重叠）");
            UnityEngine.Debug.Log("   ✓ 最适合精确控制内存布局的场景");

            UnityEngine.Debug.Log("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
        }
    }
}
