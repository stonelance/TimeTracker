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
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new System.Uri(this.urlPath + url);
                if (!string.IsNullOrEmpty(this.authToken))
                {
                    client.DefaultRequestHeaders.Add("x-auth-token", this.authToken);
                }
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                HttpContent content = new StringContent(data, UTF8Encoding.UTF8, "application/json");
                try
                {
                    HttpResponseMessage messge = await client.PostAsync(url, content).ConfigureAwait(false);
                    string description = string.Empty;
                    if (messge.IsSuccessStatusCode)
                    {
                        string result = messge.Content.ReadAsStringAsync().Result;
                        description = result;
                    }
                    else if (messge.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        await Login().ConfigureAwait(false);
                        return await PostData(data, url).ConfigureAwait(false);
                    }

                    return JObject.Parse(description);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        private async Task<JObject> PostRaw(byte[] data, string url)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new System.Uri(this.urlPath + url);
                if (!string.IsNullOrEmpty(this.authToken))
                {
                    client.DefaultRequestHeaders.Add("x-auth-token", this.authToken);
                }
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                HttpContent content = new ByteArrayContent(data);
                HttpResponseMessage messge = await client.PostAsync(url, content).ConfigureAwait(false);
                string description = string.Empty;
                if (messge.IsSuccessStatusCode)
                {
                    string result = messge.Content.ReadAsStringAsync().Result;
                    description = result;
                }
                else if (messge.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await Login().ConfigureAwait(false);
                    return await PostRaw(data, url).ConfigureAwait(false);
                }

                return JObject.Parse(description);
            }
        }

        private async Task<JObject> DoGet(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new System.Uri(this.urlPath + url);
                if (!string.IsNullOrEmpty(this.authToken))
                {
                    client.DefaultRequestHeaders.Add("x-auth-token", this.authToken);
                }
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage messge = await client.GetAsync(url).ConfigureAwait(false);
                if (messge.IsSuccessStatusCode)
                {
                    string result = messge.Content.ReadAsStringAsync().Result;
                    return JObject.Parse(result);
                }
                else if (messge.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await Login().ConfigureAwait(false);
                    return await DoGet(url).ConfigureAwait(false);
                }

                return new JObject();
            }
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
                await PostData($"{{\"mode\": \"{mode.ToString().ToLowerInvariant()}\", \"effect_id\": \"{effectId}\"}}", "/xled/v1/led/mode").ConfigureAwait(false);
            }
            else
            {
                await PostData($"{{\"mode\": \"{mode.ToString().ToLowerInvariant()}\"}}", "/xled/v1/led/mode").ConfigureAwait(false);
            }
        }

        public async Task<ModeType> GetMode()
        {
            var response = await DoGet("/xled/v1/led/mode");
            return (ModeType)Enum.Parse(typeof(ModeType), response.ToString());
        }

        public async Task<string> GetLedReset()
        {
            var response = await DoGet("/xled/v1/led/reset");
            return response.ToString();
        }

        public async Task<string[]> GetLedEffects()
        {
            var response = await DoGet("/xled/v1/led/effects");
            return response["unique_ids"].ToObject<string[]>();
        }

        public async Task<string> GetLedColor()
        {
            var response = await DoGet("/xled/v1/led/color");
            return response.ToString();
        }

        public async Task SetLedColor(byte r, byte g, byte b)
        {
            string data = $"{{\"red\": {r}, \"green\": {g}, \"blue\": {b}}}";
            await PostData(data, "/xled/v1/led/color");
        }

        public async Task UploadMovie(byte[] data)
        {
            await PostRaw(data, "/xled/v1/led/movie/full");
        }

        // delay in milliseconds, led count (doesn't seem to do anything?), number of frames to use
        public async Task SetMovieConfig(int delayInMs, int ledCount, int frameCount)
        {
            string data = $"{{\"frame_delay\": {delayInMs},\"leds_number\": {ledCount},\"frames_number\": {frameCount}}}";
            await PostData(data, "/xled/v1/led/movie/config");
        }

        public async Task<string> GetMovieConfig()
        {
            var response = await DoGet("/xled/v1/led/movie/config");
            return response.ToString();
        }

        public async Task<string> GetFirewareVersion()
        {
            var response = await DoGet("/xled/v1/fw/version");
            return response.ToString();
        }

        public async Task<string> GetTimer()
        {
            var response = await DoGet("/xled/v1/timer");
            return response.ToString();
        }

        public async Task SetTimer(int time_on, int time_now, int time_off)
        {
            string data = $"{{\"time_on\": {time_on},\"time_now\": {time_now},\"time_off\": {time_off}}}";
            await PostData(data, "/xled/v1/timer");
        }

        public async Task<string> GetDeviceName()
        {
            var response = await DoGet("/xled/v1/device_name");
            return response.ToString();
        }

        public async Task SetDeviceName(string name)
        {
            await PostData($"{{\"name\", \"{name}\"}}", "/xled/v1/device_name");
        }

        public async Task<string> GetNetworkScan()
        {
            var response = await DoGet("/xled/v1/network/scan");
            return response.ToString();
        }

        public async Task<string> GetNetworkScanResults()
        {
            var response = await DoGet("/xled/v1/network/scan_results");
            return response.ToString();
        }


        public async Task SetRTFrame(byte[] data)
        {
            await PostRaw(data, "/xled/v1/led/rt/frame");
        }

        public async Task UdpRtFrame(byte[] data, int numLeds)
        {
            /*
            header = [0] * 10;
            frame[0] = 0x00;
            frame = "".join(map(chr, frame));
            */
        }

        public async Task<string> GetDriverParams()
        {
            var response = await DoGet("/xled/v1/led/driver_params2");
            return response.ToString();
        }

        public async Task SetDriverParams(int timing_adjust_1 = 10, int timing_adjust_2 = 62)
        {
            string data = $"{{\"timing_adjust_2\": {timing_adjust_2}, \"timing_adjust_1\": {timing_adjust_1}}}";
            await PostData(data, "/xled/v1/led/driver_params2");
        }

        public async Task<string> GetMqtt()
        {
            var response = await DoGet("/xled/v1/mqtt/config");
            return response.ToString();
        }

        public async Task SetLedConfig(int first_led_id = 0, int length = 200)
        {
            string data = $"{{\"strings\": [{{ \"first_led_id\": {first_led_id}, \"length\": {length}}}]}}";
            await PostData(data, "/xled/v1/led/config");
        }

        public async Task<string> GetLedConfig()
        {
            var response = await DoGet("/xled/v1/led/config");
            return response.ToString();
        }

        public async Task<string> GetNetworkStatus()
        {
            var response = await DoGet("/xled/v1/network/status");
            return response.ToString();
        }

        public async Task SetEcho(string message)
        {
            await PostData(message, "/xled/v1/echo");
        }

        public async Task<string> GetProductionInfo()
        {
            var response = await DoGet("/xled/v1/production_info");
            return response.ToString();
        }

        public async Task<string> GetStatus()
        {
            var response = await DoGet("/xled/v1/status");
            return response.ToString();
        }

        public async Task<string> GetReset2()
        {
            var response = await DoGet("/xled/v1/led/reset2");
            return response.ToString();
        }

        public async Task<string> GetOffsets()
        {
            var response = await DoGet("/xled/v1/fw/offsets");
            return response.ToString();
        }
    }
}
