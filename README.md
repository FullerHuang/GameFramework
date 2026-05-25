# GameFramework 技术文档

> Unity 6000.0.32f1 · URP 17.0.3 · VContainer 1.16.8 · HybridCLR-ready

## 1. 项目概览

GameFramework 是一个基于 **Composition Root → Service → UseCase → MVP** 分层架构的 Unity 游戏框架。核心设计目标：

- **依赖注入**（VContainer）管理所有对象的生命周期，除全局组合根外无单例
- **响应式数据绑定**（自实现 ReactiveProperty）驱动 UI 更新
- **程序集分离**（Framework AOT + HotUpdate Interpreter），支持 HybridCLR 热更新
- **可测试性**：Presenter/UseCase/Service 通过接口注入，纯 C# 单元测试友好

## 2. 技术栈

| 组件 | 选型 | 版本 | 说明 |
|------|------|------|------|
| 引擎 | Unity 6 | 6000.0.32f1 | URP 渲染管线 |
| DI 框架 | VContainer | 1.16.8 | Git URL 安装，原生支持 LifetimeScope |
| 响应式属性 | 自实现 ReactiveProperty\<T\> | — | ~60 行，零外部依赖 |
| 输入系统 | Unity InputSystem | 1.11.2 | InputService 封装 |
| 热更新 | HybridCLR | 待接入 | 程序集分离已就绪 |
| MCP | MCPForUnity | 9.6.8 | Unity Editor 自动化操作 |

## 3. 架构分层

```
┌──────────────────────────────────────────────────────────────┐
│  HotUpdate.dll (HybridCLR Interpreter / 热更新程序集)          │
│                                                              │
│  SceneCompositionRoot  →  Presenter  →  UseCase              │
│  (场景 LifetimeScope)       ↕              ↓                  │
│                            View          Service(s)          │
│                     (MonoBehaviour)        ↓                 │
│                                          Model               │
│                                    (ReactiveProperty)         │
├──────────────────────────────────────────────────────────────┤
│  Assembly-CSharp (AOT / 随包编译)                             │
│                                                              │
│  GlobalCompositionRoot  →  Global Services                   │
│  (根 LifetimeScope)        (Input, Audio, Scene...)           │
│                                                              │
│  Framework Core: ReactiveProperty, MVP基类, 接口定义          │
└──────────────────────────────────────────────────────────────┘
```

**数据流：**

```
正向: View(用户输入) → Presenter → [UseCase] → Service → Model(更新)
反向: Model(ReactiveProperty.OnValueChanged) → Presenter(订阅) → View(更新UI)
```

**UseCase 判别标准：** Presenter 依赖 ≥2 个 Service 且有跨 Service 的业务逻辑时提取 UseCase。仅 1 个 Service 时 Presenter 直接依赖 Service。

## 4. 项目目录结构

```
Assets/
├── Framework/                          # AOT 核心框架（不参与热更新）
│   ├── Framework.asmdef                # 程序集: 引用 VContainer, Unity.InputSystem
│   ├── Core/
│   │   ├── ReactiveProperty.cs         # 响应式属性 (~60行)
│   │   ├── DisposeBag.cs               # 订阅生命周期收集器
│   │   ├── AppBuilder.cs               # DI 容器构建器
│   │   ├── GlobalCompositionRoot.cs    # 全局组合根 (LifetimeScope)
│   │   ├── Model.cs                    # Model 基类
│   │   ├── Presenter.cs                # Presenter<TView> 基类
│   │   └── UseCase.cs                  # UseCase 基类
│   ├── Interfaces/
│   │   ├── IView.cs                    # View 标记接口
│   │   ├── IService.cs                 # Service 标记接口
│   │   ├── IHotUpdateInstaller.cs      # 热更新安装器接口
│   │   └── Services/
│   │       ├── IInputService.cs        # 输入服务接口
│   │       ├── IAudioService.cs        # 音频服务接口
│   │       └── ISceneService.cs        # 场景加载服务接口
│   └── Services/
│       ├── InputService.cs             # InputSystem 封装
│       ├── AudioService.cs             # 音频管理
│       └── SceneService.cs             # 场景异步加载
│
├── HotUpdate/                          # 热更新程序集（业务代码）
│   ├── HotUpdate.asmdef                # 程序集: 引用 Framework, VContainer
│   ├── GameInstaller.cs                # IHotUpdateInstaller 实现
│   ├── Models/
│   │   └── PlayerModel.cs              # 示例: 玩家数据模型
│   ├── Services/
│   │   └── PlayerService.cs            # 玩家服务 (工厂模式，字典管理多玩家)
│   ├── Presenters/
│   │   └── PlayerPresenter.cs          # 玩家 Presenter (通过 playerId 获取 Model)
│   ├── Views/
│   │   ├── IPlayerView.cs              # View 接口契约
│   │   └── PlayerView.cs               # MonoBehaviour View (playerId 标识)
│   └── Scenes/
│       └── GameScene/
│           └── GameSceneRoot.cs        # 场景级组合根 (LifetimeScope)
│
├── Scenes/
│   ├── Bootstrap.unity                 # 启动场景 (index 0), GlobalRoot
│   └── GameTest.unity                  # 测试场景 (index 1), SceneRoot+UI
├── HybridCLR/
│   └── README.txt                      # HybridCLR 接入指南
└── 开发需求.txt                         # 原始需求文档
```

## 5. Framework 核心 API

### 5.1 ReactiveProperty\<T\>

```csharp
// 创建
var gold = new ReactiveProperty<int>(0);
var name = new ReactiveProperty<string>("Player");

// 读写
gold.Value = 100;                         // 设置值，若不同则触发 OnValueChanged
Debug.Log(gold.Value);                    // 读取值

// 订阅（返回 IDisposable，交 DisposeBag 管理）
IDisposable sub = gold.Subscribe(v => Debug.Log($"Gold changed: {v}"));
IDisposable sub2 = gold.SubscribeAndRefresh(v => ui.UpdateGold(v)); // 立即回调一次当前值

// 取消订阅
gold.Unsubscribe(callback);
```

**实现细节：** 值比较通过 `Equals()`，相同值不会触发通知。订阅者通过 `IDisposable` 管理释放，`DisposeBag` 统一收集。

### 5.2 DisposeBag

```csharp
var bag = new DisposeBag();
bag.Add(reactiveProperty.Subscribe(OnChanged));  // 添加 IDisposable
bag.Add(() => someEvent -= handler);              // 添加 Action
bag.Dispose();  // 逆序释放所有注册的资源
```

**用途：** Presenter 基类内置 `_disposeBag`，`AddDisposable()` 方法统一管理订阅生命周期。View.OnDestroy 时调用 `Presenter.Dispose()` → `DisposeBag.Dispose()` 释放所有订阅。

### 5.3 Presenter\<TView\>

```csharp
public abstract class Presenter<TView> : IDisposable where TView : IView
{
    protected readonly TView View;

    // 子类重写，在此绑定 Model 订阅和 View 事件
    public virtual void Initialize() { }

    // 注册可释放资源（订阅、事件注销等）
    protected void AddDisposable(IDisposable disposable);
    protected void AddDisposable(Action onDispose);

    // View.OnDestroy 时调用，释放所有订阅
    public virtual void Dispose();
}

// 多玩家示例：通过 playerId 从 Service 获取 Model
public class PlayerPresenter : Presenter<IPlayerView>
{
    public PlayerPresenter(IPlayerView view, string playerId, IPlayerService service)
        : base(view) { ... }

    public override void Initialize()
    {
        _model = _service.GetOrCreate(_playerId);  // 工厂模式获取 Model
        AddDisposable(_model.Gold.SubscribeAndRefresh(v => View.UpdateGold(v)));
        View.OnLevelUpClicked += HandleLevelUp;
    }
}
```

### 5.4 Model

```csharp
public abstract class Model : IDisposable
{
    protected readonly DisposeBag DisposeBag = new();
    public virtual void Dispose() { DisposeBag.Dispose(); }
}
```

### 5.5 AppBuilder + GlobalCompositionRoot

```csharp
// AppBuilder: 链式注册 DI 服务
var appBuilder = new AppBuilder()
    .Register(b => b.Register<IInputService, InputService>(Lifetime.Singleton))
    .RegisterHotUpdate();  // 反射发现 HotUpdate.GameInstaller

// GlobalCompositionRoot: 根 LifetimeScope (挂载在 Bootstrap 场景)
// Awake 时自动构建容器，DontDestroyOnLoad
```

### 5.6 IHotUpdateInstaller

```csharp
// Framework 定义接口
public interface IHotUpdateInstaller
{
    void Install(IContainerBuilder builder);
}

// HotUpdate 实现
public class GameInstaller : IHotUpdateInstaller
{
    public void Install(IContainerBuilder builder)
    {
        builder.Register<IPlayerService, PlayerService>(Lifetime.Singleton);
    }
}
```

### 5.7 全局服务接口

| 接口 | 服务 | 生命周期 | 说明 |
|------|------|----------|------|
| `IInputService` | `InputService` | Singleton | 基于 InputSystem，封装 Move/Jump/Attack/Interact |
| `IAudioService` | `AudioService` | Singleton | SFX/BGM 播放，音量控制 |
| `ISceneService` | `SceneService` | Singleton | 场景异步加载，当前场景名查询 |

## 6. 关键设计模式

### 6.1 View 注入 Presenter（子 Scope 模式 + playerId）

```csharp
// PlayerView.cs
[SerializeField] string _playerId = "Player1";  // 标识显示哪个玩家

void Awake()
{
    // 1. 找到父 LifetimeScope（GameSceneRoot）
    var parentScope = GetComponentInParent<LifetimeScope>();

    // 2. 创建子 Scope，注册自己和 playerId
    _subScope = parentScope.CreateChild(builder => {
        builder.RegisterInstance<IPlayerView>(this);
        builder.RegisterInstance(_playerId);
    });

    // 3. 解析 Presenter（playerId 从子 Scope 获取）
    _presenter = _subScope.Container.Resolve<PlayerPresenter>();
    _presenter.Initialize();
}

void OnDestroy()
{
    _presenter?.Dispose();    // 释放所有订阅
    _subScope?.Dispose();     // 销毁子 Scope
}
```

**关键约束：** View 所在的 GameObject 必须是 GameSceneRoot（LifetimeScope）的子孙节点（Transform 层级），否则 `GetComponentInParent<LifetimeScope>()` 找不到。

### 6.2 Service 管理 Model 生命周期（工厂模式）

- **Service 负责：** Model 的创建（`CreatePlayer(id, name, gold)`）、缓存（字典存储）、查找（`GetPlayer(id)`）、持久化（`SaveAllPlayers()`）
- **Presenter 负责：** 通过 `playerId` 从 Service 获取 Model，订阅响应式属性，调用 View 更新 UI
- **多玩家模式：** 每个 `PlayerView` 指定 `_playerId`，同一玩家的多个 View 共享同一个 `PlayerModel`

### 6.3 跨场景 Scope 父子关系

```csharp
// GameSceneRoot.cs
protected override LifetimeScope FindParent()
{
    // 在 Bootstrap 场景的 DontDestroyOnLoad 对象中找到根 Scope
    return LifetimeScope.Find<GlobalCompositionRoot>();
}
```

### 6.4 工厂模式与多实例

框架支持通过 Service 工厂模式管理多个同类型 Model。以玩家为例：

```csharp
// Service 作为工厂 + 缓存
IPlayerService service = ...;
service.CreatePlayer("P1", "Hero", initialGold: 100);  // 创建
service.CreatePlayer("P2", "Warrior", initialGold: 200);

PlayerModel p1 = service.GetPlayer("P1");   // 获取已有
PlayerModel p2 = service.GetOrCreate("P3"); // 获取或自动创建

service.SaveAllPlayers();  // 批量持久化
```

**View 端指定实例：** 每个 `PlayerView` 通过 `_playerId` 字段标识自己要显示的玩家。

**同一玩家多 View 共享：** 两个 View 指定同一个 `_playerId`（如 `"P1"`），底层 Presenter 会共享同一 `PlayerModel` 实例。

### 6.5 添加新 MVP 模块步骤

以添加 `Shop` 模块为例：

1. **定义 View 接口** — `Assets/HotUpdate/Views/IShopView.cs`
2. **创建 Model** — `Assets/HotUpdate/Models/ShopModel.cs`，继承 `Model`
3. **创建 Service** — `Assets/HotUpdate/Services/ShopService.cs`，实现 `IShopService : IService`（如需多实例，用工厂模式）
4. **创建 UseCase**（可选） — `Assets/HotUpdate/UseCases/PurchaseItemUseCase.cs`
5. **创建 Presenter** — `Assets/HotUpdate/Presenters/ShopPresenter.cs`，继承 `Presenter<IShopView>`
6. **创建 View** — `Assets/HotUpdate/Views/ShopView.cs`，MonoBehaviour 实现 `IShopView`
7. **注册到 DI** — `GameInstaller.cs` 注册 Service，`GameSceneRoot.cs` 注册 Presenter

## 7. 场景说明

| 场景 | Build Index | 功能 |
|------|-------------|------|
| `Bootstrap.unity` | 0 | 全局入口，挂载 GlobalCompositionRoot，DontDestroyOnLoad |
| `GameTest.unity` | 1 | 多玩家 MVP 测试场景，PlayerView (P1) + PlayerView (P2)，挂载 GameSceneRoot |
| `SampleScene.unity` | — | Unity 默认模板场景（未配置到 Build） |

**关键：** Bootstrap 必须是 Build Settings 的第一个场景。GlobalCompositionRoot 在此初始化 DI 容器。

## 8. 程序集依赖

```
Framework.asmdef
  references: VContainer, Unity.InputSystem
  namespace: GameFramework

HotUpdate.asmdef
  references: Framework, VContainer
  namespace: HotUpdate
```

HybridCLR 接入时，`HotUpdate` 程序集将编译为独立 DLL 并通过 IL 解释器加载，`Framework` 保持 AOT 编译。

## 9. 开发工作流

### 添加全局服务
1. 在 `Framework/Interfaces/Services/` 定义接口，继承 `IService`
2. 在 `Framework/Services/` 实现服务
3. 在 `GlobalCompositionRoot.Configure()` 注册

### 添加业务模块（HotUpdate）
1. 按 6.4 节流程创建 MVP 文件
2. 在 `GameInstaller.Install()` 注册业务服务
3. 在 `GameSceneRoot.Configure()` 注册场景级 Presenter/Model
4. 在 Unity 场景中创建 UI 并挂载 View 组件
5. 通过 MCP 或手动连线 SerializeField

### 编译检查
- 每次修改 `.cs` 文件后等待 Unity 编译完毕
- 检查 Console (Window > General > Console) 是否有编译错误
- 使用 MCP: `read_console(types=["error"])`

## 10. 已知注意事项

1. **View 必须在 SceneRoot 的子层级中**：`GetComponentInParent<LifetimeScope>()` 沿 Transform 树向上查找，Canvas 必须是 GameSceneRoot 的子节点

2. **Presenter 订阅必须通过 AddDisposable**：直接订阅 Model 属性而不用 `AddDisposable()` 会导致内存泄漏

3. **Framework.asmdef 显式引用**：使用 `Unity.InputSystem` 等 Package 程序集时，必须在 asmdef 的 `references` 中显式列出

4. **VContainer 解析 API**：`LifetimeScope` 的 `Resolve()` 需要通过 `.Container` 属性访问（`scope.Container.Resolve<T>()`），而非直接在 scope 上调用

5. **HybridCLR 泛型元数据**：未来接入 HybridCLR 时，HotUpdate 中尽量使用非泛型注册 API，减少补充元数据需求

## 11. 后续待完成

- [x] 多玩家工厂模式（PlayerService 字典管理）
- [ ] HybridCLR 正式接入（程序集分离已就绪）
- [ ] 更多 UseCase 示例（购买、升级等跨 Service 流程）
- [ ] 输入系统配置文件 (.inputactions) 迁移到 Framework 目录
- [ ] 单元测试（Presenter/Service/UseCase 纯 C# 测试）
- [ ] AssetBundle / Addressable 资源管理集成

## 12. 相关文档

- [架构设计文档](docs/superpowers/specs/2026-05-24-gameframework-architecture-design.md)
- [实现计划](docs/superpowers/plans/2026-05-24-gameframework-core-plan.md)
- [HybridCLR 接入指南](Assets/HybridCLR/README.txt)
- [原始需求](Assets/开发需求.txt)
