using UnityEngine;

namespace UltimaJugada
{
    /// <summary>The only persistent object. Scene objects never own the match state.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance
        {
            get
            {
                if (instance == null) new GameObject("GameSession").AddComponent<GameManager>();
                return instance;
            }
        }
        public MatchEngine Match { get; private set; }
        public MatchSettings Settings { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            var config = Resources.Load<MatchConfig>("MatchConfig");
            Settings = config != null ? config.settings : new MatchSettings();
        }

        public void NewMatch(int? seed = null) => Match = new MatchEngine(Settings, seed ?? System.Environment.TickCount);
        public MatchEngine EnsureMatch() { if (Match == null) NewMatch(); return Match; }
    }
}
