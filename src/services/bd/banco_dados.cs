using dotenv.net;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using System.Reflection.Metadata;

public class Usuario
{
    public int id {get; set;}
    public string? email {get; set;}
    public string? senha_hash {get; set;}
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
            Server = "127.0.0.1",
            Port = 3306,
            Database = "sudoku",
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
    static bool new_user(AppDbContext cont, string nome, string email, string senha, int elo = 1000)
    {
        try
        {

            Usuario novo = new Usuario
            {
                email = email,
                senha_hash = senha,
                nome = nome,
                Stats = new Stats{user_elo = elo},
            };

            cont.Add(novo);
            cont.SaveChanges();
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e}");
            return false;
        }

        return true;
    }


    static Usuario? find_user(AppDbContext cont, string nome)
    {
        Usuario? user = null;
        try
        {
            user = cont.usuarios.Include(u => u.Stats).FirstOrDefault(u =>
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


    static void Main(string[] args)
    {
        Console.WriteLine("==============");
        Console.WriteLine("BANCO DE DADOS ");
        Console.WriteLine("==============");


        using var cont = new AppDbContext();

        if (!cont.Database.CanConnect())
        {
            Console.WriteLine("sem conexao banco");
            return;
        }





        // new_user(cont, "pedro", "wow.com", "123");
        // new_user(cont, "pedro_sigma", "wow.com@porra", "12344", 1200);
        // new_user(cont, "almeida", "al@porra", "12344", 950);
        // new_user(cont, "roberto", "bert@porra", "12344", 5500);
        // new_user(cont, "porra", "maxmilneclimb@porra", "12344", 5600);
        // new_user(cont, "daniel", "janjagarnbret@porra", "12344", 2200);
        // new_user(cont, "joao", "aimori@porra", "12344", 2256);


        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/stats/{nome}", (string nome) =>
        {
            Usuario? analise = find_user(cont, nome);
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

        app.Run();
    }

}