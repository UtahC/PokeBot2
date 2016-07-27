using System.Linq;
using System.Threading.Tasks;

namespace PokemonGo.RocketAPI.Console.Bot.Transfer
{
    public class DuplicatesTransferStrategy : BaseTransferStrategy, ITransferStrategy
    {
        public DuplicatesTransferStrategy(Client pokemonClient, IPokemonTransferer transferer) : base(pokemonClient, transferer)
        {
        }

        public async Task Transfer()
        {
            var inventory = await PokemonClient.GetInventory();
            var candidatePokemon = inventory.InventoryDelta
                .InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                .Where(p => p?.PokemonId > 0);


            var unwantedPokemon = candidatePokemon.GroupBy(p => p.PokemonId)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.Skip(1));

            foreach (var pokemon in unwantedPokemon)
            {
                var transferResult = await _transferer.Transfer(pokemon);
                HandleEvent(pokemon, transferResult);
            }
        }
    }
}