using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;
using Geracao.Sudoku;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Drawing;

namespace Sockets.WebSocketServer;
public class WebSocketServer
{
    //variaveis    
    private static ConcurrentDictionary<string, WebSocket> _clients = new();


    private static int _queue_buckets_size;
    private static ConcurrentDictionary<int, ConcurrentBag<(string, int)>> _queue = new();
    private static ConcurrentDictionary<string, bool> _is_queued = new();




    private static Sudoku _sudoku = new Sudoku(6, 3, 2);
    //-------

    static async Task MessageClientAsync(string message, WebSocket webSocket)
    {
        try
        {
            byte[] response = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(response), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch
        {

        }
    }

    static async Task HandleClientAsync(string id, WebSocket webSocket)
    {
        var buffer = new byte[1024];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close) break;

                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"{id}: {message}");

                if (message.StartsWith("jogar") && message.Length == 10)
                {
                    string elo_string = message.Substring(5);
                    int elo;
                    string resposta;

                    if (_is_queued.ContainsKey(id))
                    {
                        if (_is_queued[id] == true)
                        {
                            resposta = $"ja na queue.";
                            await MessageClientAsync(resposta, webSocket);
                            continue;
                        }
                        
                    }

                    if (!int.TryParse(elo_string, out elo))
                    {
                        resposta = "elo indevido";
                        await MessageClientAsync(resposta, webSocket);
                        continue;
                    }


                    int bucket = (elo/_queue_buckets_size)*_queue_buckets_size;
                    var elo_bucket = _queue.GetOrAdd(bucket, _ => new ConcurrentBag<(string, int)>());

                    if (elo_bucket.TryTake(out (string, int) player))
                    {
                        string opp_id = player.Item1;
                        int opp_elo = player.Item2;
                        _is_queued[opp_id] = false;

                        string jogo = _sudoku.new_boards()[0];

                        resposta = $"nova match! {opp_elo} vs {elo} -> {jogo}";


                        await MessageClientAsync(resposta, webSocket);
                        await MessageClientAsync(resposta, _clients[opp_id]);
                        continue;
                    }


                        


                    elo_bucket.Add((id, elo));
                    _is_queued[id] = true;

                    resposta = $"entrou na queue {elo} -> {bucket}";
                    await MessageClientAsync(resposta, webSocket);

                    



                    // var builder = new StringBuilder();
                    // builder.Append(resposta);
                    // builder.Append('[');
                    // foreach (var p in elo_bucket)
                    // {
                    //     builder.Append($"{p.Item2} ");
                    // }
                    // builder.Append(']');
                    // resposta = builder.ToString();


                }
                else
                {
                    await MessageClientAsync(message, webSocket);
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n\nerro com '{id}': {ex.Message}\n\n");
        }
        finally
        {
            _clients.TryRemove(id, out _);
            if (webSocket.State != WebSocketState.Aborted)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
    }


    public static async Task start()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();
        
        Console.WriteLine("Servidor ligado");

        _queue_buckets_size = 50;
        // for (int i = 0; i < _queue_buckets_size*50; i+=_queue_buckets_size)
        // {
        //     _queue.TryAdd(i, new ConcurrentBag<(string, int)>());
        // }


        while (true)
        {
            var context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                var wsContext = await context.AcceptWebSocketAsync(null);
                string clientID = Guid.NewGuid().ToString();

                Console.WriteLine($"novo cliente: {clientID}\n");
                _clients.TryAdd(clientID, wsContext.WebSocket);
                _ = Task.Run(() => HandleClientAsync(clientID, wsContext.WebSocket));
                await MessageClientAsync($"voce é {clientID}", wsContext.WebSocket);
            }
        }
    }


}