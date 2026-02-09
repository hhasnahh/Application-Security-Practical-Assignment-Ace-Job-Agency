using System.Text.Json;

namespace AceJobAgencyPortal.Services
{
    public class RecaptchaV3
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _cfg;

        public RecaptchaV3(IHttpClientFactory http, IConfiguration cfg)
        {
            _http = http;
            _cfg = cfg;
        }

        public async Task<(bool ok, double score, string err)> VerifyAsync(string token, string expectedAction)
        {
            var secret = _cfg["Recaptcha:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret))
                return (false, 0, "Missing reCAPTCHA SecretKey");

            var client = _http.CreateClient();

            var res = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={Uri.EscapeDataString(secret)}&response={Uri.EscapeDataString(token)}",
                null);

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;

            var success = root.TryGetProperty("success", out var s) && s.GetBoolean();
            var score = root.TryGetProperty("score", out var sc) ? sc.GetDouble() : 0;
            var action = root.TryGetProperty("action", out var a) ? a.GetString() : "";

            if (!success) return (false, score, "not_success");
            if (!string.Equals(action, expectedAction, StringComparison.OrdinalIgnoreCase))
                return (false, score, "action_mismatch");

            // Typical threshold 0.5
            if (score < 0.5) return (false, score, "low_score");

            return (true, score, "");
        }
    }
}
