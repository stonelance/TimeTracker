using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Threading.Tasks;
using TimeTracker;

namespace TwinklyPlugin
{
    public class TwinklyPlugin : ITrackerPlugin
    {
        public string Name { get { return "TwinklyPlugin"; } }
        public string Description { get { return "This plugin allows changing the state of a Twinkly LED light based on the currently active activity"; } }

        private ActivityManager activityManager;
        private PluginSettings settings;
        private Twinkly twinkly;
        private int ledCount;
        private ActivityId? activeActivityId;
        private DateTime lastLoginAttempt;

        public class PluginSettings
        {
            public string DeviceIP;
        }

        public class ActivitySettings
        {
            public Color LedColor;
            public string EffectId;
        }

        public void Initialize(Configuration configuration, ActivityManager activityManager, JObject settings)
        {
            this.activityManager = activityManager;
            this.settings = settings.ToObject<PluginSettings>();

            this.twinkly = new Twinkly(this.settings.DeviceIP);

            // Try to connect to the twinkly in the background
            this.lastLoginAttempt = DateTime.Now;
            this.twinkly.Login().ContinueWith(async (_) =>
                {
                    if (this.twinkly.IsLoggedIn)
                    {
                        var deviceInfo = await this.twinkly.GetDeviceInfo();
                        this.ledCount = deviceInfo.number_of_led;

                        //var effectIds = this.twinkly.GetLedEffects();

                        var v = await this.twinkly.GetDriverParams();

                        await UpdateTwinklyState();
                    }
                });
        }

        private async Task UpdateTwinklyState()
        {
            if (!this.twinkly.IsLoggedIn &&
                (DateTime.Now - this.lastLoginAttempt) > TimeSpan.FromMinutes(5))
            {
                this.lastLoginAttempt = DateTime.Now;
                await this.twinkly.Login();
            }

            if (this.twinkly.IsLoggedIn && this.activeActivityId.HasValue)
            {
                var activitySettings = this.activityManager.GetPluginSettingsFromActivity(this.Name, this.activeActivityId.Value)?.ToObject<ActivitySettings>();
                var activityColor = activitySettings?.LedColor ?? new Color();
                var effectId = activitySettings?.EffectId;

                /*
                var pixels = new byte[3 * this.ledCount];
                for (int i = 0; i < this.ledCount; ++i)
                {
                    pixels[i * 3 + 0] = activityColor.R;
                    pixels[i * 3 + 1] = activityColor.G;
                    pixels[i * 3 + 2] = activityColor.B;
                }

                this.twinkly.SetMode(Twinkly.ModeType.Off);
                this.twinkly.GetLedReset();
                this.twinkly.UploadMovie(pixels);
                this.twinkly.SetMode(Twinkly.ModeType.Movie);
                this.twinkly.SetMovieConfig(0, 64, 1);
                */

                if (!String.IsNullOrEmpty(effectId))
                {
                    await this.twinkly.SetMode(Twinkly.ModeType.Effect, effectId);
                }
                else
                {
                    await this.twinkly.SetMode(Twinkly.ModeType.Color);
                    await this.twinkly.SetLedColor(activityColor.R, activityColor.G, activityColor.B);
                }
            }
        }

        public void OnActiveActivityChanged(ActivityId activityId)
        {
            this.activeActivityId = activityId;
            UpdateTwinklyState().ConfigureAwait(false);
        }

        public void Shutdown()
        {
            if (this.twinkly.IsLoggedIn)
            {
                // Turn it off when the app shuts down
                this.twinkly.SetMode(Twinkly.ModeType.Off).Wait();
            }
        }
    }
}
