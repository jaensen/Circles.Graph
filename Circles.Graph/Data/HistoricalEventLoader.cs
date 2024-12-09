using System.Text;
using System.Text.Json;
using Circles.Graph.Rpc;

namespace Circles.Graph;

public class HistoricalEventLoader(string httpUrl)
{
    public async Task<List<CirclesEventResult>> LoadHistoricalEventsAsync()
    {
        var requestBody = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "circles_events",
            @params = new object?[]
            {
                null,
                0,
                null,
                new[]
                {
                    "CrcV2_Trust",
                    "CrcV2_TransferSingle",
                    "CrcV2_TransferBatch"
                }
            }
        };

        var jsonResponse = await SendHttpRequestAsync(httpUrl, requestBody);
        var parsedResponse = JsonSerializer.Deserialize<CirclesEventsResponse>(jsonResponse,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new CirclesEventResultConverter() }
            });

        parsedResponse?.Result?.Reverse();
        return parsedResponse?.Result ?? new List<CirclesEventResult>();
    }

    private static async Task<string> SendHttpRequestAsync(string url, object requestBody)
    {
        using var httpClient = new HttpClient();
        var jsonRequest = JsonSerializer.Serialize(requestBody);
        var response = await httpClient.PostAsync(
            url,
            new StringContent(jsonRequest, Encoding.UTF8, "application/json"));
        return await response.Content.ReadAsStringAsync();
    }
}