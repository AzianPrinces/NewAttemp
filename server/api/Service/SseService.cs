using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace api.Service;

public class SseService
{
    private readonly List<HttpResponse> _clients = new();

    public async Task BroadcastTypingAsync(string username)
    {
        var data = System.Text.Json.JsonSerializer.Serialize(new
            { username });
        
        //formating 
        var sseMessage = $"event: typing\ndata: {data}\n\n";

        foreach (var client in _clients.ToList())
        {
            try
            {
                await client.WriteAsync(sseMessage);
                await client.Body.FlushAsync();
            }
            catch
            {
                _clients.Remove(client);
            }
        }
    }
}