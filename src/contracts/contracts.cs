
using System.Text.Json.Serialization;
namespace contracts;

public record new_sudokus(string[] boards);

public record api_message(string message);

public record user_stats(

    [property: JsonPropertyName("elo")] int elo,
    [property: JsonPropertyName("vitorias")]int vitorias,
    [property: JsonPropertyName("partidas")]int partidas,
    [property: JsonPropertyName("melhor_tempo")]int melhor_tempo

);


public record user_info ( 
    string nome,
    string email,
    string hash,
    string salt
);

public record player_elo_rel (
    string nome,
    int elo
);