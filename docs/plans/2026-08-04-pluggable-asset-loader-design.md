# 可插拔资源加载器设计

日期：2026-08-04  
状态：已实现（首版）

配置与使用说明：[资源加载器配置与使用](../asset-loader-configuration-and-usage.md)

## 目标

用统一泛型接口封装四种资源后端，启动前通过配置选择其一，整局游戏只使用该后端：

- BundleMaster（现有，支持热更）
- YooAsset（支持热更）
- Addressables（支持热更）
- Resources（原生，不热更）

业务与 Launch 流程只依赖门面，不直接依赖具体框架。

## 约束（已确认）

1. 切换时机：启动前配置切换（非整局热切换）
2. 接口范围：常用完整集（加载/卸载/场景/存在性/热更/Tick）
3. 寻址：统一 Unity 工程路径 `Assets/...`（保留 BPath）
4. 异步：继续使用 `ETTask` / `ETTask<T>`
5. Resources：仅本地加载，热更 API 空实现成功返回

## 架构

```
业务 / Launch FSM
       │
       ▼
  AssetService  (静态门面)
       │
       ▼
  IAssetLoader
       │
  ┌────┼────────────┬──────────────┐
  ▼    ▼            ▼              ▼
BundleMaster  YooAsset   Addressables   Resources
 Adapter       Adapter     Adapter        Adapter
```

- 配置：`AssetBackendConfig`（ScriptableObject，置于 Resources）
- 字段：`Backend`（枚举）、`DefaultPackageName`（默认 `AllBundle`）、CDN/模式等后端可选参数
- 启动最早：`LaunchAOT` → `AssetService.Bootstrap(config)` → 创建对应 Adapter

## 接口

独立程序集 `Game.AssetCore`（AOT / HotFix 均可引用，不依赖具体资源框架）。

```csharp
public enum AssetBackendType
{
    BundleMaster = 0,
    YooAsset = 1,
    Addressables = 2,
    Resources = 3,
}

public interface IAssetLoader
{
    AssetBackendType Backend { get; }
    bool SupportsHotUpdate { get; }

    ETTask InitializeAsync(AssetRuntimeOptions options);
    ETTask<bool> InitializePackageAsync(string packageName);
    void Tick();
    void Dispose();

    T Load<T>(string assetPath, string packageName = null) where T : Object;
    ETTask<T> LoadAsync<T>(string assetPath, string packageName = null) where T : Object;
    ETTask<IAssetHandle<T>> LoadHandleAsync<T>(string assetPath, string packageName = null) where T : Object;

    ETTask<ISceneHandle> LoadSceneAsync(
        string scenePath,
        string packageName = null,
        LoadSceneMode mode = LoadSceneMode.Single);

    void Unload(string assetPath, string packageName = null);
    void Unload(IAssetHandle handle);
    bool Exists(string assetPath, string packageName = null);

    ETTask<IUpdateInfo> CheckUpdateAsync(IReadOnlyDictionary<string, bool> packages);
    ETTask DownloadUpdateAsync(IUpdateInfo info, IProgress<UpdateProgress> progress = null);
}
```

句柄与更新信息：

- `IAssetHandle` / `IAssetHandle<T>`：可 `Dispose` 释放
- `ISceneHandle`：进度与完成状态
- `IUpdateInfo`：`NeedUpdate`、`TotalBytes`、`Packages`

门面示例：

```csharp
var sprite = await AssetService.LoadAsync<Sprite>(BPath.Assets_HotRes_egg__png);
AssetService.Unload(BPath.Assets_HotRes_egg__png);
```

约定：

- `packageName == null` → `DefaultPackageName`
- DLL 包显式传 `"DllBundle"`
- 简单业务用 path 版 Load/Unload；需要精细释放时用 Handle 版

## 适配器差异

| 能力 | BundleMaster | YooAsset | Addressables | Resources |
|------|--------------|----------|--------------|-----------|
| 路径 | 原样 | location = 路径 | address = 路径 | 裁成 Resources 相对路径 |
| 分包 | AllBundle / DllBundle | 同名 ResourcePackage | Label/Group 或全局 | 忽略 |
| 热更 | 现有流程 | Version + Downloader | Catalog 更新 + 依赖下载 | 跳过 |
| 句柄 | 包装 LoadHandler | AssetHandle | AsyncOperationHandle | 自建引用计数 |
| Tick | AssetComponent.Update | 按需 | 空 | 空 |

### Resources 路径规则

- 输入：`Assets/Resources/Foo/Bar.asset`
- 内部：`Foo/Bar`（去掉 `Assets/Resources/` 前缀与扩展名）
- 不在 Resources 下的路径：`Exists` 返回 false，Load 返回 null 并打日志

### Resources / 热更 DLL

Resources 模式不承担热更 DLL 加载。该模式下：

- Editor：可继续直接使用本地程序集（与现有 Editor 跳过 LoadDll 一致）
- Runtime：若仍走 LoadDll，应明确失败或跳过并文档说明「Resources 仅用于内置资源验证」

## Launch 改造

状态机不变：`Launch → UpdateAssetBundle → LoadDll → StartGame`

替换点：

1. `GameProduceUpdateAssetBundle` → `AssetService` 的 Check/Download/Tick；若 `!SupportsHotUpdate` 直接下一状态
2. `GameProduceLoadDll` / `GameProduceStartGame` / `launchGame` → `AssetService.LoadAsync` 等
3. Editor 快捷路径可保留跳过 Update/Dll，但资源加载仍走 `AssetService`

## 目录与程序集

```
Assets/BundleMaster/ETTaskAsync/    # 通用 ETTask 实现，不依赖具体资源后端
  ETTask.asmdef

Assets/Scripts_AssetCore/           # 接口、门面、Config、句柄模型
  Game.AssetCore.asmdef             # 仅引用 ETTask

Assets/Scripts_AssetAdapters/       # 四个 Adapter
  Game.AssetAdapters.asmdef         # 引用 ETTask + AssetCore + BM/Yoo/Addressables
  BundleMasterAssetLoader.cs
  YooAssetAssetLoader.cs
  AddressablesAssetLoader.cs
  ResourcesAssetLoader.cs
```

`ETTask` 是独立的通用异步程序集。`Game.AssetCore` 不引用 BundleMaster，具体后端依赖只存在于 Adapters。  
`GameAOT` / `Game` 引用 `Game.AssetCore`（及必要时 Adapters 的工厂注册）。  
工厂放在 Adapters 或 AOT 启动处，按枚举 `new` 对应实现，避免 Core 反向依赖具体框架。

## 错误处理

1. **未 Bootstrap**：门面 API 抛明确异常或 `Debug.LogError` 并返回默认失败结果
2. **Load 失败**：异步返回 `null`，同步返回 `null`，统一日志带 backend + path + package
3. **热更失败**：`DownloadUpdateAsync` 传播错误；Launch 层决定重试/中止（保持现有行为可先 LogError）
4. **不支持的操作**：Resources 的更新 API 成功空操作；场景若不在 Resources 则失败并日志
5. **切换后端**：不迁移已加载资源；必须在 Bootstrap 前选定，运行中禁止更换（门面可断言）

## 验收标准

1. 配置切到 BundleMaster：现有启动、DLL 热更、`launchGame` 加载 Sprite 行为与现在一致
2. 配置切到 Resources：能 `Load`/`LoadAsync` 加载 `Assets/Resources` 下资源；更新步骤被跳过
3. 配置切到 YooAsset：在完成对应打包与地址=`Assets/...` 约定后，能 Initialize + LoadAsync 通路径资源
4. 配置切到 Addressables：同上，address 使用工程路径
5. 业务代码（HotFix）无直接 `using BM` 加载调用（可保留 BPath 常量所在命名空间）
6. AOT Launch 三处 Produce 不再直接调用 `AssetComponent`（仅 Adapter 内允许）

## 实现顺序建议

1. AssetCore：接口、句柄、Config、AssetService 门面
2. BundleMasterAdapter + Launch/HotFix 替换调用（默认后端，保证不回归）
3. ResourcesAdapter（便于无打包验证切换）
4. YooAssetAdapter（含构建地址约定说明）
5. AddressablesAdapter（含 address 命名约定）
6. 文档补充各后端打包检查清单

## 非目标（本期不做）

- 运行中热切换后端
- 多后端并存（按包分流）
- 引入 UniTask / 替换 ETTask
- 统一三家构建管线为单一菜单（可后续加）
