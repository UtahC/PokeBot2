using System.Linq;
using System.Threading.Tasks;
using AllEnum;
using PokemonGo.RocketAPI.GeneratedCode;

namespace PokemonGo.RocketAPI.Console.Bot.Transfer
{
    public abstract class BaseTransferStrategy : BasePokemonClientJob
    {
        public event TransferEventHander TransferIgnored;
        public event TransferEventHander TransferSuccess;
        public event TransferEventHander TransferFailed;

        protected readonly IPokemonTransferer _transferer;
        protected BaseTransferStrategy(Client pokemonClient, IPokemonTransferer transferer) : base(pokemonClient)
        {
            _transferer = transferer;
        }

        protected void HandleEvent(PokemonData pokemon, TransferStatus status)
        {
            if (status == TransferStatus.Success)
            {
                OnTransferSuccess(pokemon);
            }
            else if (status == TransferStatus.Fail)
            {
                OnTransferFailed(pokemon);
            }
            else if(status == TransferStatus.Ignored)
            {
                OnTransferIgnored(pokemon);
            }
        }

        protected virtual void OnTransferIgnored(PokemonData pokemon)
        {
            TransferIgnored?.Invoke(this, pokemon);
        }

        protected virtual void OnTransferSuccess(PokemonData pokemon)
        {
            TransferSuccess?.Invoke(this, pokemon);
        }

        protected virtual void OnTransferFailed(PokemonData pokemon)
        {
            TransferFailed?.Invoke(this, pokemon);
        }
    }
    public class AllButStrongestTransferStrategy : BaseTransferStrategy, ITransferStrategy
    {
        private static readonly PokemonId[] UnwantedPokemonTypes = {
            PokemonId.Pidgey,
            PokemonId.Rattata,
            PokemonId.Weedle,
            PokemonId.Zubat,
            PokemonId.Caterpie,
            PokemonId.Pidgeotto,
            PokemonId.Paras,
            PokemonId.Venonat,
            PokemonId.Psyduck,
            PokemonId.Poliwag,
            PokemonId.Slowpoke,
            PokemonId.Drowzee,
            PokemonId.Gastly,
            PokemonId.Goldeen,
            PokemonId.Staryu,
            PokemonId.Magikarp,
            PokemonId.Clefairy,
            PokemonId.Eevee,
            PokemonId.Tentacool,
            PokemonId.Dratini,
            PokemonId.Ekans,
            PokemonId.Jynx,
            PokemonId.Lickitung,
            PokemonId.Spearow,
            PokemonId.NidoranFemale,
            PokemonId.NidoranMale
        };

        public AllButStrongestTransferStrategy(Client pokemonClient, IPokemonTransferer transferer) : base(pokemonClient, transferer)
        {
        }

        public async Task Transfer()
        {
            var inventory = await PokemonClient.GetInventory();
            var pokemons = inventory.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Pokemon)
                .Where(p => p != null && p?.PokemonId > 0)
                .ToArray();
            
            var orderedPokemonOfDesiredType = pokemons.Where(p => UnwantedPokemonTypes.Contains(p.PokemonId)).OrderByDescending(p => p.Cp);
            var unwantedPokemon = orderedPokemonOfDesiredType.Skip(1);

            foreach (var pokemon in unwantedPokemon)
            {
                var transferResult = await _transferer.Transfer(pokemon);
                HandleEvent(pokemon, transferResult);
            }
        }
    }
}