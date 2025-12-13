using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace BankClient.Services
{
    public class CurrencyService
    {
        private readonly HttpClient _httpClient;
        private const string API_URL = "https://api.exchangerate-api.com/v4/latest/USD";
        private Dictionary<string, decimal> _cachedRates;
        private DateTime _lastUpdate;

        public CurrencyService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            _cachedRates = new Dictionary<string, decimal>();
            _lastUpdate = DateTime.MinValue;
        }

        public async Task<Dictionary<string, decimal>> GetLatestRatesAsync()
        {
            try
            {
                if (_cachedRates.Count > 0 && (DateTime.Now - _lastUpdate).TotalSeconds < 30)
                {
                    return new Dictionary<string, decimal>(_cachedRates);
                }

                var response = await _httpClient.GetStringAsync(API_URL);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var data = JsonSerializer.Deserialize<ExchangeRateResponse>(response, options);

                if (data?.Rates != null && data.Rates.Count > 0)
                {
                    _cachedRates = data.Rates;
                    _lastUpdate = DateTime.Now;
                    return new Dictionary<string, decimal>(data.Rates);
                }

                return GetFallbackRates();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Network error fetching currency rates: {ex.Message}");
                return GetFallbackRates();
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Request timeout while fetching currency rates");
                return GetFallbackRates();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parsing error: {ex.Message}");
                return GetFallbackRates();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error fetching currency rates: {ex.Message}");
                return GetFallbackRates();
            }
        }

        private Dictionary<string, decimal> GetFallbackRates()
        {
            if (_cachedRates.Count > 0)
            {
                return new Dictionary<string, decimal>(_cachedRates);
            }

            return new Dictionary<string, decimal>
            {
                { "EUR", 0.92m },
                { "GBP", 0.79m },
                { "JPY", 149.50m },
                { "CHF", 0.88m },
                { "CAD", 1.36m },
                { "AUD", 1.52m },
                { "CNY", 7.24m },
                { "INR", 83.12m }
            };
        }

        public class ExchangeRateResponse
        {
            [JsonPropertyName("rates")]
            public Dictionary<string, decimal> Rates { get; set; }

            [JsonPropertyName("base")]
            public string Base { get; set; }

            [JsonPropertyName("date")]
            public string Date { get; set; }
        }
    }
}