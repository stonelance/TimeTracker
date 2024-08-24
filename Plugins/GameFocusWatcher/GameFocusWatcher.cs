using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeTracker;
using TimeTracker.Watchers;

namespace GameFocusWatcher
{
    public class GameFocusWatcher : BaseWatcher
    {
        public enum State
        {
            Inactive,
            Active,
        }

        public State CurrentState;

        public class KnownGame
        {
            public string Title;
            public string Aumid;
            public string ExePath;
        }

        private DateTime knownGameListUpdateTime;
        private List<KnownGame> knownGameList;

        public override bool IsActive
        {
            get
            {
                return this.CurrentState == State.Active;
            }
        }

        public GameFocusWatcher(string displayName, ActivityId activity, IDictionary<string, JToken> settings)
            : base(displayName, activity)
        {
        }

        private void UpdateKnownGameList()
        {
            var mruGamesListPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\GameDVR\\GameMRU\\LocalMruGameList.json");
            if (File.Exists(mruGamesListPath))
            {
                var latestUpdateTime = File.GetLastWriteTime(mruGamesListPath);

                if (this.knownGameListUpdateTime < latestUpdateTime)
                {
                    this.knownGameListUpdateTime = latestUpdateTime;

                    var mruGamesListJson = File.ReadAllText(mruGamesListPath);
                    var obj = JObject.Parse(mruGamesListJson);
                    this.knownGameList = obj.SelectToken("titles").ToObject<List<KnownGame>>();
                }
            }
        }

        public override void OnForegroundProcessNameChanged(string foregroundProcessName)
        {
            State newState = State.Inactive;

            UpdateKnownGameList();

            if (this.knownGameList.FirstOrDefault(x => String.Equals(foregroundProcessName, Path.GetFileNameWithoutExtension(x.ExePath), StringComparison.OrdinalIgnoreCase)) != null)
            {
                newState = State.Active;
            }

            if (this.CurrentState != newState)
            {
                this.CurrentState = newState;
                OnPropertyChanged("");
            }
        }
    }
}
