using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Cryptography; /* eu MARCELO botei isso */
using System.Net.Http.Json; /* eu MARCELO botei isso */
using System.Threading.Tasks; /* eu MARCELO botei isso */
using System.Net.Http; /* eu MARCELO botei isso */
using Microsoft.IdentityModel.Tokens; /* eu Marcelo botei isso */
using contracts;
using System.Collections.Specialized;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Unicode;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Net; /* eu MARCELO botei isso */

using System.Text;
using System.Collections.Concurrent;



Console.WriteLine("===================");
Console.WriteLine("INTERFACE INTERFACE");
Console.WriteLine("===================");



string CriarToken(string nome, string email)
    {
        List<Claim> clains = new List<Claim>()
        {
        new Claim("Email", email),
        new Claim("Username", nome)
        };

        var silencio = "abobrinhacomemolesesoltabbvemdancarcomigochatovelhocomibostaontemnaomintofalosoverdades"; //tbm esta no appsettings

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(silencio));

        var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
        claims: clains,
        expires: DateTime.Now.AddDays(1),
        signingCredentials: cred
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return jwt;
    }


async Task<bool> Cadastrar(Cadastro dados)
{
    var client = new HttpClient();
    try
    {
        var i = await client.PostAsJsonAsync($"http://localhost:5127/cadastrar", dados);
        if (!i.IsSuccessStatusCode)
        {
            return false;
        }
        return true;
    }
    catch
    {
        return false;
    }

}


/* 
CLIENTE -> INTERFACE (dê-me token, esse sou eu: JWT)
INTERFACE -> CLIENTE (token de entrada: 123)
CLIENTE -> WEBSOCKET (eu posso entrar : 123)
WEBSOCKET -> INTERFACE (ele pode entrar? 123)
*/


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => //marcelo aqui
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
var app = builder.Build();
app.UseCors("AllowAll"); //marcelo aqui

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();


app.MapGet("/leaderboard", async () =>
{
    var client = new HttpClient();

    var response = await client.GetAsync("http://localhost:5127/leaderboard");

    if (response.StatusCode == HttpStatusCode.NoContent || ! response.IsSuccessStatusCode)
    {
        return Results.NoContent();
    }

    var dados = await client.GetFromJsonAsync<List<player_elo_rel>>("http://localhost:5127/leaderboard");

    var formatado = dados!.Select(u => new object[] {u.nome, u.elo}).ToList();

    return Results.Ok(formatado);
});


app.MapPost("/login", async (Login data) =>
{   

    try
    {
        var client = new HttpClient();  
        user_private_info? dados = await client.GetFromJsonAsync<user_private_info>(
            $"http://localhost:5127/find/{data.nome}"
        );

        if (dados == null)
        {
            return Results.NotFound("CREDINV");
        }

        byte[] senhaHash = Convert.FromBase64String(dados.hash);
        byte[] senhaSalt = Convert.FromBase64String(dados.salt);

        using var hmac = new HMACSHA512(senhaSalt);
        var ComputeHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data.senha));


        if (!ComputeHash.SequenceEqual(senhaHash))
        {
            return Results.NotFound("CREDINV");
        }

        var jwt = CriarToken(dados.nome, dados.email);


        return Results.Ok( jwt );

    }
    catch
    {
        return Results.NotFound("CREDINV");
    }

}).WithName("Login");


app.MapPost("/cadastro", async (Cadastro data) =>
{
    try
    {
        var client = new HttpClient();  
        user_private_info? dados = await client.GetFromJsonAsync<user_private_info>(
            $"http://localhost:5127/find/{data.nome}"
        );
        return Results.Conflict("Coagulo já existe");
    }
    catch
    {
        Task<bool> t  = Cadastrar(data);
        if (await t)
        {
            return Results.Created($"/login/", new Login(data.nome, data.senha));  
        }
        return Results.Conflict("Email em uso");
    }

}).WithName("Cadastro");


app.MapGet("/stats/{nome}", async (string nome) => {
    var client = new HttpClient();


    var response = await client.GetAsync($"http://localhost:5127/stats/{nome}");

    if (! response.IsSuccessStatusCode)
    {
        return Results.NotFound("coagulo inexistente?");
    }


    var stats = await client.GetFromJsonAsync<user_stats>(
        $"http://localhost:5127/stats/{nome}"
    );

    return Results.Ok(stats);
}).WithName("GetUserStats");


ConcurrentDictionary<string, (DateTime, string)> solicitacoes = new();
ConcurrentDictionary<string, string> solicitando = new();


app.MapGet("/jogartoken/{nome}", (string nome) =>
{
    if (solicitando.ContainsKey(nome))
    {
        if (solicitacoes.TryGetValue(solicitando[nome], out (DateTime, string) info))
        {
            if (info.Item1 <= DateTime.Now)
            {
                solicitacoes.TryRemove(solicitando[nome], out _);
                solicitando.TryRemove(info.Item2, out _);
            }
        }

        return Results.Conflict("já solicitado");
    }
    if (solicitacoes.Count > 5000)
    {
        return Results.StatusCode(503);
    }

    string token = Guid.NewGuid().ToString("N");

    solicitacoes.TryAdd(token, (DateTime.Now.AddSeconds(10), nome));
    solicitando.TryAdd(nome, token);

    return Results.Ok(token);
});

app.MapGet("/confirmar/{token}", (string token) =>
{
    if (solicitacoes.TryGetValue(token, out (DateTime, string) info))
    {
        if (info.Item1 >= DateTime.Now)
        {
            solicitacoes.TryRemove(token, out _);
            solicitando.TryRemove(info.Item2, out _);
            return Results.Ok(info.Item2);
        }

        return Results.Unauthorized();
    }
    else
    {
        return Results.Unauthorized();
    }

});

app.Run();

public record Login(string nome, string senha);
public record Cadastro(string nome, string email, string senha);