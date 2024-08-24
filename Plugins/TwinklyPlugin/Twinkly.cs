using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace TwinklyPlugin
{
    // This logic is based on python script, but i don't remember where it was from.
    internal class Twinkly
    {
        public enum ModeType
        {
            Off,
            Color,
            Demo, // starts predefined sequence of effects that are changed after few seconds
            Movie, // plays predefined or uploaded effect. If movie hasn’t been set (yet) code 1104 is returned.
            RT, // receive effect in real time
            Effect, // play effect with effect_id
            Playlist, // firmware >=2.5.6
            Restart
        };

        private string authToken;
        private string ip;
        private string urlPath;

        public bool IsLoggedIn { get { return !String.IsNullOrEmpty(this.authToken); } }

        public Twinkly(string ip)
        {
            this.authToken = string.Empty;
            this.ip = ip;
            this.urlPath = "http://" + ip;
        }

        private async Task<JObject> PostData(string data, string url)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new System.Uri(this.urlPath + url);
            if (!string.IsNullOrEmpty(this.authToken))
            {
                client.DefaultRequestHeaders.Add("x-auth-token", this.authToken);
            }
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            HttpContent content = new StringContent(data, UTF8Encoding.UTF8, "application/json");
            try
            {
                HttpResponseMessage messge = await client.PostAsync(url, content);
                string description = string.Empty;
                if (messge.IsSuccessStatusCode)
                {
                    string result = messge.Content.ReadAsStringAsync().Result;
                    description = result;
                }
                else if (messge.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await Login();
                    return await PostData(data, url);
                }

                return JObject.Parse(description);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<JObject> PostRaw(byte[] data, string url)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new System.Uri(this.urlPath + url);
            if (!string.IsNullOrEmpty(this.authToken))
            {
                client.DefaultRequestHeaders.Add("x-auth-token", this.authToken);
            }
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            HttpContent content = new ByteArrayContent(data);
            HttpResponseMessage messge = await client.PostAsync(url, content);
            string description = string.Empty;
            if (messge.IsSuccessStatusCode)
            {
                string result = messge.Content.ReadAsStringAsync().Result;
                description = result;
            }
            else if (messge.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await Login();
                return await PostRaw(data, url);
            }

            return JObject.Parse(description);
        }

        private async Task<JObject> DoGet(string url)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new System.Uri(this.urlPath + url);
            if (!string.IsNullOrEmpty(this.authToken))
            {
                client.DefaultRequestHeaders.Add("x-auth-token", this.authToken);
            }
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage messge = await client.GetAsync(url);
            if (messge.IsSuccessStatusCode)
            {
                string result = messge.Content.ReadAsStringAsync().Result;
                return JObject.Parse(result);
            }
            else if (messge.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await Login();
                return await DoGet(url);
            }

            return new JObject();
        }

        public async Task Login()
        {
            var challenge = new String('A', 64);

            string data = $"{{\"challenge\": \"{challenge}\"}}";
            var jResp = await PostData(data, "/xled/v1/login");
            if (jResp != null)
            {
                this.authToken = jResp["authentication_token"].Value<string>();
                var chall = jResp["challenge-response"];
                data = $"{{\"challenge-response\": \"{chall}\"}}";

                await PostData(data, "/xled/v1/verify");
            }
            else
            {
                this.authToken = null;
            }
        }

        public async Task Logout()
        {
            await PostData("", "/xled/v1/logout");
        }

        public async Task<JObject> GetDeviceInfo()
        {
            return await DoGet("/xled/v1/gestalt");
        }

        public async Task SetMode(ModeType mode, string effectId = null)
        {
            if (effectId != null)
            {
                await PostData($"{{\"mode\": \"{mode.ToString().ToLowerInvariant()}\", \"effect_id\": \"{effectId}\"}}", "/xled/v1/led/mode");
            }
            else
            {
                await PostData($"{{\"mode\": \"{mode.ToString().ToLowerInvariant()}\"}}", "/xled/v1/led/mode");
            }
        }

        public ModeType GetMode()
        {
            return (ModeType)Enum.Parse(typeof(ModeType), DoGet("/xled/v1/led/mode").ToString());
        }

        public string GetLedReset()
        {
            return DoGet("/xled/v1/led/reset").ToString();
        }

        public string[] GetLedEffects()
        {
            var v = DoGet("/xled/v1/led/effects").Result;
            return v["unique_ids"].ToObject<string[]>();
        }

        public string GetLedColor()
        {
            return DoGet("/xled/v1/led/color").ToString();
        }

        public async Task SetLedColor(byte r, byte g, byte b)
        {
            string data = $"{{\"red\": {r}, \"green\": {g}, \"blue\": {b}}}";
            await PostData(data, "/xled/v1/led/color");
        }

        public void UploadMovie(byte[] data)
        {
            PostRaw(data, "/xled/v1/led/movie/full").Wait();
        }

        // delay in milliseconds, led count (doesn't seem to do anything?), number of frames to use
        public void SetMovieConfig(int delayInMs, int ledCount, int frameCount)
        {
            string data = $"{{\"frame_delay\": {delayInMs},\"leds_number\": {ledCount},\"frames_number\": {frameCount}}}";
            PostData(data, "/xled/v1/led/movie/config").Wait();
        }

        public string GetMovieConfig()
        {
            return DoGet("/xled/v1/led/movie/config").Result.ToString();
        }

        public string GetFirewareVersion()
        {
            return DoGet("/xled/v1/fw/version").Result.ToString();
        }

        public string GetTimer()
        {
            return DoGet("/xled/v1/timer").Result.ToString();
        }

        public void SetTimer(int time_on, int time_now, int time_off)
        {
            string data = $"{{\"time_on\": {time_on},\"time_now\": {time_now},\"time_off\": {time_off}}}";
            PostData(data, "/xled/v1/timer").Wait();
        }

        public string DeviceName
        {
            get
            {
                return DoGet("/xled/v1/device_name").Result.ToString();
            }
            set
            {
                PostData($"{{\"name\", \"{value}\"}}", "/xled/v1/device_name").Wait();
            }
        }

        public string GetNetworkScan()
        {
            return DoGet("/xled/v1/network/scan").Result.ToString();
        }

        public string GetNetworkScanResults()
        {
            return DoGet("/xled/v1/network/scan_results").Result.ToString();
        }


        public void SetRTFrame(byte[] data)
        {
            PostRaw(data, "/xled/v1/led/rt/frame").Wait();
        }

        public void UdpRtFrame(byte[] data, int numLeds)
        {
            /*
            header = [0] * 10;

            frame[0] = 0x00;

            frame = "".join(map(chr, frame));
            */
        }

        public string GetDriverParams()
        {
            return DoGet("/xled/v1/led/driver_params2").ToString();
        }

        public void SetDriverParams(int timing_adjust_1 = 10, int timing_adjust_2 = 62)
        {
            string data = $"{{\"timing_adjust_2\": {timing_adjust_2}, \"timing_adjust_1\": {timing_adjust_1}}}";
            PostData(data, "/xled/v1/led/driver_params2").Wait();
        }

        public string GetMqtt()
        {
            return DoGet("/xled/v1/mqtt/config").ToString();
        }

        public void SetLedConfig(int first_led_id = 0, int length = 200)
        {
            string data = $"{{\"strings\": [{{ \"first_led_id\": {first_led_id}, \"length\": {length}}}]}}";
            PostData(data, "/xled/v1/led/config").Wait();
        }

        public string GetLedConfig()
        {
            return DoGet("/xled/v1/led/config").ToString();
        }

        public string GetNetworkStatus()
        {
            return DoGet("/xled/v1/network/status").ToString();
        }

        public void SetEcho(string message)
        {
            PostData(message, "/xled/v1/echo").Wait();
        }

        public string GetProductionInfo()
        {
            return DoGet("/xled/v1/production_info").ToString();
        }

        public string GetStatus()
        {
            return DoGet("/xled/v1/status").ToString();
        }

        public string GetReset2()
        {
            return DoGet("/xled/v1/led/reset2").ToString();
        }

        public string GetOffsets()
        {
            return DoGet("/xled/v1/fw/offsets").ToString();
        }
    }
}
