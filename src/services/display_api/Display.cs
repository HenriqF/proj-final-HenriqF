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

app.Run();
