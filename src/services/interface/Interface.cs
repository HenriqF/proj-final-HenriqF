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
using System.Text.Unicode; /* eu MARCELO botei isso */



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


async Task<string> Cadastrar(Cadastro dados)
{
    var client = new HttpClient();
    try
    {
        var i = await client.PostAsJsonAsync($"http://localhost:5127/cadastrar", dados);
        if (!i.IsSuccessStatusCode)
        {
            return "JATEM";
        }
        return "CADASTRADO";
    }
    catch
    {
        return "JATEM";
    }

}







var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => //marcelo aqui
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   // This sets Access-Control-Allow-Origin: *
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
var app = builder.Build();

app.UseCors("AllowAll"); //marcelo aqui

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/login", async (Login data) =>
{   

    try
    {
        var client = new HttpClient();  
        user_info? dados = await client.GetFromJsonAsync<user_info>(
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
        user_info? dados = await client.GetFromJsonAsync<user_info>(
            $"http://localhost:5127/find/{data.nome}"
        );
        return Results.Conflict("Coagulo já existe");
    }
    catch
    {
        Task<string> t  = Cadastrar(data);
        string resposta = await t;
        return Results.Created($"/login/", new Login(data.nome, data.senha));  
    }

}).WithName("Cadastro");



app.Run();

public record Login(string nome, string senha);
public record Cadastro(string nome, string email, string senha);