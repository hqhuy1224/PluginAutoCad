using Newtonsoft.Json;
using PluginAutoCad.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PluginAutoCad.Services
{
    public class AuthService
    {
        private readonly string _authUrl;

        public AuthService(string authUrl)
        {
            _authUrl = authUrl;
        }

        public async Task<string> LoginAsync(string username, string password)
        {
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
                {
                    { "client_id", "customer" },
                    { "client_secret", "z5GkMQvkMyihDiCAzExUU99mmA39GLJQ" },
                    { "username", username },
                    { "password", password },
                    { "grant_type", "password" }
                };

                var body = new FormUrlEncodedContent(values);

                var response = await client.PostAsync(
                    $"{_authUrl}/realms/Customer/protocol/openid-connect/token",
                    body);

                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception(json);

                var token = JsonConvert.DeserializeObject<TokenResponse>(json);

                return token.access_token;
            }
        }
    }
}