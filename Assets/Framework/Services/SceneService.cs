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
