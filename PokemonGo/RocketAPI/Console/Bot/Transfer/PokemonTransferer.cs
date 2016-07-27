using System.Threading.Tasks;
using PokemonGo.RocketAPI.GeneratedCode;

namespace PokemonGo.RocketAPI.Console.Bot.Transfer
{
    public class PokemonTransferer : BasePokemonClientJob, IPokemonTransferer
    {
        public async Task<TransferStatus> Transfer(PokemonData pokemon, float keepPerfectPokemonLimit = 80.0f)
        {
            if (Perfect(pokemon) >= keepPerfectPokemonLimit || pokemon.Favorite == 0) return TransferStatus.Ignored;

            var transferPokemonResponse = await PokemonClient.TransferPokemon(pokemon.Id);

            await Task.Delay(3000);
            if (transferPokemonResponse.Status == 1)
            {
                return TransferStatus.Success;
            }

            return TransferStatus.Fail;
        }

        private static float Perfect(PokemonData poke)
        {
            return ((float)(poke.IndividualAttack + poke.IndividualDefense + poke.IndividualStamina) / (3.0f * 15.0f)) * 100.0f;
        }
        
        public PokemonTransferer(Client pokemonClient) : base(pokemonClient)
        {
        }
    }
}