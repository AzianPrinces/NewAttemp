using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace api;

[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private static readonly List<Stream> _clients = new List<Stream>();
    private static readonly object _lock = new object();

    [HttpGet(nameof(Connect))]
    public async Task Connect()
    {
        //for browser so it knows about it is a live stream
        Response.Headers.ContentType = "text/event-stream";
        
        //disabling caching for immediate update
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        //Add current clients to the list, Locking list so it thread safety
        lock (_lock)
        {
            _clients.Add(Response.Body);
        }
        
        //flush so client knows that connection is accepted
        await Response.Body.FlushAsync();

        try
        {
            //Keeps loop alive as long as there is connection
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
                _clients.Remove(Response.Body);
            }

            Console.WriteLine("Client disconnected");
        }
    }
    
    [HttpPost(nameof(SendMessage))]
    public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
    {
        if (string.IsNullOrEmpty(request.Content)) return BadRequest("Cannot be empty");
        
        //convert string to bytes
        byte[] buffer = Encoding.UTF8.GetBytes($"data: {request.Content}\n\n");
        
        //Copy the list to avoid errors while sending if someone is disconnected
        List<Stream> currentClients;
        lock (_lock)
        {
            currentClients = new List<Stream>(_clients);
        }

        foreach (var clientStream in currentClients)
        {
            try
            {
                //Write bytes to specific clients stream
                await clientStream.WriteAsync(buffer);
                await clientStream.FlushAsync();
            }
            catch
            {
                
            }
        }
        return Ok($"Sent to {currentClients.Count} clients. ");
    }
    
    
        
    }


    
    
