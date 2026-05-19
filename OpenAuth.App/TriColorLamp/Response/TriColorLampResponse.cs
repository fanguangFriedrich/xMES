using System.Text.Json.Serialization;

namespace OpenAuth.App.TriColorLamp.Response
{
    public class TriColorLampTokenResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; }

        [JsonPropertyName("data")]
        public TriColorLampTokenData Data { get; set; }
    }

    public class TriColorLampTokenData
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
}
