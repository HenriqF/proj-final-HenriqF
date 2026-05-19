using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Drawing;
using System.Net.Http.Json; /* eu MARCELO botei isso */
using System.Threading.Tasks; /* eu MARCELO botei isso */
using System.Net.Http; /* eu MARCELO botei isso */
using System.Security.Cryptography; /* eu Marcelo botei isso */
using Microsoft.IdentityModel.Tokens; /* eu Marcelo botei isso */

using contracts;
using System.Collections.Specialized;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt; /* eu MARCELO botei isso */


namespace Sockets.WebSocketServer;
public class WebSocketServer
{
    //variaveis    
    private static ConcurrentDictionary<string, WebSocket> _clients_sockets = new();

    private static int _elo_prestiege_size = 200;
    private static ConcurrentDictionary<int, string> _queued_clients = new();  

    private static ConcurrentDictionary<string, string[]> _playing_clients = new();  //player, sudoku_board
    private static ConcurrentDictionary<string, string> _playing_opponent = new();

    //-------


    private static void RemovePlayingClient(string id)
    {
        _playing_clients.TryRemove(id, out _);
        _playing_opponent.TryRemove(id, out _);
    }

    private static async Task<string[]> FindUser(string player_name, WebSocket webSocket)
    {
        var client = new HttpClient();

        var response = await client.GetAsync($"http://localhost:5127/find/{player_name}");

        string[] erro = {"!"};
        
        if (! response.IsSuccessStatusCode)
        {
            Console.WriteLine($"{response}");
            return erro;
        }

        var user = await response.Content.ReadFromJsonAsync<string[]>();

        if (user == null)
        {
            return erro;
        }
        
        return user;
    }

    private static async void FindMatch(string player_name, string id, WebSocket webSocket)
    {
        var client = new HttpClient();

        var get_stats_response = await client.GetAsync($"http://localhost:5127/stats/{player_name}");

        if (! get_stats_response.IsSuccessStatusCode)
        {
            await MessageClientAsync("player nao existe...?" , webSocket);
            return;
        }

        var stats = await client.GetFromJsonAsync<user_stats>(
            $"http://localhost:5127/stats/{player_name}"
        );
        if (stats == null)
        {
            await MessageClientAsync("player sem estatisticas...?" , webSocket);
            return;
        }

        int elo_bucket = stats.elo/_elo_prestiege_size;
        if (_queued_clients.ContainsKey(elo_bucket))
        {
            if (_queued_clients[elo_bucket] == id)
            {
                await MessageClientAsync("já está dentro da queue...", webSocket);
                return;
            }

            if (_queued_clients.TryRemove(elo_bucket, out string? opp_id))
            {
                new_sudokus? sudoku = await client.GetFromJsonAsync<new_sudokus>(
                    "http://localhost:5121/new"
                );

                if (sudoku == null)
                {
                    await MessageClientAsync("falha ao gerar sudokus...", webSocket);
                    await MessageClientAsync("falha ao gerar sudokus...", _clients_sockets[opp_id]);
                    return;
                }

                _playing_clients.TryAdd(id, sudoku.boards);
                _playing_clients.TryAdd(opp_id, sudoku.boards);

                _playing_opponent.TryAdd(id, opp_id);
                _playing_opponent.TryAdd(opp_id, id);

                await MessageClientAsync("sudoku:" + sudoku.boards[0], webSocket);
                await MessageClientAsync("sudoku:" + sudoku.boards[0], _clients_sockets[opp_id]);

                Console.WriteLine(sudoku.boards[0]);
                Console.WriteLine(sudoku.boards[1]);
                return;
            }
        }


        if (_queued_clients.TryAdd(elo_bucket, id))
        {
            await MessageClientAsync($"procurando por oponente...", webSocket);
            return;
        }
        else
        {
            await MessageClientAsync($"falha em entrar na queue.", webSocket);
            return;
        }
    }


    private static async Task MessageClientAsync(string message, WebSocket webSocket)
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

    private static async Task HandleClientAsync(string id, WebSocket webSocket)
    {
        var buffer = new byte[1024];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close) break;

            string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"{id}: {message}");


            if (_playing_clients.ContainsKey(id))
            {
                if (message == _playing_clients[id][1])
                {
                    await MessageClientAsync("perdeu:" , _clients_sockets[_playing_opponent[id]]);
                    await MessageClientAsync("ganhou:" , webSocket);

                    RemovePlayingClient(_playing_opponent[id]);
                    RemovePlayingClient(id);
                }
                else
                {
                    await MessageClientAsync("echo:" + message , webSocket);
                }
            }


            else if (message.StartsWith("jogar ") && message.Length > 6)
            {
                string player_name = message.Substring(6);
                FindMatch(player_name, id, webSocket);
            }


            else
            {
                await MessageClientAsync("echo:" + message , webSocket);
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

        app.Map("/ws/{token}", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest) return;

            string? token = context.Request.RouteValues["token"]?.ToString();
            if (token == null) return;


            var client = new HttpClient();
            var response = await client.GetAsync($"http://localhost:5269/confirmar/{token}");

            if (response.StatusCode == HttpStatusCode.Unauthorized) return;
            string usuario = await response.Content.ReadAsStringAsync();


            using var web_socket = await context.WebSockets.AcceptWebSocketAsync();
            string client_id = usuario;

            if (_clients_sockets.ContainsKey(client_id))return;


            try
            {
                _clients_sockets.TryAdd(client_id, web_socket);

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
                _clients_sockets.TryRemove(client_id, out _);
                Console.WriteLine($"saiu: {client_id}");
            }
        });

        app.Run();
    }
}