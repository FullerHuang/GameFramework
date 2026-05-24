using System.Threading.Tasks;

namespace GameFramework
{
    public interface ISceneService : IService
    {
        Task LoadSceneAsync(string sceneName);
        string CurrentSceneName { get; }
    }
}
