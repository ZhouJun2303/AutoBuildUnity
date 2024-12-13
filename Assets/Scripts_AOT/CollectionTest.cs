using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectionTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        MyTestDictionary<int, int> myTestDictionary = new MyTestDictionary<int, int>();
        myTestDictionary.Add(1, 2);
        myTestDictionary.Add(1, 2);
        myTestDictionary.Add(1, 2);
    }

    // Update is called once per frame
    void Update()
    {

    }
}

public class MyTestDictionary<T1, T2>
{
    Dictionary<T1, T2> m = new Dictionary<T1, T2>();

    public void Add(T1 key, T2 value)
    {
        if (m.ContainsKey(key))
        {
            m[key] = value;
        }
        else
        {
            m.Add(key, value);
        }
    }
}

//泛型委托
delegate T NumberChange<T>(T value);
delegate void NumberChange1<T>(T value);
delegate void NumberChange2<T1, T2>(T1 value1, T2 value2);

public class TestDelegate
{
    public int AddNum(int num)
    {
        return 1;
    }

    public void AddNum1(int num)
    {

    }

    public void AddNum2(string num, int a)
    {

    }
    public TestDelegate()
    {
        NumberChange<int> numberChange = new NumberChange<int>(AddNum);
        NumberChange1<int> numberChange1 = new NumberChange1<int>(AddNum1);
        NumberChange2<string, int> numberChange2 = new NumberChange2<string, int>(AddNum2);
    }
}

//泛型约束
public class Helper<T> where T : new()//T：new() （类型参数必须具有无参数的公共构造函数。当与其他约束一起使用时new() 约束必须最后指定）
{
}



public interface IExample
{
    void Display();
}

// 基类约束
public class BaseClass
{
    public void ShowBase()
    {
        Console.WriteLine("Base class method.");
    }
}

// 泛型类，它结合了多个约束
public class ComplexGeneric<T> where T : BaseClass, IExample, new()
{
    public T Value { get; set; }

    public ComplexGeneric()
    {
        // 创建一个 T 的实例
        Value = new T();
    }

    public void CallMethods()
    {
        // 调用泛型类型 T 实现的 IExample 接口方法
        Value.Display();

        // 调用 T 的基类方法
        if (Value is BaseClass baseClassInstance)
        {
            baseClassInstance.ShowBase();
        }
    }
}

// 一个实现了 IExample 接口并继承自 BaseClass 的类
public class DerivedClass : BaseClass, IExample
{
    public void Display()
    {
        Console.WriteLine("Derived class Display method.");
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        // 使用 DerivedClass 作为 T 的具体类型
        ComplexGeneric<DerivedClass> complex = new ComplexGeneric<DerivedClass>();

        // 调用方法
        complex.CallMethods();
    }
}