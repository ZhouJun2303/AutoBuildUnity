using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"BundleMaster.dll",
		"MyTaskTest.dll",
		"System.dll",
		"UnityEngine.AndroidJNIModule.dll",
		"UnityEngine.CoreModule.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// ET.ETAsyncTaskMethodBuilder<object>
	// ET.ETTask<object>
	// System.Action<object,int,byte,byte>
	// System.Action<object>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.Enumerator<object,byte>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,byte>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,int>
	// System.Collections.Generic.Dictionary.KeyCollection<object,byte>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,byte>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,int>
	// System.Collections.Generic.Dictionary.ValueCollection<object,byte>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary<int,int>
	// System.Collections.Generic.Dictionary<object,byte>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.EqualityComparer<byte>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,byte>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,byte>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,byte>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.KeyValuePair<int,int>
	// System.Collections.Generic.KeyValuePair<object,byte>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List.SynchronizedList<object>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<byte>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.Queue.Enumerator<object>
	// System.Collections.Generic.Queue<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<object>
	// System.Func<System.Threading.Tasks.VoidTaskResult>
	// System.Func<int>
	// System.Func<object,System.Threading.Tasks.VoidTaskResult>
	// System.Func<object,object,object>
	// System.Func<object,object>
	// System.Func<object>
	// System.IEquatable<Long2>
	// System.Predicate<object>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<object>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<object>
	// System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.TaskAwaiter<object>
	// System.Threading.Tasks.Task.<>c<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.Task.<>c<object>
	// System.Threading.Tasks.Task<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.Task<object>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass35_0<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass35_0<object>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass35_1<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass35_1<object>
	// System.Threading.Tasks.TaskFactory<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.TaskFactory<object>
	// }}

	public void RefMethods()
	{
		// ET.ETTask<object> BM.AssetComponent.LoadAsync<object>(string,string)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,launchGame.<LoadResTest>d__12>(object&,launchGame.<LoadResTest>d__12&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<launchGame.<LoadResTest>d__12>(launchGame.<LoadResTest>d__12&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<BM.AssetComponent.<LoadAsync>d__6<object>>(BM.AssetComponent.<LoadAsync>d__6<object>&)
		// System.Void MyTaskTest.MyAsyncTaskMethodBuilder.AwaitOnCompleted<object,launchGame.<Test1>d__6>(object&,launchGame.<Test1>d__6&)
		// System.Void MyTaskTest.MyAsyncTaskMethodBuilder.AwaitOnCompleted<object,launchGame.<Test2>d__7>(object&,launchGame.<Test2>d__7&)
		// System.Void MyTaskTest.MyAsyncTaskMethodBuilder.AwaitOnCompleted<object,launchGame.<Test5>d__10>(object&,launchGame.<Test5>d__10&)
		// System.Void MyTaskTest.MyAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,launchGame.<Test5>d__10>(System.Runtime.CompilerServices.TaskAwaiter&,launchGame.<Test5>d__10&)
		// System.Void MyTaskTest.MyAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,launchGame.<Test5>d__10>(object&,launchGame.<Test5>d__10&)
		// System.Void MyTaskTest.MyAsyncTaskMethodBuilder.Start<launchGame.<Test1>d__6>(launchGame.<Test1>d__6&)
		// System.Void MyTaskTest.MyAsyncTaskMethodBuilder.Start<launchGame.<Test2>d__7>(launchGame.<Test2>d__7&)
		// System.Void MyTaskTest.MyAsyncTaskMethodBuilder.Start<launchGame.<Test5>d__10>(launchGame.<Test5>d__10&)
		// object[] System.Array.Empty<object>()
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitOnCompleted<object,launchGame.<Test3>d__8>(object&,launchGame.<Test3>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitOnCompleted<object,launchGame.<Test4>d__9>(object&,launchGame.<Test4>d__9&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitOnCompleted<object,launchGame.<Test3>d__8>(object&,launchGame.<Test3>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitOnCompleted<object,launchGame.<Test4>d__9>(object&,launchGame.<Test4>d__9&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<launchGame.<Test3>d__8>(launchGame.<Test3>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<launchGame.<Test4>d__9>(launchGame.<Test4>d__9&)
		// object UnityEngine.AndroidJNIHelper.ConvertFromJNIArray<object>(System.IntPtr)
		// System.IntPtr UnityEngine.AndroidJNIHelper.GetFieldID<object>(System.IntPtr,string,bool)
		// object UnityEngine.AndroidJavaObject.FromJavaArrayDeleteLocalRef<object>(System.IntPtr)
		// object UnityEngine.AndroidJavaObject.GetStatic<object>(string)
		// object UnityEngine.AndroidJavaObject._GetStatic<object>(string)
		// object UnityEngine.Component.GetComponent<object>()
		// object UnityEngine._AndroidJNIHelper.ConvertFromJNIArray<object>(System.IntPtr)
		// System.IntPtr UnityEngine._AndroidJNIHelper.GetFieldID<object>(System.IntPtr,string,bool)
	}
}