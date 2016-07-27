using System.Collections.Generic;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.GeneratedCode;

namespace PokemonGo.RocketAPI.Console.Bot.Transfer
{
    public interface IPokemonTransferer
    {
        Task<TransferStatus> Transfer(PokemonData unwantedPokemons, float keepPerfectPokemonLimit = 80.0f);
    }
    
}