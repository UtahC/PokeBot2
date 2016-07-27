using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.GeneratedCode;

namespace PokemonGo.RocketAPI.Console.Bot.Evolution
{
    public class BasicEvolutionStrategy : BasePokemonClientJob, IEvolutionStrategy
    {
        public BasicEvolutionStrategy(Client pokemonClient) : base(pokemonClient)
        {
        }
        
        public event EvolveEventHander OnEvolve;
        public event EvolveEventHander OnEvolveFail;

        public async Task Evolve()
        {
            var inventory = await PokemonClient.GetInventory();
            var pokemonToEvolve = inventory.InventoryDelta
                .InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                .Where(p => p?.PokemonId > 0)
                .ToArray();

            foreach (var pokemon in pokemonToEvolve)
            {
                EvolvePokemonOut evolvePokemonOutProto;
                do
                {
                    evolvePokemonOutProto = await PokemonClient.EvolvePokemon(pokemon.Id);

                    if (evolvePokemonOutProto.Result == 1)
                    {
                        OnEvolve?.Invoke(this, pokemon, evolvePokemonOutProto);

                        //Only delay on success?
                        await Task.Delay(3000);
                    }
                    else
                    {
                        OnEvolveFail?.Invoke(this, pokemon, evolvePokemonOutProto);
                    }
                } while (evolvePokemonOutProto.Result == 1);
                
            }
        }
    }
    
}
