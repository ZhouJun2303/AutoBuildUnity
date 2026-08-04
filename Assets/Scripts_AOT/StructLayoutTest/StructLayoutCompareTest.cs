using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 合并对比测试：同时展示 Auto 和 Sequential 布局的差异
/// </summary>
public class StructLayoutCompareTest : MonoBehaviour
{
    unsafe void Start()
    {
        UnityEngine.Debug.Log("\n╔════════════════════════════════════════════════════════════╗");
        UnityEngine.Debug.Log("║   StructLayout(LayoutKind.Auto) vs Sequential 对比测试     ║");
        UnityEngine.Debug.Log("╚════════════════════════════════════════════════════════════╝\n");

        // 创建两个结构体实例
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

        // 获取 Auto 布局的信息
        int autoSize = sizeof(StructLayoutAuto);

        // 使用 stackalloc 在栈上分配结构体，然后获取地址
        StructLayoutAuto* pAuto = stackalloc StructLayoutAuto[1];
        *pAuto = autoStruct;  // 复制结构体到栈上

        byte* autoBase = (byte*)pAuto;
        var autoFields = new[]
        {
            (name: "byte1", offset: (long)&pAuto->byte1 - (long)autoBase),
            (name: "long1", offset: (long)&pAuto->long1 - (long)autoBase),
            (name: "byte2", offset: (long)&pAuto->byte2 - (long)autoBase),
            (name: "long2", offset: (long)&pAuto->long2 - (long)autoBase),
            (name: "byte3", offset: (long)&pAuto->byte3 - (long)autoBase),
            (name: "long3", offset: (long)&pAuto->long3 - (long)autoBase),
            (name: "byte4", offset: (long)&pAuto->byte4 - (long)autoBase)
        };
        Array.Sort(autoFields, (a, b) => a.offset.CompareTo(b.offset));

        // 获取 Sequential 布局的信息
        int sequentialSize = Marshal.SizeOf(typeof(StructLayoutSequential));
        var sequentialFields = new[]
        {
            (name: "byte1", offset: Marshal.OffsetOf(typeof(StructLayoutSequential), nameof(StructLayoutSequential.byte1)).ToInt64()),
            (name: "long1", offset: Marshal.OffsetOf(typeof(StructLayoutSequential), nameof(StructLayoutSequential.long1)).ToInt64()),
            (name: "byte2", offset: Marshal.OffsetOf(typeof(StructLayoutSequential), nameof(StructLayoutSequential.byte2)).ToInt64()),
            (name: "long2", offset: Marshal.OffsetOf(typeof(StructLayoutSequential), nameof(StructLayoutSequential.long2)).ToInt64()),
            (name: "byte3", offset: Marshal.OffsetOf(typeof(StructLayoutSequential), nameof(StructLayoutSequential.byte3)).ToInt64()),
            (name: "long3", offset: Marshal.OffsetOf(typeof(StructLayoutSequential), nameof(StructLayoutSequential.long3)).ToInt64()),
            (name: "byte4", offset: Marshal.OffsetOf(typeof(StructLayoutSequential), nameof(StructLayoutSequential.byte4)).ToInt64())
        };
        Array.Sort(sequentialFields, (a, b) => a.offset.CompareTo(b.offset));

        // 显示结构体大小对比
        UnityEngine.Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        UnityEngine.Debug.Log($"📊 结构体大小对比:");
        UnityEngine.Debug.Log($"   Auto布局:      {autoSize} 字节");
        UnityEngine.Debug.Log($"   Sequential布局: {sequentialSize} 字节");
        UnityEngine.Debug.Log($"   差异:          {Math.Abs(sequentialSize - autoSize)} 字节");
        if (autoSize < sequentialSize)
            UnityEngine.Debug.Log($"   ✓ Auto布局节省了 {sequentialSize - autoSize} 字节");
        else if (autoSize > sequentialSize)
            UnityEngine.Debug.Log($"   ✗ Auto布局多用了 {autoSize - sequentialSize} 字节（这种情况很少见）");
        else
            UnityEngine.Debug.Log($"   = 大小相同");

        // 显示字段声明顺序
        UnityEngine.Debug.Log($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        UnityEngine.Debug.Log($"📝 字段声明顺序（两者相同）:");
        UnityEngine.Debug.Log($"   byte1 → long1 → byte2 → long2 → byte3 → long3 → byte4");

        // 显示 Auto 布局的实际内存排列
        UnityEngine.Debug.Log($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        UnityEngine.Debug.Log($"🔀 Auto布局 - 实际内存排列顺序（CLR自动优化后）:");
        for (int i = 0; i < autoFields.Length; i++)
        {
            UnityEngine.Debug.Log($"   {i + 1}. {autoFields[i].name,6} @ 偏移 {autoFields[i].offset,2}");
        }

        // 显示 Sequential 布局的实际内存排列
        UnityEngine.Debug.Log($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        UnityEngine.Debug.Log($"📐 Sequential布局 - 实际内存排列顺序（保持声明顺序）:");
        for (int i = 0; i < sequentialFields.Length; i++)
        {
            UnityEngine.Debug.Log($"   {i + 1}. {sequentialFields[i].name,6} @ 偏移 {sequentialFields[i].offset,2}");
        }

        // 对比字段排列顺序是否相同
        UnityEngine.Debug.Log($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        bool orderDifferent = false;
        for (int i = 0; i < autoFields.Length; i++)
        {
            if (autoFields[i].name != sequentialFields[i].name)
            {
                orderDifferent = true;
                break;
            }
        }

        if (orderDifferent)
        {
            UnityEngine.Debug.Log($"✅ 字段排列顺序不同！");
            UnityEngine.Debug.Log($"   Auto布局已重排字段以优化内存对齐");
        }
        else
        {
            UnityEngine.Debug.Log($"⚠️  字段排列顺序相同（这种情况下Auto布局没有重排）");
            UnityEngine.Debug.Log($"   可能原因：当前字段顺序已经接近最优，或者CLR认为不需要重排");
        }

        // 详细的内存布局可视化
        UnityEngine.Debug.Log($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        UnityEngine.Debug.Log($"📊 内存布局详细对比:");

        UnityEngine.Debug.Log($"\n   [Auto布局] ({autoSize} 字节):");
        PrintMemoryLayout(autoFields, autoSize);

        UnityEngine.Debug.Log($"\n   [Sequential布局] ({sequentialSize} 字节):");
        PrintMemoryLayout(sequentialFields, sequentialSize);

        // 总结和解释
        UnityEngine.Debug.Log($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        UnityEngine.Debug.Log($"📌 总结:");
        UnityEngine.Debug.Log($"   • Auto布局：CLR可以自由重排字段，优化内存对齐");
        UnityEngine.Debug.Log($"   • Sequential布局：严格按声明顺序，可能产生padding浪费");
        UnityEngine.Debug.Log($"   • Auto布局不能与非托管代码互操作（Marshal）");
        UnityEngine.Debug.Log($"   • Sequential布局可以与非托管代码互操作");

        if (!orderDifferent)
        {
            UnityEngine.Debug.Log($"\n⚠️  重要说明：Auto布局没有重排字段的原因");
            UnityEngine.Debug.Log($"   1. CLR的Auto布局不一定总是重排字段");
            UnityEngine.Debug.Log($"      - Auto布局的目的是优化内存对齐，而不是强制重排");
            UnityEngine.Debug.Log($"      - CLR可能认为当前顺序已经足够好");
            UnityEngine.Debug.Log($"   2. Unity/IL2CPP环境的影响");
            UnityEngine.Debug.Log($"      - Unity使用IL2CPP时，布局行为可能与标准.NET不同");
            UnityEngine.Debug.Log($"      - Mono和IL2CPP的实现可能有差异");
            UnityEngine.Debug.Log($"   3. 实际应用建议");
            UnityEngine.Debug.Log($"      - 如果需要特定的字段顺序，使用Sequential");
            UnityEngine.Debug.Log($"      - 如果需要与非托管代码互操作，必须使用Sequential或Explicit");
            UnityEngine.Debug.Log($"      - Auto布局的优势在于CLR可以自由优化，但不保证会重排");
        }
        UnityEngine.Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
    }

    void PrintMemoryLayout((string name, long offset)[] fields, int totalSize)
    {
        System.Text.StringBuilder[] lines = new System.Text.StringBuilder[(totalSize + 7) / 8];
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = new System.Text.StringBuilder($"    偏移 {i * 8,2}: ");
        }

        for (int i = 0; i < totalSize; i++)
        {
            // 查找当前位置是哪个字段
            string fieldName = "";
            for (int j = 0; j < fields.Length; j++)
            {
                long fieldOffset = fields[j].offset;
                long fieldEnd = fieldOffset + GetFieldSize(fields[j].name);
                if (i >= fieldOffset && i < fieldEnd)
                {
                    fieldName = fields[j].name;
                    break;
                }
            }

            int lineIndex = i / 8;
            int colIndex = i % 8;

            if (fieldName.StartsWith("long"))
                lines[lineIndex].Append("L");  // L = Long (8字节)
            else if (fieldName.StartsWith("int"))
                lines[lineIndex].Append("I");  // I = Int (4字节)
            else if (fieldName.StartsWith("short"))
                lines[lineIndex].Append("S");  // S = Short (2字节)
            else if (fieldName.StartsWith("byte"))
                lines[lineIndex].Append("B");  // B = Byte (1字节)
            else
                lines[lineIndex].Append(".");  // . = Padding
        }

        foreach (var line in lines)
        {
            UnityEngine.Debug.Log(line.ToString());
        }

        UnityEngine.Debug.Log("    说明: L=Long(8字节), I=Int(4字节), S=Short(2字节), B=Byte(1字节), .=填充");
    }

    int GetFieldSize(string fieldName)
    {
        if (fieldName.StartsWith("long")) return 8;
        if (fieldName.StartsWith("int")) return 4;
        if (fieldName.StartsWith("short")) return 2;
        if (fieldName.StartsWith("byte")) return 1;
        return 1;
    }
}
