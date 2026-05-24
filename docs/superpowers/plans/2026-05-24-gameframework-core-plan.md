# GameFramework 框架核心实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建 GameFramework 核心框架：ReactiveProperty + VContainer DI + GlobalCompositionRoot + HotUpdate 基础设施 + 示例 MVP 流程验证

**Architecture:** Framework (AOT) 定义接口与基类，HotUpdate (Interpreter) 实现业务代码。VContainer 管理依赖注入，View 通过子 Scope 模式解析 Presenter，Presenter 通过 DisposeBag 管理订阅生命周期。

**Tech Stack:** Unity 6 (URP), VContainer 1.16.8, HybridCLR-ready 程序集分离

---

## 文件结构总览

```
Assets/
├── Framework/
│   ├── Framework.asmdef                    # Task 2
│   ├── Core/
│   │   ├── ReactiveProperty.cs             # Task 3
│   │   ├── DisposeBag.cs                   # Task 5
│   │   ├── AppBuilder.cs                   # Task 6
│   │   └── GlobalCompositionRoot.cs        # Task 6
│   ├── Interfaces/
│   │   ├── IView.cs                        # Task 4
│   │   ├── IService.cs                     # Task 4
│   │   ├── IHotUpdateInstaller.cs          # Task 4
│   │   └── Services/
│   │       ├── IInputService.cs            # Task 7
│   │       ├── IAudioService.cs            # Task 7
│   │       └── ISceneService.cs            # Task 7
│   └── Services/
│       ├── InputService.cs                 # Task 7
│       ├── AudioService.cs                 # Task 7
│       └── SceneService.cs                 # Task 7
├── HotUpdate/
│   ├── HotUpdate.asmdef                    # Task 2
│   ├── GameInstaller.cs                    # Task 8
│   ├── Scenes/
│   │   └── GameScene/
│   │       └── GameSceneRoot.cs            # Task 8
│   ├── Models/
│   │   └── PlayerModel.cs                  # Task 9
│   ├── Services/
│   │   └── PlayerService.cs               # Task 10
│   ├── Views/
│   │   └── PlayerView.cs                   # Task 11
│   └── Presenters/
│       └── PlayerPresenter.cs              # Task 12
├── Scenes/
│   └── Bootstrap.unity                     # Task 6 (手动创建场景)
└── HybridCLR/                              # Task 13 (配置占位)
```

---

### Task 1: 安装 VContainer

**Files:**
- Modify: `Packages/manifest.json`

- [ ] **Step 1: 添加 VContainer git 依赖**

在 `Packages/manifest.json` 的 `dependencies` 中添加:

```json
"jp.hadashikick.vcontainer": "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.16.8"
```

- [ ] **Step 2: 等待 Unity 解析包**

打开 Unity Editor，等待 Package Manager 自动解析和导入 VContainer。Console 中应无报错。

- [ ] **Step 3: 验证 VContainer 可用**

在 Unity Editor 中，通过 `Window > Package Manager` 确认 `VContainer` 已安装，版本显示为 1.16.8。

- [ ] **Step 4: 提交**

```bash
git add Packages/manifest.json Packages/packages-lock.json
git commit -m "chore: add VContainer 1.16.8 dependency"
```

---

### Task 2: 创建程序集定义

**Files:**
- Create: `Assets/Framework/Framework.asmdef`
- Create: `Assets/Framework/Framework.asmdef.meta` (Unity auto)
- Create: `Assets/HotUpdate/HotUpdate.asmdef`
- Create: `Assets/HotUpdate/HotUpdate.asmdef.meta` (Unity auto)

- [ ] **Step 1: 创建 Framework.asmdef**

```json
{
    "name": "Framework",
    "rootNamespace": "GameFramework",
    "references": [
        "VContainer"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: 创建 HotUpdate.asmdef**

```json
{
    "name": "HotUpdate",
    "rootNamespace": "HotUpdate",
    "references": [
        "Framework",
        "VContainer"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: 等待 Unity 编译**

回到 Unity Editor，等待程序集重新编译。Console 中应无报错。确认 `Assets/Framework/` 和 `Assets/HotUpdate/` 下的脚本现在分别属于对应程序集。

- [ ] **Step 4: 验证程序集引用**

在 Unity Editor 中，打开 `Window > Analysis > Assembly Validation` 或直接检查 Console 中无 CS0234（缺少程序集引用）错误。

- [ ] **Step 5: 提交**

```bash
git add Assets/Framework/Framework.asmdef Assets/Framework/Framework.asmdef.meta
git add Assets/HotUpdate/HotUpdate.asmdef Assets/HotUpdate/HotUpdate.asmdef.meta
git commit -m "chore: add Framework and HotUpdate assembly definitions"
```

---

### Task 3: 实现 ReactiveProperty<T>

**Files:**
- Create: `Assets/Framework/Core/DisposeBag.cs`
- Create: `Assets/Framework/Core/ReactiveProperty.cs`
- Create: `Assets/Framework/Core/ReactiveProperty.cs.meta` (Unity auto)

说明: DisposeBag 和 ReactiveProperty 耦合在一起（ReactiveProperty 的 Subscribe 返回 IDisposable 交给 DisposeBag 管理），所以合并在一个 Task 中。

- [ ] **Step 1: 创建 DisposeBag.cs**

```csharp
using System;
using System.Collections.Generic;

namespace GameFramework
{
    /// <summary>
    /// 收集 IDisposable 并在自身 Dispose 时统一释放，用于 Presenter 管理订阅生命周期。
    /// </summary>
    public sealed class DisposeBag : IDisposable
    {
        readonly List<IDisposable> _disposables = new();
        bool _disposed;

        public void Add(IDisposable disposable)
        {
            if (!_disposed) _disposables.Add(disposable);
        }

        public void Add(Action onDispose)
        {
            if (!_disposed) _disposables.Add(new ActionDisposable(onDispose));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            for (int i = _disposables.Count - 1; i >= 0; i--)
                _disposables[i]?.Dispose();
            _disposables.Clear();
        }

        sealed class ActionDisposable : IDisposable
        {
            Action _action;
            public ActionDisposable(Action action) => _action = action;
            public void Dispose() => _action?.Invoke();
        }
    }
}
```

- [ ] **Step 2: 创建 ReactiveProperty.cs**

```csharp
using System;

namespace GameFramework
{
    public sealed class ReactiveProperty<T>
    {
        T _value;
        event Action<T> OnValueChanged;

        public ReactiveProperty() => _value = default;
        public ReactiveProperty(T initialValue) => _value = initialValue;

        public T Value
        {
            get => _value;
            set
            {
                if (Equals(_value, value)) return;
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }

        public IDisposable Subscribe(Action<T> callback)
        {
            OnValueChanged += callback;
            return new Subscription(this, callback);
        }

        /// <summary>
        /// 同时触发一次当前值回调（用于初始化 UI 状态）
        /// </summary>
        public IDisposable SubscribeAndRefresh(Action<T> callback)
        {
            callback(_value);
            return Subscribe(callback);
        }

        public void Unsubscribe(Action<T> callback)
        {
            OnValueChanged -= callback;
        }

        sealed class Subscription : IDisposable
        {
            readonly ReactiveProperty<T> _owner;
            readonly Action<T> _callback;

            public Subscription(ReactiveProperty<T> owner, Action<T> callback)
            {
                _owner = owner;
                _callback = callback;
            }

            public void Dispose()
            {
                _owner.Unsubscribe(_callback);
            }
        }
    }
}
```

- [ ] **Step 3: 等待 Unity 编译通过**

Console 应无报错。

- [ ] **Step 4: 提交**

```bash
git add Assets/Framework/Core/DisposeBag.cs Assets/Framework/Core/DisposeBag.cs.meta
git add Assets/Framework/Core/ReactiveProperty.cs Assets/Framework/Core/ReactiveProperty.cs.meta
git commit -m "feat: add ReactiveProperty<T> and DisposeBag"
```

---

### Task 4: 创建核心标记接口

**Files:**
- Create: `Assets/Framework/Interfaces/IView.cs`
- Create: `Assets/Framework/Interfaces/IService.cs`
- Create: `Assets/Framework/Interfaces/IHotUpdateInstaller.cs`

- [ ] **Step 1: 创建 IView.cs**

```csharp
namespace GameFramework
{
    /// <summary>
    /// View 层标记接口。MonoBehaviour 实现的 View 需要继承此接口。
    /// </summary>
    public interface IView { }
}
```

- [ ] **Step 2: 创建 IService.cs**

```csharp
namespace GameFramework
{
    /// <summary>
    /// Service 层标记接口。所有服务层接口需继承此接口。
    /// </summary>
    public interface IService { }
}
```

- [ ] **Step 3: 创建 IHotUpdateInstaller.cs**

```csharp
using VContainer;

namespace GameFramework
{
    /// <summary>
    /// 热更新安装器接口。Framework 定义此接口，HotUpdate 程序集中的类实现它。
    /// GlobalCompositionRoot 通过反射发现实现类并调用 Install 完成热更代码的 DI 注册。
    /// </summary>
    public interface IHotUpdateInstaller
    {
        void Install(IContainerBuilder builder);
    }
}
```

- [ ] **Step 4: 等待 Unity 编译通过**

- [ ] **Step 5: 提交**

```bash
git add Assets/Framework/Interfaces/IView.cs Assets/Framework/Interfaces/IView.cs.meta
git add Assets/Framework/Interfaces/IService.cs Assets/Framework/Interfaces/IService.cs.meta
git add Assets/Framework/Interfaces/IHotUpdateInstaller.cs Assets/Framework/Interfaces/IHotUpdateInstaller.cs.meta
git commit -m "feat: add core interfaces (IView, IService, IHotUpdateInstaller)"
```

---

### Task 5: 创建 MVP 基类

**Files:**
- Create: `Assets/Framework/Core/Presenter.cs`
- Create: `Assets/Framework/Core/Model.cs`
- Create: `Assets/Framework/Core/UseCase.cs`

- [ ] **Step 1: 创建 Model.cs**

```csharp
using System;

namespace GameFramework
{
    /// <summary>
    /// Model 基类。管理响应式数据和自身业务逻辑，通过 Service 管理生命周期。
    /// </summary>
    public abstract class Model : IDisposable
    {
        protected readonly DisposeBag DisposeBag = new();

        public virtual void Dispose()
        {
            DisposeBag.Dispose();
        }
    }
}
```

- [ ] **Step 2: 创建 Presenter.cs**

```csharp
using System;

namespace GameFramework
{
    /// <summary>
    /// Presenter 基类。持有 View 接口依赖，通过 DisposeBag 管理所有订阅生命周期。
    /// 生命周期绑定 View：View.Awake → Resolve → Initialize，View.OnDestroy → Dispose。
    /// </summary>
    public abstract class Presenter<TView> : IDisposable where TView : IView
    {
        protected readonly TView View;
        readonly DisposeBag _disposeBag = new();

        protected Presenter(TView view)
        {
            View = view;
        }

        /// <summary>
        /// 在此方法中订阅 Model 属性和 View 事件。
        /// VContainer 解析完成后由 View 调用。
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// 添加一个可释放资源，在 Presenter 销毁时自动释放。
        /// 用于管理 Model 订阅（ReactiveProperty.Subscribe 返回的 IDisposable）和 View 事件注销。
        /// </summary>
        protected void AddDisposable(IDisposable disposable) => _disposeBag.Add(disposable);
        protected void AddDisposable(Action onDispose) => _disposeBag.Add(onDispose);

        public virtual void Dispose()
        {
            _disposeBag.Dispose();
        }
    }
}
```

- [ ] **Step 3: 创建 UseCase.cs**

```csharp
namespace GameFramework
{
    /// <summary>
    /// UseCase 基类。封装跨 Service 的业务流程，不依赖 View。
    /// 判断标准：Presenter 依赖 ≥2 个 Service 且有跨 Service 业务判断时提取 UseCase。
    /// </summary>
    public abstract class UseCase { }
}
```

- [ ] **Step 4: 等待 Unity 编译通过**

- [ ] **Step 5: 提交**

```bash
git add Assets/Framework/Core/Model.cs Assets/Framework/Core/Model.cs.meta
git add Assets/Framework/Core/Presenter.cs Assets/Framework/Core/Presenter.cs.meta
git add Assets/Framework/Core/UseCase.cs Assets/Framework/Core/UseCase.cs.meta
git commit -m "feat: add MVP base classes (Model, Presenter<T>, UseCase)"
```

---

### Task 6: 创建 AppBuilder + GlobalCompositionRoot

**Files:**
- Create: `Assets/Framework/Core/AppBuilder.cs`
- Create: `Assets/Framework/Core/GlobalCompositionRoot.cs`

- [ ] **Step 1: 创建 AppBuilder.cs**

```csharp
using System;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;

namespace GameFramework
{
    /// <summary>
    /// DI 容器构建器。封装 VContainer 的注册流程，支持通过 IHotUpdateInstaller 加载热更代码。
    /// </summary>
    public class AppBuilder
    {
        readonly List<Action<IContainerBuilder>> _registrations = new();

        public AppBuilder Register(Action<IContainerBuilder> registration)
        {
            _registrations.Add(registration);
            return this;
        }

        /// <summary>
        /// 通过反射加载 HotUpdate 程序集中的 IHotUpdateInstaller 实现并调用。
        /// </summary>
        public AppBuilder RegisterHotUpdate()
        {
            var type = Type.GetType("HotUpdate.GameInstaller, HotUpdate");
            if (type != null && typeof(IHotUpdateInstaller).IsAssignableFrom(type))
            {
                var installer = (IHotUpdateInstaller)Activator.CreateInstance(type);
                _registrations.Add(builder => installer.Install(builder));
            }
            return this;
        }

        public void Build(IContainerBuilder builder)
        {
            foreach (var reg in _registrations)
                reg(builder);
        }
    }
}
```

- [ ] **Step 2: 创建 GlobalCompositionRoot.cs**

```csharp
using VContainer.Unity;

namespace GameFramework
{
    /// <summary>
    /// 全局组合根。挂载到 Bootstrap 场景的 GameObject 上，设置 DontDestroyOnLoad。
    /// 作为 VContainer 根 LifetimeScope，管理所有全局服务的生命周期。
    /// 场景级组合根通过父子 LifetimeScope 关系继承此容器。
    /// </summary>
    public class GlobalCompositionRoot : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            var appBuilder = new AppBuilder();

            // 注册全局服务（后续 Task 中逐步添加）
            // appBuilder.Register(b => b.Register<IInputService, InputService>(Lifetime.Singleton));

            // 加载热更新模块
            appBuilder.RegisterHotUpdate();

            appBuilder.Build(builder);
        }
    }
}
```

- [ ] **Step 3: 在 Unity Editor 中创建 Bootstrap 场景**

在 Unity Editor 中手动操作：
1. `File > New Scene`，保存为 `Assets/Scenes/Bootstrap.unity`
2. 创建空 GameObject，命名为 `GlobalRoot`
3. 给 `GlobalRoot` 添加 `GlobalCompositionRoot` 脚本
4. 保存场景

- [ ] **Step 4: 验证场景可播放**

点击 Play，Console 中应无报错。`GlobalCompositionRoot` 应正常初始化（即使暂无服务注册）。

- [ ] **Step 5: 提交**

```bash
git add Assets/Framework/Core/AppBuilder.cs Assets/Framework/Core/AppBuilder.cs.meta
git add Assets/Framework/Core/GlobalCompositionRoot.cs Assets/Framework/Core/GlobalCompositionRoot.cs.meta
git add Assets/Scenes/Bootstrap.unity Assets/Scenes/Bootstrap.unity.meta
git commit -m "feat: add AppBuilder and GlobalCompositionRoot"
```

---

### Task 7: 全局服务接口与实现

**Files:**
- Create: `Assets/Framework/Interfaces/Services/IInputService.cs`
- Create: `Assets/Framework/Interfaces/Services/IAudioService.cs`
- Create: `Assets/Framework/Interfaces/Services/ISceneService.cs`
- Create: `Assets/Framework/Services/InputService.cs`
- Create: `Assets/Framework/Services/AudioService.cs`
- Create: `Assets/Framework/Services/SceneService.cs`
- Modify: `Assets/Framework/Core/GlobalCompositionRoot.cs` (在 `Configure` 中注册服务)

- [ ] **Step 1: 创建 IInputService.cs**

```csharp
using UnityEngine;

namespace GameFramework
{
    public interface IInputService : IService
    {
        Vector2 MoveDirection { get; }
        bool IsActionPressed(string actionName);
    }
}
```

- [ ] **Step 2: 创建 IAudioService.cs**

```csharp
namespace GameFramework
{
    public interface IAudioService : IService
    {
        void PlaySFX(string clipName);
        void PlayBGM(string clipName);
        void SetVolume(float volume);
    }
}
```

- [ ] **Step 3: 创建 ISceneService.cs**

```csharp
using System.Threading.Tasks;

namespace GameFramework
{
    public interface ISceneService : IService
    {
        Task LoadSceneAsync(string sceneName);
        string CurrentSceneName { get; }
    }
}
```

- [ ] **Step 4: 创建 InputService.cs**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFramework
{
    public class InputService : IInputService, System.IDisposable
    {
        InputAction _moveAction;
        InputAction _jumpAction;
        InputAction _attackAction;
        InputAction _interactAction;

        public Vector2 MoveDirection => _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

        public InputService()
        {
            _moveAction = new InputAction("Move", InputActionType.Value, "<Gamepad>/leftStick");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            _attackAction = new InputAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
            _interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");

            _moveAction.Enable();
            _jumpAction.Enable();
            _attackAction.Enable();
            _interactAction.Enable();
        }

        public bool IsActionPressed(string actionName)
        {
            return actionName switch
            {
                "Jump" => _jumpAction.IsPressed(),
                "Attack" => _attackAction.IsPressed(),
                "Interact" => _interactAction.IsPressed(),
                _ => false
            };
        }

        public void Dispose()
        {
            _moveAction?.Disable();
            _jumpAction?.Disable();
            _attackAction?.Disable();
            _interactAction?.Disable();
        }
    }
}
```

注意: 使用 InputAction 直接创建而非依赖 `InputSystem_Actions` 生成类，避免程序集引用问题（生成类在 Assembly-CSharp，Framework.asmdef 无法引用）。

- [ ] **Step 5: 创建 AudioService.cs**

```csharp
using UnityEngine;

namespace GameFramework
{
    public class AudioService : IAudioService
    {
        AudioSource _sfxSource;
        AudioSource _bgmSource;

        public AudioService()
        {
            var go = new GameObject("AudioService");
            Object.DontDestroyOnLoad(go);
            _sfxSource = go.AddComponent<AudioSource>();
            _bgmSource = go.AddComponent<AudioSource>();
            _bgmSource.loop = true;
        }

        public void PlaySFX(string clipName)
        {
            var clip = Resources.Load<AudioClip>($"Audio/SFX/{clipName}");
            if (clip != null) _sfxSource.PlayOneShot(clip);
        }

        public void PlayBGM(string clipName)
        {
            var clip = Resources.Load<AudioClip>($"Audio/BGM/{clipName}");
            if (clip != null && _bgmSource.clip?.name != clipName)
            {
                _bgmSource.clip = clip;
                _bgmSource.Play();
            }
        }

        public void SetVolume(float volume)
        {
            _sfxSource.volume = volume;
            _bgmSource.volume = volume;
        }
    }
}
```

- [ ] **Step 6: 创建 SceneService.cs**

```csharp
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace GameFramework
{
    public class SceneService : ISceneService
    {
        public string CurrentSceneName => SceneManager.GetActiveScene().name;

        public async Task LoadSceneAsync(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null) return;
            while (!op.isDone)
                await Task.Yield();
        }
    }
}
```

- [ ] **Step 7: 更新 GlobalCompositionRoot.cs 注册全局服务**

修改 `Configure` 方法，在 `appBuilder.Build(builder)` 之前添加注册：

```csharp
protected override void Configure(IContainerBuilder builder)
{
    var appBuilder = new AppBuilder();

    // 全局服务
    appBuilder.Register(b => b.Register<IInputService, InputService>(Lifetime.Singleton));
    appBuilder.Register(b => b.Register<IAudioService, AudioService>(Lifetime.Singleton));
    appBuilder.Register(b => b.Register<ISceneService, SceneService>(Lifetime.Singleton));

    appBuilder.RegisterHotUpdate();
    appBuilder.Build(builder);
}
```

- [ ] **Step 8: 等待 Unity 编译通过**

- [ ] **Step 9: 提交**

```bash
git add Assets/Framework/Interfaces/Services/ Assets/Framework/Services/
git add Assets/Framework/Core/GlobalCompositionRoot.cs
git commit -m "feat: add global services (Input, Audio, Scene)"
```

---

### Task 8: GameInstaller + 场景组合根

**Files:**
- Create: `Assets/HotUpdate/GameInstaller.cs`
- Create: `Assets/HotUpdate/Scenes/GameScene/GameSceneRoot.cs`

说明: 此 Task 创建 HotUpdate 的基础设施，但暂不注册具体业务类型（后续 Task 逐步添加）。

- [ ] **Step 1: 创建 GameInstaller.cs**

```csharp
using VContainer;
using GameFramework;

namespace HotUpdate
{
    public class GameInstaller : IHotUpdateInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            // 业务服务
            // 后续 Task 中在此注册 PlayerService 等业务服务
        }
    }
}
```

- [ ] **Step 2: 创建 GameSceneRoot.cs**

```csharp
using VContainer;
using VContainer.Unity;
using GameFramework;

namespace HotUpdate
{
    /// <summary>
    /// 场景级组合根。挂载到游戏场景的 GameObject 上，作为 GlobalCompositionRoot 的子 LifetimeScope。
    /// 注册场景级的 Presenter、Model 等（Transient/Scoped 生命周期）。
    /// </summary>
    public class GameSceneRoot : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 场景级注册（后续 Task 中逐步添加）
            // builder.Register<PlayerPresenter>(Lifetime.Transient);
            // builder.Register<PlayerModel>(Lifetime.Scoped);
        }
    }
}
```

- [ ] **Step 3: 验证 HotUpdate 程序集中通过反射发现 GameInstaller**

在 GlobalCompositionRoot 中 `appBuilder.RegisterHotUpdate()` 被调用。Play 模式下检查 Console 无报错。可以在 AppBuilder.RegisterHotUpdate 中加临时 Debug.Log 验证类型被发现。

- [ ] **Step 4: 等待 Unity 编译通过**

- [ ] **Step 5: 提交**

```bash
git add Assets/HotUpdate/GameInstaller.cs Assets/HotUpdate/GameInstaller.cs.meta
git add Assets/HotUpdate/Scenes/GameScene/GameSceneRoot.cs Assets/HotUpdate/Scenes/GameScene/GameSceneRoot.cs.meta
git commit -m "feat: add GameInstaller and GameSceneRoot"
```

---

### Task 9: 创建 PlayerModel（示例 Model）

**Files:**
- Create: `Assets/HotUpdate/Models/PlayerModel.cs`

- [ ] **Step 1: 创建 PlayerModel.cs**

```csharp
using GameFramework;

namespace HotUpdate
{
    public class PlayerModel : Model
    {
        public ReactiveProperty<int> Gold { get; } = new(0);
        public ReactiveProperty<int> Level { get; } = new(1);
        public ReactiveProperty<string> Name { get; } = new("Player");

        public void AddGold(int amount)
        {
            Gold.Value += amount;
        }

        public void LevelUp()
        {
            Level.Value++;
        }

        public void SetName(string name)
        {
            Name.Value = name;
        }
    }
}
```

- [ ] **Step 2: 等待 Unity 编译通过**

- [ ] **Step 3: 提交**

```bash
git add Assets/HotUpdate/Models/PlayerModel.cs Assets/HotUpdate/Models/PlayerModel.cs.meta
git commit -m "feat: add PlayerModel example"
```

---

### Task 10: 创建 PlayerService（示例 Service）

**Files:**
- Create: `Assets/HotUpdate/Services/PlayerService.cs`
- Create: `Assets/HotUpdate/Services/IPlayerService.cs` (可选，如果不需要跨程序集暴露则省略)

说明: 因为 PlayerService 仅在 HotUpdate 内部使用，不需要在 Framework 中定义接口。直接创建即可。

- [ ] **Step 1: 创建 PlayerService.cs**

```csharp
using System.Collections.Generic;
using GameFramework;

namespace HotUpdate
{
    public interface IPlayerService : IService
    {
        PlayerModel GetPlayerModel();
        void SavePlayerData();
    }

    public class PlayerService : IPlayerService
    {
        PlayerModel _cachedModel;

        public PlayerModel GetPlayerModel()
        {
            if (_cachedModel == null)
            {
                _cachedModel = new PlayerModel();
                // 模拟加载存档数据
                _cachedModel.SetName("Hero");
                _cachedModel.AddGold(100);
            }
            return _cachedModel;
        }

        public void SavePlayerData()
        {
            // 持久化保存逻辑
            UnityEngine.Debug.Log($"[PlayerService] Saved: {_cachedModel.Name.Value}, Gold={_cachedModel.Gold.Value}");
        }
    }
}
```

- [ ] **Step 2: 更新 GameInstaller.cs 注册业务服务**

```csharp
public void Install(IContainerBuilder builder)
{
    builder.Register<IPlayerService, PlayerService>(Lifetime.Singleton);
}
```

- [ ] **Step 3: 等待 Unity 编译通过**

- [ ] **Step 4: 提交**

```bash
git add Assets/HotUpdate/Services/PlayerService.cs Assets/HotUpdate/Services/PlayerService.cs.meta
git add Assets/HotUpdate/GameInstaller.cs
git commit -m "feat: add PlayerService with model lifecycle management"
```

---

### Task 11: 创建 PlayerView（示例 View）

**Files:**
- Create: `Assets/HotUpdate/Views/IPlayerView.cs`
- Create: `Assets/HotUpdate/Views/PlayerView.cs`

- [ ] **Step 1: 创建 IPlayerView.cs**

```csharp
using System;
using GameFramework;

namespace HotUpdate
{
    public interface IPlayerView : IView
    {
        void UpdateGold(int gold);
        void UpdateLevel(int level);
        void UpdateName(string name);
        event Action OnLevelUpClicked;
    }
}
```

- [ ] **Step 2: 创建 PlayerView.cs**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;
using GameFramework;

namespace HotUpdate
{
    public class PlayerView : MonoBehaviour, IPlayerView
    {
        [SerializeField] Text _goldText;
        [SerializeField] Text _levelText;
        [SerializeField] Text _nameText;
        [SerializeField] Button _levelUpButton;

        PlayerPresenter _presenter;
        LifetimeScope _subScope;

        public event Action OnLevelUpClicked;

        void Awake()
        {
            var parentScope = GetComponentInParent<LifetimeScope>();
            _subScope = parentScope.CreateChild(builder =>
            {
                builder.RegisterInstance<IPlayerView>(this);
            });
            _presenter = _subScope.Resolve<PlayerPresenter>();
            _presenter.Initialize();

            _levelUpButton?.onClick.AddListener(() => OnLevelUpClicked?.Invoke());
        }

        void OnDestroy()
        {
            _presenter?.Dispose();
            _subScope?.Dispose();
        }

        public void UpdateGold(int gold)
        {
            if (_goldText != null) _goldText.text = $"金币: {gold}";
        }

        public void UpdateLevel(int level)
        {
            if (_levelText != null) _levelText.text = $"等级: {level}";
        }

        public void UpdateName(string name)
        {
            if (_nameText != null) _nameText.text = name;
        }
    }
}
```

- [ ] **Step 3: 等待 Unity 编译通过**

- [ ] **Step 4: 提交**

```bash
git add Assets/HotUpdate/Views/IPlayerView.cs Assets/HotUpdate/Views/IPlayerView.cs.meta
git add Assets/HotUpdate/Views/PlayerView.cs Assets/HotUpdate/Views/PlayerView.cs.meta
git commit -m "feat: add PlayerView (MonoBehaviour) with sub-scope DI pattern"
```

---

### Task 12: 创建 PlayerPresenter（集成验证）

**Files:**
- Create: `Assets/HotUpdate/Presenters/PlayerPresenter.cs`
- Modify: `Assets/HotUpdate/GameInstaller.cs` (注册 PlayerPresenter)
- Modify: `Assets/HotUpdate/Scenes/GameScene/GameSceneRoot.cs` (注册场景级类型)

- [ ] **Step 1: 创建 PlayerPresenter.cs**

```csharp
using GameFramework;

namespace HotUpdate
{
    public class PlayerPresenter : Presenter<IPlayerView>
    {
        readonly PlayerModel _model;
        readonly IPlayerService _service;

        public PlayerPresenter(IPlayerView view, PlayerModel model, IPlayerService service)
            : base(view)
        {
            _model = model;
            _service = service;
        }

        public override void Initialize()
        {
            // 订阅 Model 响应式属性，自动更新 View
            AddDisposable(_model.Gold.Subscribe(v => View.UpdateGold(v)));
            AddDisposable(_model.Level.Subscribe(v => View.UpdateLevel(v)));
            AddDisposable(_model.Name.Subscribe(v => View.UpdateName(v)));

            // 订阅 View 事件，调用 Service
            View.OnLevelUpClicked += HandleLevelUp;

            // 立即刷新一次 UI
            View.UpdateGold(_model.Gold.Value);
            View.UpdateLevel(_model.Level.Value);
            View.UpdateName(_model.Name.Value);
        }

        public override void Dispose()
        {
            View.OnLevelUpClicked -= HandleLevelUp;
            _service.SavePlayerData();
            base.Dispose();
        }

        void HandleLevelUp()
        {
            _model.LevelUp();
        }
    }
}
```

- [ ] **Step 2: 注册 Presenter 到 DI**

更新 `GameSceneRoot.cs`:

```csharp
protected override void Configure(IContainerBuilder builder)
{
    builder.Register<PlayerPresenter>(Lifetime.Transient);
    builder.Register<PlayerModel>(Lifetime.Scoped);
}
```

更新 `GameInstaller.cs`（注册 PlayerModel 工厂或保持 Service 提供）：

```csharp
public void Install(IContainerBuilder builder)
{
    builder.Register<IPlayerService, PlayerService>(Lifetime.Singleton);
}
```

- [ ] **Step 3: 在 Unity Editor 中创建测试场景**

手动操作：
1. 新建场景 `Assets/Scenes/GameTest.unity`
2. 创建空 GameObject，挂载 `GameSceneRoot`
3. 在 Canvas 下创建 PlayerView GameObject（含 Text 和 Button 子节点）
4. 将相关 UI 引用拖入 PlayerView 的 SerializeField
5. 打开 Build Settings，添加 Bootstrap 和 GameTest 场景

- [ ] **Step 4: Play 模式验证**

运行 Bootstrap 场景（或直接在 GameTest 场景中测试）：
- PlayerView 应显示 PlayerModel 初始化数据
- 点击升级按钮，等级数字应更新
- Console 无报错

- [ ] **Step 5: 提交**

```bash
git add Assets/HotUpdate/Presenters/PlayerPresenter.cs Assets/HotUpdate/Presenters/PlayerPresenter.cs.meta
git add Assets/HotUpdate/Scenes/GameScene/GameSceneRoot.cs
git add Assets/HotUpdate/GameInstaller.cs
git add Assets/Scenes/GameTest.unity Assets/Scenes/GameTest.unity.meta
git commit -m "feat: add PlayerPresenter with full MVP integration"
```

---

### Task 13: HybridCLR 配置准备

**Files:**
- Create: `Assets/HybridCLR/README.txt` (配置说明)

- [ ] **Step 1: 创建 HybridCLR 配置说明文件**

```txt
HybridCLR 集成步骤：

1. 安装 HybridCLR 包：
   - 通过 Unity Package Manager 添加 git URL
   - 或从 https://github.com/focus-creative-games/hybridclr 下载

2. 配置 HybridCLR Settings：
   - Window > HybridCLR > Settings
   - 设置 HotUpdate 程序集列表: ["HotUpdate"]

3. 补充元数据：
   - 在 AOT 端调用 HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly
   - 确保 VContainer 泛型路径的元数据已补充

4. 热更新 DLL 加载：
   - 从 AssetBundle/网络下载 HotUpdate.dll.bytes
   - 通过 Assembly.Load(byte[]) 加载
   - GlobalCompositionRoot 中的 RegisterHotUpdate() 会自动发现

5. View Prefab 管理：
   - HotUpdate 中的 View Prefab 打包为 AssetBundle
   - 运行时加载 Prefab 并挂载到场景

当前 Framework 和 HotUpdate 的程序集分离已就绪，可直接接入 HybridCLR。
```

- [ ] **Step 2: 提交**

```bash
git add Assets/HybridCLR/README.txt Assets/HybridCLR/README.txt.meta
git commit -m "docs: add HybridCLR integration guide"
```
