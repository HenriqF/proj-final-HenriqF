using dotenv.net;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using System.Reflection.Metadata;
using System.Security.Cryptography; /* eu MARCELO botei isso */
using contracts;
using System.Text.Json;
public class Usuario
{
    public int id {get; set;}
    public string? email {get; set;}
    public byte[]? senha_hash {get; set;}

    public byte[]? senha_salt {get; set;}
    public string? nome {get; set;}

    public string? foto_link {get; set;}

    public Stats? Stats { get; set; }
    public ICollection<Partida>? JogosUserGanhador {get; set;}
    public ICollection<Partida>? JogosUserDerrotado {get; set;}
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

public class Partida
{
    public int id {get; set;}
    public int user_ganhador {get; set;}
    public int user_derrotado {get; set;}
    public string? tabuleiros {get;set;}

    public Usuario? UserGanhador { get; set; }
    public Usuario? UserDerrotado { get; set; }
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
        modelBuilder.Entity<Usuario>(user =>
        {   
            user.ToTable("usuarios");
            user.HasKey(u => u.id);


            user.HasOne(u => u.Stats).WithOne(s => s.Usuario).HasForeignKey<Stats>(s => s.user_id);
        });

        modelBuilder.Entity<Partida>(part =>
        {
            part.ToTable("partidas");
            part.HasKey(p => p.id);

            part.HasOne(p => p.UserGanhador).WithMany(u => u.JogosUserGanhador).HasForeignKey(p => p.user_ganhador).OnDelete(DeleteBehavior.Restrict);
            part.HasOne(p => p.UserDerrotado).WithMany(u => u.JogosUserDerrotado).HasForeignKey(p => p.user_derrotado).OnDelete(DeleteBehavior.Restrict);
        });
    }
}




public class Program
{

    static async Task<(string nome, int elo)[]?> leaderboard(AppDbContext cont)
    {
        try
        {
            (string, int)[]? tp = await cont.usuarios.Join(cont.sudoku_stats, u => u.id, s => s.user_id, (u, s) => new { u.nome, s.user_elo })
                                        .OrderByDescending(i => i.user_elo)
                                        .Take(100)
                                        .Select(i => ValueTuple.Create(i.nome!, i.user_elo))
                                        .ToArrayAsync();

            
            return tp.Length > 0 ? tp : null;
        }
        catch
        {
            return null;
        }
    }

    static async Task<bool> novo_user(AppDbContext cont, string nome, string email, string senha, int elo = 1000, string foto_link = ".")
    {
        try
        {
            if (!foto_link.StartsWith("https://i.pinimg.com/"))
            {
                foto_link = "https://i.pinimg.com/1200x/36/bd/a2/36bda22a62ac3d53be8c6664e7f0df31.jpg";
            }

            using var hmac = new HMACSHA512();
            
            byte[] senhaSalt = hmac.Key;
            byte[] senhaHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(senha));

            Usuario novo = new Usuario
            {
                email = email,
                senha_hash = senhaHash,
                senha_salt = senhaSalt,
                nome = nome,
                foto_link = foto_link,
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

    static async Task<bool> nova_partida(AppDbContext cont, string ganhador, string derrotado, string tabuleiros)
    {
        try
        {
            Usuario? u_ganhador = await find_user(cont, ganhador);
            Usuario? u_derrotado = await find_user(cont, derrotado);
            if (u_ganhador == null || u_derrotado == null)
            {
                return false;
            }

            Partida nova = new Partida
            {
                user_ganhador = u_ganhador.id,
                user_derrotado = u_derrotado.id,
                tabuleiros = tabuleiros
            };

            cont.Add(nova);
            await cont.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e}");
            return false;
        }
        return true;
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






    static async Task testar(AppDbContext cont)
    {
        Usuario? analise = await find_user(cont, "pedro");
        if (analise != null) goto deu_ruim;

        Console.WriteLine("pedro não exite");

        if (!await novo_user(cont, "pedro", "wow.com", "123")) goto deu_ruim;

        Console.WriteLine("pedro criado");

        analise = await find_user(cont, "pedro");
        if (analise == null) goto deu_ruim;

        Console.WriteLine($"pedro existe: {analise.id}");

        if (!await novo_user(cont, "pedro_sigma", "wow.com@hudson", "12344", 1200)) goto deu_ruim;
        Console.WriteLine("pedro_sigma criado");

        if (!await nova_partida(cont, "pedro", "pedro_sigma", "abc")) goto deu_ruim;
        Console.WriteLine($"Criada partida entre pedro e pedro_sigma");


        await novo_user(cont, "almeida", "al@hudson", "12344", 950);
        await novo_user(cont, "roberto", "bert@hudson", "12344", 5500);
        await novo_user(cont, "hudson", "maxmilneclimb@hudson", "12344", 5600);
        await novo_user(cont, "daniel", "janjagarnbret@hudson", "12344", 2200);
        await novo_user(cont, "joao", "aimori@hudson", "12344", 2256);


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

        if (args.Length > 0 && args[0] == "teste") await testar(cont);

        

        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        app.UseHttpsRedirection();


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

            int g_pos = cont.sudoku_stats.Count(s => s.user_elo > analise.Stats.user_elo) + 1;



            return Results.Ok( new user_stats(
                analise.id,
                analise.email!,
                analise.foto_link,

                analise.Stats.user_elo,
                analise.Stats.qtd_jogos_ganhos,
                analise.Stats.qtd_jogos_jogados,
                analise.Stats.melhor_tempo,

                g_pos
            ));
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


            return Results.Ok(new user_private_info(analise.nome, analise.email, _senhaHash, _senhaSalt));
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

                await novo_user(cont, cadastro.nome, cadastro.email, cadastro.senha);
                return Results.Ok(new user_private_info(cadastro.nome, cadastro.email, "", ""));
            }
            catch
            {
                return Results.Conflict("nnao criado");
            }
        });

        app.MapGet("/leaderboard", async () =>
        {
            (string nome, int elo)[]? res = await leaderboard(cont);
            if (res == null)
            {
                return Results.NoContent();
            }
 
            return Results.Ok(res.Select(u => new {u.nome, u.elo}));
        });
        app.Run();
    }

}

public record Cadastro(string nome, string email, string senha);