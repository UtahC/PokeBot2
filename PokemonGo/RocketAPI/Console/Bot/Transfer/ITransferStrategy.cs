using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.GeneratedCode;

namespace PokemonGo.RocketAPI.Console.Bot.Transfer
{
    public delegate void TransferEventHander(object sender, PokemonData pokemon);
    public interface ITransferStrategy
    {
        event TransferEventHander TransferIgnored;
        event TransferEventHander TransferSuccess;
        event TransferEventHander TransferFailed;
        Task Transfer();
    }
}
