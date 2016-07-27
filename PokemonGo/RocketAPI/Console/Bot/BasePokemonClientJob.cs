namespace PokemonGo.RocketAPI.Console.Bot
{
    public abstract class BasePokemonClientJob
    {
        protected Client PokemonClient { get; set; }

        protected BasePokemonClientJob(Client pokemonClient)
        {
            PokemonClient = pokemonClient;
        }
    }
}