using System.Linq;
using System.Threading.Tasks;

namespace PokemonGo.RocketAPI.Console.Bot.Transfer
{
    public class CpTransferStrategy : BaseTransferStrategy, ITransferStrategy
    {
        private readonly int _minCp;

        public CpTransferStrategy(Client pokemonClient, IPokemonTransferer transferer, int minCp) : base(pokemonClient, transferer)
        {
            _minCp = minCp;
        }

        public async Task Transfer()
        {
            var inventory = await PokemonClient.GetInventory();
            var unwantedPokemon = inventory.InventoryDelta
                .InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                .Where(p => p?.PokemonId > 0 && p.Cp >= _minCp)
                .ToArray();

            foreach (var pokemon in unwantedPokemon)
            {
                var transferResult = await _transferer.Transfer(pokemon);
                HandleEvent(pokemon, transferResult);
            }
        }
    }
}