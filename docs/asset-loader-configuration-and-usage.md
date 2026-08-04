# 资源加载器配置与使用

本文说明 `BundleMaster`、`YooAsset`、`Addressables` 和 `Resources` 四种后端的项目配置、运行条件与统一 API 用法。架构背景见 [可插拔资源加载器设计](plans/2026-08-04-pluggable-asset-loader-design.md)。

## 1. 公共配置

启动时读取：

```text
Assets/Resources/AssetBackendConfig.asset
```

字段含义：

| 字段 | 说明 |
|---|---|
| `Backend` | 启动时选择的资源后端，运行中不能切换 |
| `DefaultPackageName` | `packageName == null` 时使用的默认包名，默认 `AllBundle` |
| `YooPlayMode` | YooAsset 的 EditorSimulate、Offline 或 Host 模式 |
| `YooEditorSimulateRoot` | YooAsset 编辑器模拟文件系统的包根目录 |

公共约定：

- 业务地址统一写成 Unity 工程路径，例如 `Assets/HotRes/egg.png`。
- 热更 DLL 固定显式使用 `DllBundle`，普通资源默认使用 `DefaultPackageName`。
- `Load`、`LoadAsync` 每调用一次，就应对应调用一次 `Unload(path)`。
- `LoadHandleAsync` 返回独立租约，应通过 `Dispose()` 或 `AssetService.Unload(handle)` 精确释放。
- 同一次加载不要同时使用 path 卸载和 handle 卸载。
- 场景加载后必须检查 `ISceneHandle.Succeeded`；失败原因在 `Error`。

后端能力：

| 后端 | 分包 | 热更新 | 地址来源 | 场景要求 |
|---|---|---|---|---|
| BundleMaster | 原生分包 | `AssetLoadMode.Build` | BundleMaster 构建配置 | 加入对应 BundleMaster 场景配置 |
| YooAsset | `ResourcePackage` | 仅 Host 模式 | Collector 地址规则 | 加入对应 YooAsset Package |
| Addressables | 全局地址，package 参数不分流 | Remote Catalog | Addressable Address | 标记为 Addressable |
| Resources | 忽略 package | 不支持 | `Assets/Resources` 相对路径 | 加入 Unity Build Settings |

## 2. BundleMaster

### 配置步骤

1. 将 `Backend` 设为 `BundleMaster`。
2. 在 BundleMaster BuildSettings 中保留普通资源包 `AllBundle`，或将 `DefaultPackageName` 改为实际普通资源包名。
3. 重新执行 HybridCLR DLL 编译和补充元数据裁剪，确保生成 `Strip_ETTask.dll.bytes`。
4. 创建 `DllBundle`，收集以下目录：
   - `Assets/HotDll/HotUpdateDlls`
   - `Assets/HotDll/AOTAssemblyMetadataDlls`
5. 将启动场景 `Assets/Scenes_HotFix/ToolScene.unity` 加入普通资源包的 Scene 列表。
6. 发布模式将 `AssetComponentConfig.AssetLoadMode` 设为 `Build` 并生成所有分包。

Launch 使用以下远端根目录：

```text
{GameConfig.RemotePath}/{ServerVersion}/AssetBundles
```

该目录下应保持 BundleMaster 生成的 `AllBundle`、`DllBundle` 及版本/日志文件结构。`Develop` 和 `Local` 模式不会进入远端更新流程。

### 检查项

- `DefaultPackageName` 与 BuildSettings 的 `BuildName` 完全一致。
- DLL 文件在构建后仍以 `.bytes` 作为资源加载。
- `Assets/HotDll/AOTAssemblyMetadataDlls/Strip_ETTask.dll.bytes` 已重新生成并收进 `DllBundle`，不要沿用拆分前的旧元数据目录。
- Scene 路径保持完整工程路径，不要只写场景名。
- 服务器不可用或下载取消时，Launch 会停留在更新状态，不再进入 DLL 加载。

## 3. YooAsset

项目使用 YooAsset `3.0.5`。

### Package 与地址

至少创建两个 `ResourcePackage`：

| Package | 内容 |
|---|---|
| `AllBundle` 或 `DefaultPackageName` | 普通资源和启动场景 |
| `DllBundle` | HotUpdate DLL 与 AOT Metadata |

Collector 的 Address Rule 必须产生完整工程路径：

```text
Assets/HotRes/egg.png
Assets/Scenes_HotFix/ToolScene.unity
Assets/HotDll/HotUpdateDlls/Game.dll.bytes
```

如果使用自定义 Address Rule，应先在 YooAsset 构建报告中确认最终 Location 与上述路径一致。

### 运行模式

`EditorSimulate`：

- 用 YooAsset 编辑器模拟构建生成 Package Root。
- 将默认资源包对应的根目录写入 `YooEditorSimulateRoot`。
- 当前 Editor 启动流程会跳过热更 DLL，适合普通资源和场景联调。

`Offline`：

- 将构建产物按 YooAsset 内置文件系统要求复制到 StreamingAssets。
- 不请求远端版本，也不执行下载。

`Host`：

- 内置文件放入 StreamingAssets，远端文件部署到 `BundleServerUrl` 对应目录。
- 当前 Launch 会把 `BundleServerUrl` 设置为公共版本目录：

```text
{GameConfig.RemotePath}/{ServerVersion}/AssetBundles
```

- 该 URL 下的文件名结构必须与 YooAsset Host 构建产物一致。
- 初始化、版本请求、Manifest 或 Downloader 任一步失败都会终止 Launch。

### 构建检查项

- 两个 Package 都已构建，名称大小写一致。
- 启动场景属于默认 Package。
- DLL 与 Metadata 属于 `DllBundle`。
- Host 模式已部署 Package Manifest 和全部远端 Bundle。
- CDN 若需要备用域名，应扩展 `HostRemoteService`，当前实现只使用一个根地址。

## 4. Addressables

Addressables 不按 `packageName` 选择 Group；`AllBundle` 和 `DllBundle` 参数只用于保持统一接口，实际查找完全依赖 Address。

### 必须登记的地址

至少将以下内容标记为 Addressable，并把 Address 设置为完整工程路径：

```text
Assets/Scenes_HotFix/ToolScene.unity
Assets/HotDll/HotUpdateDlls/Game.dll.bytes
Assets/HotDll/AOTAssemblyMetadataDlls/*.bytes
Assets/HotRes/...
```

场景资源应放入 Scene Group；DLL 和普通资源可按更新策略拆分 Group。Group 名称不参与运行时寻址。

### 本地与远端配置

本地验证：

1. 设置合适的 Play Mode Script。
2. 使用 `Use Asset Database` 或先执行一次 New Build。
3. 确认 Addressables Analyze 没有重复地址或场景依赖问题。

远端更新：

1. 为需要热更新的 Group 启用 Remote Build Path 和 Remote Load Path。
2. 在 AddressableAssetSettings 中启用远端 Catalog。
3. 首包执行 `New Build -> Default Build Script` 并保存 `addressables_content_state.bin`。
4. 后续版本使用 `Update a Previous Build`。
5. 部署新 Catalog、Hash 和远端 Bundle，保证 Player 的 Remote Load Path 可访问。

运行时检查 Catalog 更新后，会先更新 Catalog、计算新 Locator 全部 Key 的下载大小，再下载依赖。任何一步失败都会向 Launch 传播异常。

### 检查项

- Address 必须是 `Assets/...`，不能保留默认文件名地址。
- `ToolScene` 必须标记为 Addressable，仅加入 Build Settings 不够。
- DLL 必须以 `TextAsset` 可加载的 `.bytes` 文件登记。
- Remote Catalog 未启用时，`CheckForCatalogUpdates` 不会发现内容更新。

## 5. Resources

Resources 后端用于内置资源验证，不执行资源或 DLL 热更新。

### 资源规则

资源必须位于：

```text
Assets/Resources/...
```

业务仍传完整路径和扩展名：

```csharp
await AssetService.LoadAsync<TextAsset>("Assets/Resources/Config/GameConfig.bytes");
```

内部会转换为：

```text
Config/GameConfig
```

不在 `Assets/Resources` 下的普通资源会返回 `null` 并记录错误。

### 场景与代码限制

- Resources 场景通过 `SceneManager` 加载，必须加入 `File -> Build Settings -> Scenes In Build`。
- 调用仍使用完整工程路径，例如 `Assets/Scenes_Builtin/Test.unity`。
- Resources 模式会跳过 `DllBundle`，Player 中只能运行已经编入 Player 的代码。
- 如果启动场景依赖 HybridCLR 热更程序集，Resources 模式不适合作为完整 Player 启动后端。
- Resources 后端会在版本请求之前直接跳过更新，可离线运行。

## 6. 统一 API 示例

按路径加载：

```csharp
Sprite sprite = await AssetService.LoadAsync<Sprite>(BPath.Assets_HotRes_egg__png);
if (sprite == null)
    throw new InvalidOperationException("sprite load failed");

AssetService.Unload(BPath.Assets_HotRes_egg__png);
```

按句柄加载：

```csharp
IAssetHandle<Sprite> handle =
    await AssetService.LoadHandleAsync<Sprite>(BPath.Assets_HotRes_egg__png);

try
{
    Sprite sprite = handle.Asset;
    if (!handle.IsValid)
        throw new InvalidOperationException("sprite load failed");
}
finally
{
    handle.Dispose();
}
```

加载场景：

```csharp
ISceneHandle scene = await AssetService.LoadSceneAsync(
    "Assets/Scenes_HotFix/ToolScene.unity",
    mode: LoadSceneMode.Additive);

if (!scene.Succeeded)
    throw new InvalidOperationException(scene.Error);
```

直接调用更新接口：

```csharp
var packages = new Dictionary<string, bool>
{
    [AssetService.Options.DefaultPackageName] = false,
    ["DllBundle"] = false,
};

IUpdateInfo update = await AssetService.CheckUpdateAsync(packages);
if (update.NeedUpdate)
    await AssetService.DownloadUpdateAsync(update);
```

## 7. 常见故障定位

| 现象 | 优先检查 |
|---|---|
| `package not initialized` | Package 名称、初始化返回值、YooAsset Manifest |
| 资源返回 `null` | Address/Location 是否为完整 `Assets/...` 路径 |
| 场景加载失败 | BM/Yoo/Addressables 的场景收集配置，或 Resources Build Settings |
| DLL 加载失败 | `DllBundle` 是否包含 `.bytes` 文件，地址是否完整 |
| Addressables 无更新 | Remote Catalog、Content Update Build、Remote Load Path |
| YooAsset Host 请求 404 | `BundleServerUrl` 与 Host 构建文件结构是否一致 |
| Resources 路径错误 | 文件是否确实位于 `Assets/Resources` 下 |
| 重复加载后资源提前失效 | 每次 Load 是否只执行了一次对应的 Unload，是否混用了 path 与 handle 释放 |
