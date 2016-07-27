using System.Linq;
using System.Threading.Tasks;

namespace PokemonGo.RocketAPI.Console.Bot.Transfer
{
    public class AllTransferStrategy : BaseTransferStrategy, ITransferStrategy
    {
        public AllTransferStrategy(Client pokemonClient, IPokemonTransferer transferer) : base(pokemonClient, transferer)
        {
        }

        public async Task Transfer()
        {
            var inventory = await PokemonClient.GetInventory();
            var unwantedPokemon = inventory.InventoryDelta
                .InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                .Where(p => p?.PokemonId > 0)
                .ToArray();

            foreach (var pokemon in unwantedPokemon)
            {
                var transferResult = await _transferer.Transfer(pokemon);
                HandleEvent(pokemon, transferResult);
            }
        }
    }
}