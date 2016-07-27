using System.Collections.Generic;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.GeneratedCode;

namespace PokemonGo.RocketAPI.Console.Bot.Evolution
{
    public delegate void EvolveEventHander(object sender, PokemonData pokemonIn, EvolvePokemonOut pokemonOut);
    public interface IEvolutionStrategy
    {
        event EvolveEventHander OnEvolve;
        event EvolveEventHander OnEvolveFail;
        Task Evolve();
    }
}
