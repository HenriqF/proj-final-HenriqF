using dotenv.net;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using System.Reflection.Metadata;
using System.Security.Cryptography; /* eu MARCELO botei isso */
using contracts;
public class Usuario
{
    public int id {get; set;}
    public string? email {get; set;}
    public byte[]? senha_hash {get; set;}

    public byte[]? senha_salt {get; set;}
    public string? nome {get; set;}

    public Stats? Stats { get; set; }
}

public class Stats
{
    [Key]
    public int user_id {get; set;}
    public int user_elo {get; set;} = 1000;
    public int qtd_jogos_jogados {get; set;}
    public int qtd_jogos_ganhos {get; set;}
    public int melhor_tempo  {get; set;}


    public Usuario? Usuario { get; set; }
}


public class AppDbContext : DbContext
{
    public DbSet<Usuario> usuarios { get; set; }
    public DbSet<Stats> sudoku_stats { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        DotEnv.Load();

        var builder = new MySqlConnectionStringBuilder
        {
            Server = Environment.GetEnvironmentVariable("HOST"),
            Port = 3306,
            Database = Environment.GetEnvironmentVariable("MYSQL_DATABASE"),
            UserID = "root",
            Password = Environment.GetEnvironmentVariable("MYSQL_ROOT_PASSWORD")
        };

        var connection = builder.ConnectionString;

        options.UseMySql(connection, ServerVersion.AutoDetect(connection));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Stats).WithOne(s => s.Usuario)
            .HasForeignKey<Stats>(s => s.user_id);
    }
}



public class Program
{
    static async Task<bool> new_user(AppDbContext cont, string nome, string email, string senha, int elo = 1000)
    {
        try
        {
            using var hmac = new HMACSHA512();
            
            byte[] senhaSalt = hmac.Key;
            byte[] senhaHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(senha));

            Usuario novo = new Usuario
            {
                email = email,
                senha_hash = senhaHash,
                senha_salt = senhaSalt,
                nome = nome,
                Stats = new Stats{user_elo = elo},
            };

            cont.Add(novo);
            await cont.SaveChangesAsync();

        }
        catch
        {
            return false;
        }

        return true;
    }

    static async Task<Usuario?> find_user(AppDbContext cont, string nome)
    {
        Usuario? user = null;
        try
        {
            user = await cont.usuarios.Include(u => u.Stats).FirstOrDefaultAsync(u =>
                (
                    u.nome == nome
                )
            );
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e}");
            return user;
        }

        return user;
    }

    static async Task<Usuario?> userDoEmail(AppDbContext cont, string email)
    {
        Usuario? user = null;
        try
        {
            user = await cont.usuarios.FirstOrDefaultAsync(u =>
                (
                    u.email == email
                )
            );
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e}");
            return user;
        }

        return user;
    }



    static async void testar(AppDbContext cont)
    {
        Usuario? analise = await find_user(cont, "pedro");
        if (analise != null) goto deu_ruim;

        Console.WriteLine("pedro não exite");

        if (!await new_user(cont, "pedro", "wow.com", "123")) goto deu_ruim;

        Console.Write("pedro criado");

        analise = await find_user(cont, "pedro");
        if (analise == null) goto deu_ruim;

        Console.WriteLine($"pedro existe: {analise.email} {analise.id}");


        await new_user(cont, "pedro_sigma", "wow.com@hudson", "12344", 1200);
        await new_user(cont, "almeida", "al@hudson", "12344", 950);
        await new_user(cont, "roberto", "bert@hudson", "12344", 5500);
        await new_user(cont, "hudson", "maxmilneclimb@hudson", "12344", 5600);
        await new_user(cont, "daniel", "janjagarnbret@hudson", "12344", 2200);
        await new_user(cont, "joao", "aimori@hudson", "12344", 2256);


        Environment.Exit(0);


        deu_ruim:
            Console.WriteLine("Deu Ruim");
            Environment.Exit(1);
    }


    static async Task Main(string[] args)
    {
        Console.WriteLine("==============");
        Console.WriteLine("BANCO DE DADOS ");
        Console.WriteLine("==============");


        using var cont = new AppDbContext();

        if (!cont.Database.CanConnect())
        {
            Console.WriteLine("sem conexao banco");
            Environment.Exit(1);
        }

        if (args[0] == "teste") testar(cont);

        

        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();


        app.MapGet("/stats/{nome}", async (string nome) =>
        {
            Usuario? analise = await find_user(cont, nome);
            if (analise == null)
            {
                return Results.NotFound("Coagulo nao existe");
            }
            if (analise.Stats == null)
            {
                return Results.NotFound("Coagulo nao tem stats?");
            }

            return Results.Ok( new{
                elo = analise.Stats.user_elo,
                vitorias = analise.Stats.qtd_jogos_ganhos,
                partidas = analise.Stats.qtd_jogos_jogados,
                melhor_tempo = analise.Stats.melhor_tempo,
            });
        });

        app.MapGet("/find/{nome}", async (string nome) =>
        {
            Usuario? analise = await find_user(cont, nome);
            if (analise == null|| analise.senha_hash==null||analise.senha_salt ==null ||analise.nome==null||analise.email==null)
            {
                return Results.NotFound("Coagulo nao existe");
            }

            string _senhaHash = Convert.ToBase64String(analise.senha_hash);
            string _senhaSalt = Convert.ToBase64String(analise.senha_salt);


            return Results.Ok(new user_info(analise.nome, analise.email, _senhaHash, _senhaSalt));
        });
        
        app.MapPost("/cadastrar", async (Cadastro cadastro) =>
        {
            try
            {
                Usuario? us = await userDoEmail(cont, cadastro.email);
                if(us != null)
                {
                    return Results.Conflict("nnao criado");
                }

                await new_user(cont, cadastro.nome, cadastro.email, cadastro.senha);
                return Results.Ok(new user_info(cadastro.nome, cadastro.email, "", ""));
            }
            catch
            {
                return Results.Conflict("nnao criado");
            }
        });


        app.Run();
    }

}

public record Cadastro(string nome, string email, string senha);