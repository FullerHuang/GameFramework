# GameFramework 架构设计

## 概述

基于 **Composition Root → Service → UseCase → MVP** 的分层架构，通过 VContainer 依赖注入和自实现 ReactiveProperty 实现响应式数据绑定。Framework（AOT）与 HotUpdate（HybridCLR Interpreter）分离，支持热更新。

## 分层架构

```
HotUpdate.dll (HybridCLR Interpreter)
├── SceneCompositionRoot    场景组合根（VContainer LifetimeScope per Scene）
├── MVP Layer
│   ├── View                MonoBehaviour，实现 IView 接口
│   ├── Presenter           注入 IView + IUseCase + Model，生命周期绑定 View
│   └── Model               ReactiveProperty<T> + 业务逻辑
├── UseCase Layer           跨 Service 业务流程，不依赖 View
└── Business Service Layer  业务服务，管理 Model 生命周期

Assembly-CSharp (AOT)
├── GlobalCompositionRoot   DI 容器构建入口 + IHotUpdateInstaller 发现
├── Global Service Layer    InputService, AudioService, SceneService 等
└── Framework Core          ReactiveProperty, MVP 基类, 接口定义
```

## 数据流

```
正向: View(用户输入) → Presenter → UseCase → Service(s) → Model(更新)
反向: Model(OnValueChanged) → Presenter(订阅回调) → View(更新UI)
```

## 核心接口定义（Framework 层）

```csharp
// 响应式属性
public class ReactiveProperty<T> {
    T _value;
    event Action<T> OnValueChanged;
    public T Value { get; set; }  // set 时触发 OnValueChanged
    public void Subscribe(Action<T> callback);
    public void Unsubscribe(Action<T> callback);
}

// 标记接口
public interface IView { }
public interface IService { }

// Presenter 基类
public abstract class Presenter<TView> where TView : IView {
    protected TView View;
    public virtual void Initialize() { }
    public virtual void Dispose() { }
}

// Model 基类
public abstract class Model : IDisposable { }

// UseCase 基类
public abstract class UseCase { }

// 热更新安装器接口（Framework 定义，HotUpdate 实现）
public interface IHotUpdateInstaller {
    void Install(IContainerBuilder builder);
}
```

## 关键设计决策

### 1. Service 管理 Model（方案 B）

- Service 负责 Model 的创建、缓存、持久化和释放
- Presenter 通过 DI 直接注入 Model 并订阅其响应式属性
- Service 不做 Model 属性的代理

### 2. 面板级 MVP 支持一组 View

- Presenter 可注入多个 IView 接口，每个接口对应一个子组件
- 兼容注入单个 IView 的简单场景

### 3. UseCase 判断标准

- Presenter 依赖 ≥2 个 Service 且有跨 Service 业务判断 → 提取 UseCase
- 仅依赖 1 个 Service → Presenter 直接依赖 Service

### 4. View 注入 Presenter 的方式

View 在 Awake 中创建 VContainer 子 Scope，注册自身后解析 Presenter：

```csharp
public class PlayerView : MonoBehaviour, IPlayerView {
    PlayerPresenter _presenter;

    void Awake() {
        var parentScope = GetComponentInParent<LifetimeScope>();
        var subScope = parentScope.CreateChild(b =>
            b.RegisterInstance<IPlayerView>(this));
        _presenter = subScope.Resolve<PlayerPresenter>();
    }

    void OnDestroy() {
        _presenter.Dispose();
        subScope.Dispose();
    }
}
```

### 5. HybridCLR 集成

- Framework 定义 `IHotUpdateInstaller`，HotUpdate 实现
- GlobalCompositionRoot 反射发现并调用 Installer
- HotUpdate 中优先使用非泛型注册 API（`Register(Type, Type)`）减少补充元数据压力
- View 的 Prefab 通过 AssetBundle 下发

## 程序集划分

| 程序集 | 编译方式 | 内容 |
|--------|----------|------|
| Assembly-CSharp | AOT | Framework/Core, Framework/Interfaces, Framework/Services |
| HotUpdate.dll | Interpreter | Game/Services, Game/UseCases, Game/Models, Game/Presenters, Game/Views |

## 目录结构

```
Assets/
├── Framework/Core/          AppBuilder, GlobalCompositionRoot, ReactiveProperty
├── Framework/Interfaces/    IView, IPresenter, IService, IHotUpdateInstaller, 全局服务接口
├── Framework/Services/      全局服务实现
├── HotUpdate/               GameInstaller, Scenes/, Services/, UseCases/, Models/, Presenters/, Views/
└── HybridCLR/               HybridCLR 配置与补充元数据
```

## 已知风险与缓解

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| View Awake 中有 DI 样板代码 | 中 | VContainer LifetimeScope 模式已成熟，样板代码量小且一致 |
| Model 订阅泄漏（忘记取消订阅） | 中 | Presenter 基类提供 DisposeBag，自动管理订阅生命周期 |
| HybridCLR 泛型补充元数据 | 中 | 提供非泛型注册 API，前期减少泛型路径 |

## 技术选型

| 组件 | 选择 | 说明 |
|------|------|------|
| DI 框架 | VContainer | 成熟 Unity DI，原生支持场景生命周期 |
| 响应式属性 | 自实现 ReactiveProperty<T> | ~100 行，无外部依赖 |
| 热更新 | HybridCLR | IL 解释器方案，同一 CLR 运行 |
