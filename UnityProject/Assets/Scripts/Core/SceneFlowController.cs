using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimaJugada
{
    public static class SceneFlowController
    {
        public const string MenuScene = "MainMenu";
        public const string MatchScene = "Match";
        public const string ResultScene = "MatchResult";

        public static void PlayNewMatch()
        {
            GameManager.Instance.NewMatch();
            SceneManager.LoadScene(MatchScene);
        }
        public static void ShowResult() => SceneManager.LoadScene(ResultScene);
        public static void MainMenu() => SceneManager.LoadScene(MenuScene);
        public static void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
