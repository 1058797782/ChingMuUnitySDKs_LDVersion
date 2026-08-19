# ChingMu Unity SDK - LD Version

青瞳 Unity SDK v3.0.1 的 Unity Package Manager 版本。

本仓库保留青瞳官方仓库历史和原始 unitypackage 导入基线，同时在 `main` 提供可直接从 Git 安装的包结构。安装后运行时代码和 native 插件位于 `Packages/com.lastdream.chingmu`，不会把 SDK 主体复制到使用方工程的 `Assets` 目录。

## 当前状态

| 项目 | 状态 |
|---|---|
| 官方基线 | ChingMu Unity SDK v3.0.1 |
| 包版本 | `3.0.1-ld.2`（优化候选） |
| Unity 基线 | `2022.3.60f1` |
| 目标平台 | Windows x86_64；官方 `x86` 目录中的 DLL 实际也是 x86_64 |
| Runtime 源码 | 24 个 C# 文件及自动化测试 |
| Native 插件 | 5 个 DLL |
| 编译验证 | `LastDream.ChingMu`：0 error，0 warning；设备信号验证待执行 |

## 安装

### Package Manager

在 Unity 中打开：

```text
Window > Package Manager > + > Add package from git URL
```

输入固定版本地址：

```text
https://github.com/1058797782/ChingMuUnitySDKs_LDVersion.git?path=/Packages/com.lastdream.chingmu#v3.0.1-ld.1
```

### manifest.json

也可以在使用方工程的 `Packages/manifest.json` 中加入：

```json
"com.lastdream.chingmu": "https://github.com/1058797782/ChingMuUnitySDKs_LDVersion.git?path=/Packages/com.lastdream.chingmu#v3.0.1-ld.1"
```

固定标签适合正式工程。需要跟踪尚未发布的改动时，可以暂时省略末尾的版本标签并使用默认分支。

## 快速接入

1. 在场景中新建一个空物体。
2. 添加 `CMPluginThreadManager`。
3. 选择协议类型：
   - `Vrpn`
   - `LiveStream`
4. 填写青瞳服务端地址和端口。示例地址格式：

   ```text
   MCAvatar@127.0.0.1
   MCServer@127.0.0.1
   ```

5. 根据用途添加组件：
   - `BodyTracker`：刚体位置与旋转。
   - `HumanTracker`：非重定向人体骨骼。
   - `HumanRetargetForVrpn`：VRPN 人物重定向。
   - `HumanRetargetForLiveStream`：LiveStream 人物重定向。
6. 确认对应青瞳服务端已启动后再进入 Play Mode。

端口、服务端模式和角色编号应以实际青瞳服务端配置为准。原始操作手册保存在：

```text
Packages/com.lastdream.chingmu/Documentation~/Official/CMManualforUnity.pdf
```

## 配置文件

默认可以直接使用 Inspector 中的配置，不要求向 `Assets` 写入文件。

如果启用 `isUsingConfig`，可从 Package Manager 的 Samples 导入 **Configuration Template**，然后将 `Config.json` 放到使用方工程的：

```text
Assets/StreamingAssets/Config.json
```

Unity Package Manager 不会把包内文件直接安装成使用方工程的 StreamingAssets，因此这一步保持为显式选择。

当前维护版会在启动 native 线程与创建连接之前读取配置，并兼容 `ServerIP` / `serverIP` 以及旧模板的 `bodiesID` 字段。配置模板使用统一的 `Bodies`、`IMUBodies` 和 `Humans` 列表。

## 可选 Samples

包提供两个可选 Sample：

| Sample | 内容 |
|---|---|
| Official Content | 官方 unitypackage 中的示例场景、预制体、字体、模型、材质和纹理 |
| Configuration Template | 官方 `Config.json` 模板 |

只有主动点击 Import 时，Sample 才会复制到使用方工程的 `Assets/Samples`。只安装 Runtime 不会导入这些内容。

## 包目录

```text
Packages/com.lastdream.chingmu/
├─ package.json
├─ README.md
├─ CHANGELOG.md
├─ Third Party Notices.md
├─ Runtime/
│  ├─ LastDream.ChingMu.asmdef
│  └─ ChingMu/
│     ├─ Core/
│     └─ Plugins/
├─ Samples~/
│  ├─ OfficialContent/
│  └─ Configuration/
└─ Documentation~/
   └─ Official/
```

## 文件来源与维护边界

| 路径 | 来源 | 说明 |
|---|---|---|
| `Runtime/ChingMu/Core` | 青瞳官方 v3.0.1 | Runtime C#；仅包含下方列出的兼容修正 |
| `Runtime/ChingMu/Plugins` | 青瞳官方 v3.0.1 | 原始 native 插件 |
| `Samples~/OfficialContent` | 青瞳官方 v3.0.1 发布内容 | 可选示例与美术内容 |
| `Samples~/Configuration/Config.json` | 青瞳官方 v3.0.1 | 配置模板 |
| `Documentation~/Official` | 青瞳官方 v3.0.1 | 原始手册 |
| 包清单、asmdef、README、CHANGELOG | LD 包装层 | Package Manager 集成和维护说明 |

仓库根部的 `Assets`、`ProjectSettings` 和普通 Unity 模板资源用于维护与编译这个包，不会随 Git 子目录包安装到其他工程。

## 相对官方 v3.0.1 的兼容修正

- 从 `HumanTracker.cs` 删除未使用的 `Mono.Cecil` 引用，修复 Runtime 程序集的 `CS0246`。
- 从 `VRPNSetup.cs` 删除未使用的 Visual Scripting 引用，避免无必要的包依赖。
- 增加 `LastDream.ChingMu.asmdef`，建立明确的 Runtime 程序集边界。
- LiveStream 按目标 ID 直接读取原生内存，停止逐调用反序列化完整固定容量帧。
- VRPN 人体、重定向和设备融合缓冲区改为复用，消除逐次采样数组分配。
- native 回调使用 AOT 安全的静态入口、安全令牌和主线程队列，并补齐注销与清理。
- 修复人物 ID 被误当数组下标、配置读取时序、回调委托丢失、跨线程列表竞争、资源缺失崩溃等问题。
- 诊断扫描与高频日志默认关闭；LiveStream 可视化会在官方缺失预制体时使用程序化后备。
- 修复 native 插件平台设置，并禁用官方错误标注为 x86 的同一份 x86_64 DLL。
- 修复两个引用缺失 UVRPN 外部组件的可选场景，恢复包自身的 `BodyTracker`。
- 保留公开组件类名、原始序列化字段、native 入口和资源 GUID。

## 已知限制

- native DLL 面向 Windows x86_64；其他平台和 32 位 Windows 不受支持。
- 官方 `XR Origin.prefab` 引用了 v3.0.1 unitypackage 未包含的 XR Interaction Toolkit Sample 资源。
- `LiveStream.dll` 依赖 Visual C++ 2010 运行库，目标机器缺失时会在连接前加载失败。
- 目前没有青瞳信号输入，连接、真实姿态、回调顺序和长时间 native 内存仍需硬件回归。
- `Samples~/OfficialContent/Rescours` 的原始拼写为官方发布结构的一部分，为避免破坏引用而保留。

## 分支与版本

| 分支或标签 | 用途 |
|---|---|
| `main` | 当前 Package Manager 维护分支 |
| `v3.0.1-ld.1` | 首个可安装的固定版本 |
| `legacy/unitypackage-import` | 未修复、未迁移的 v3.0.1 Unity 工程导入基线 |
| `legacy/original` | fork 前的官方仓库历史 |

升级 Runtime 或重新导入官方包时，应先核对 `.meta`、native 插件平台设置和序列化字段，避免破坏现有场景与预制体引用。

## 上游与授权提示

- 官方仓库：<https://github.com/ChingMuVisionTech/ChingMuUnitySDKs>
- 本仓库：<https://github.com/1058797782/ChingMuUnitySDKs_LDVersion>

检查上游仓库和 v3.0.1 发布内容时未发现明确的 LICENSE 文件或 SPDX 声明。本仓库不对青瞳官方内容授予额外权利；使用和再分发前应向原提供方确认许可范围。第三方内容说明见：

```text
Packages/com.lastdream.chingmu/Third Party Notices.md
```
