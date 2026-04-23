using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;


using contracts;

Console.WriteLine("===============");
Console.WriteLine("DISPLAY DISPLAY");
Console.WriteLine("===============");


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/sudoku", async () => {
    var client = new HttpClient();
    var response = await client.GetFromJsonAsync<new_sudokus>(
        "http://localhost:5121/new"
    );

    return Results.Ok(response?.boards);
});


app.MapGet("/stats/{nome}", async (string nome) => {
    var client = new HttpClient();


    var response = await client.GetAsync($"http://localhost:5127/stats/{nome}");

    if (! response.IsSuccessStatusCode)
    {

        return Results.NotFound("coagulo secreto");
    }


    var stats = await client.GetFromJsonAsync<user_stats>(
        $"http://localhost:5127/stats/{nome}"
    );

    return Results.Ok(stats);
});

app.Run();
