using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using api.DTOs;
using api.Service;
using Microsoft.AspNetCore.Mvc;

namespace api;

[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, List<Stream>> _roomClients = new();
    private static readonly object _lock = new object();
    

    [HttpGet(nameof(Connect))]
    public async Task Connect([FromQuery] string room)
    {
        //validate room querry parameter
        if (string.IsNullOrEmpty(room))
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("Room parameter is required");
            return;

        }
        
        //SSE headers
        //for browser so it knows about it is a live stream
        Response.Headers.ContentType = "text/event-stream";
        //disabling caching for immediate update
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        //Add current clients to the list, Locking list so it thread safety
        lock (_lock)
        {
            if (!_roomClients.ContainsKey(room))
            {
                _roomClients[room] = new List<Stream>();
            }
            _roomClients[room].Add(Response.Body);
        }
        
        Console.WriteLine($"Client connected to room: {room}");
        //flush so client knows that connection is accepted
        await Response.Body.FlushAsync();

        try
        {
            //Keeps connection alive
            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                //check every second if client is alive
                await Task.Delay(1000);
            }
        }
        finally
        {
            //cleans if the client disconnected or broken
            lock (_lock)
            {
                if (_roomClients.ContainsKey(room))
                {
                    _roomClients[room].Remove(Response.Body);
                    
                    if (_roomClients[room].Count == 0)
                    {
                        _roomClients.TryRemove(room, out _);
                    }
                }
            }

            Console.WriteLine($"Client disconnected: {room}");
        }
    }
    
    [HttpPost(nameof(SendMessage))]
    public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
    {
        if (string.IsNullOrEmpty(request.Username)) return BadRequest("Username reqired");
        if (string.IsNullOrEmpty(request.Content)) return BadRequest("Cannot be empty");
        if (string.IsNullOrEmpty(request.Room)) return BadRequest("Room required");

        var formattedMessage = $"{request.Username}: {request.Content}";
        //convert string to bytes
        byte[] buffer = Encoding.UTF8.GetBytes($"data: {formattedMessage}\n\n");

        await BroadcastToRoom(request.Room, buffer);
        
        return Ok("Sent message. ");
    }

    [HttpPost(nameof(UserTyping))]
    public async Task<IActionResult> UserTyping([FromBody] TypingRequest request)
    {
        var data = JsonSerializer.Serialize(new { username = request.Username });
        var sseMessage = $"event: typing\ndata: {data}\n\n";
        byte[] buffer = Encoding.UTF8.GetBytes(sseMessage);

        // 2. Reuse the same list of clients you use for SendMessage
        await BroadcastToRoom(request.Room, buffer);
        return Ok();
    }


    private async Task BroadcastToRoom(string room, byte[] buffer)
    {
        List<Stream> currentClients;
        
        lock (_lock)
        {
            if (!_roomClients.ContainsKey(room))
            {
                return;
            }

            currentClients = new List<Stream>(_roomClients[room]);
        }

        foreach (var clientStream in currentClients)
        {
            try
            {
                await clientStream.WriteAsync(buffer);
                await clientStream.FlushAsync();
            }
            catch
            {
                // Ideally remove broken clients here, but ignoring is fine for now
            }
        }
    }


}


    
    
