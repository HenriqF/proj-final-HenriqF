using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Drawing;

using contracts;


namespace Sockets.WebSocketServer;
public class WebSocketServer
{
    //variaveis    
    private static ConcurrentDictionary<string, WebSocket> _clients = new();

    //-------

    static async Task MessageClientAsync(string message, WebSocket webSocket)
    {
        try
        {
            byte[] response = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(response), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception e)
        {
            Console.WriteLine($"erro ao mandar mensagem: {e}");
            return;
        }
    }

    static async Task HandleClientAsync(string id, WebSocket webSocket)
    {
        var buffer = new byte[1024];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close) break;

            string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"{id}: {message}");

            if (message == "jogar")
            {
                var client = new HttpClient();
                new_sudokus? response = await client.GetFromJsonAsync<new_sudokus>(
                    "http://localhost:5121/new"
                );

                if (response == null)
                {
                    throw new Exception("falha ao gerar sudoku.");
                }
                await MessageClientAsync(response.boards[0] , webSocket);

            }
            else
            {
                await MessageClientAsync(message , webSocket);
            }


        }
    
    }

    public static void Main(string[] args)
    {
        Console.WriteLine("===================");
        Console.WriteLine("WEBSOCKET WEBSOCKET");
        Console.WriteLine("===================");


        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.UseWebSockets();

        app.Map("/ws", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var web_socket = await context.WebSockets.AcceptWebSocketAsync();
            string client_id = Guid.NewGuid().ToString();

            try
            {
                _clients.TryAdd(client_id, web_socket);

                Console.WriteLine($"novo cliente: {client_id}");
                await MessageClientAsync($"voce é {client_id}", web_socket);
                
                await HandleClientAsync(client_id, web_socket);
            }
            catch 
            {
                Console.WriteLine($"erro fatal: {client_id}");
            }
            finally
            {
                _clients.TryRemove(client_id, out _);
                Console.WriteLine($"saiu: {client_id}");
            }
        });

        app.Run();
    }
}