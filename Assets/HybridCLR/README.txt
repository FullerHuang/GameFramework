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
