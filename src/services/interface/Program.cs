using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;


using contracts;

Console.WriteLine("===================");
Console.WriteLine("INTERFACE INTERFACE");
Console.WriteLine("===================");


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();



app.MapGet("/stats/{nome}", async (string nome) => {
    return Results.Ok(nome);
}).WithName("GetUserStats");

app.Run();
