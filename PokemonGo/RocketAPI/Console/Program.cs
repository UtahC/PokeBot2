#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AllEnum;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.GeneratedCode;
using System.Net.Http;
using System.Text;
using Google.Protobuf;
using PokemonGo.RocketAPI.Helpers;
using System.IO;
using System.Configuration;
using PokemonGo.RocketAPI.Console.Bot.Evolution;
using PokemonGo.RocketAPI.Console.Bot.Transfer;
using static System.String;

#endregion

namespace PokemonGo.RocketAPI.Console
{
    internal class Program
    {
        private static readonly ISettings ClientSettings = new Settings();
        private static Thread commandthread;
        static int Currentlevel = -1;
        private static int _totalExperience = 0;
        private static int TotalPokemon = 0;
        private static DateTime TimeStarted = DateTime.Now;
        public static DateTime InitSessionDateTime = DateTime.Now;
        public static double GetRuntime()
        {
            return ((DateTime.Now - TimeStarted).TotalSeconds) / 3600;
        }
        public static void ColoredConsoleWrite(ConsoleColor color, string text)
        {
            ConsoleColor originalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(text);
            System.Console.ForegroundColor = originalColor;
        }
        public static void ColoredConsoleWriteTimestamped(ConsoleColor color, string text)
        {
            ColoredConsoleWrite(color, $"[{DateTime.Now.ToString("HH:mm:ss")}] {text}");
        }

        public static string _getSessionRuntimeInTimeFormat()
        {
            return (DateTime.Now - InitSessionDateTime).ToString(@"dd\.hh\:mm\:ss");
        }


        private static ITransferStrategy SelectTransferStrategy(Client pokemonClient, IPokemonTransferer transferer, ISettings settings)
        {
            switch (ClientSettings.TransferType.ToLower())
            {
                case "cp":
                    return new CpTransferStrategy(pokemonClient, transferer, ClientSettings.TransferCPThreshold);
                case "duplicate":
                    return new DuplicatesTransferStrategy(pokemonClient, transferer);
                case "all":
                    return new AllTransferStrategy(pokemonClient, transferer);
                case "leaveStrongest":
                default:
                    return new AllButStrongestTransferStrategy(pokemonClient, transferer);
            }
        }

        private static async void Execute()
        {
            //Boostrap - Replace with IoC Container
            var client = new Client(ClientSettings);
            IPokemonTransferer transferer = new PokemonTransferer(client);
            IEvolutionStrategy evolutionStrategy = new BasicEvolutionStrategy(client);
            ITransferStrategy transferStrategy = SelectTransferStrategy(client, transferer, ClientSettings);
            
            //Hook Up Events...
            evolutionStrategy.OnEvolve += EvolutionStrategy_OnEvolve;
            evolutionStrategy.OnEvolveFail += EvolutionStrategy_OnEvolveFail;

            transferStrategy.TransferSuccess += TransferStrategy_TransferSuccess;
            transferStrategy.TransferFailed += TransferStrategy_TransferFailed;
            transferStrategy.TransferIgnored += TransferStrategy_TransferIgnored;

            try
            {
                if (ClientSettings.AuthType == AuthType.Ptc)
                    await client.DoPtcLogin(ClientSettings.PtcUsername, ClientSettings.PtcPassword);
                else if (ClientSettings.AuthType == AuthType.Google)
                    await client.DoGoogleLogin();

                await client.SetServer();


                //TODO: Clean this up
                await PrintBoostrap(client);
                await PrintBoostrap(client);


                //Step 1: Transfer any pokemon according to the users selected strategy
                await transferStrategy.Transfer();

                //Step 2: If we want to evolve, then apply to evolve strategy
                if (ClientSettings.EvolveAllGivenPokemons) await evolutionStrategy.Evolve();



                client.RecycleItems(client);

                await Task.Delay(5000);
                PrintLevel(client);
                await ExecuteFarmingPokestopsAndPokemons(client);
                ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["no_nearby_loc_found"]}");
                await Task.Delay(10000);
                Execute();
            }
            catch (TaskCanceledException tce) { ColoredConsoleWriteTimestamped(ConsoleColor.White, $"{Language.GetPhrases()["task_canceled_ex"]}"); Execute(); }
            catch (UriFormatException ufe) { ColoredConsoleWriteTimestamped(ConsoleColor.White, $"{Language.GetPhrases()["sys_uri_format_ex"]}"); Execute(); }
            catch (ArgumentOutOfRangeException aore) { ColoredConsoleWriteTimestamped(ConsoleColor.White, $"{Language.GetPhrases()["arg_out_of_range_ex"]}"); Execute(); }
            catch (ArgumentNullException ane) { ColoredConsoleWriteTimestamped(ConsoleColor.White, $"{Language.GetPhrases()["arg_null_ref"]}"); Execute(); }
            catch (NullReferenceException nre) { ColoredConsoleWriteTimestamped(ConsoleColor.White, $"{Language.GetPhrases()["null_ref"]}"); Execute(); }
            //await ExecuteCatchAllNearbyPokemons(client);
        }



        private static async Task PrintBoostrap(Client client)
        {
            var profile = await client.GetProfile();

            await ConsoleLevelTitle(profile.Profile.Username, client);
            ColoredConsoleWrite(ConsoleColor.Yellow, "----------------------------");
            ColoredConsoleWrite(ConsoleColor.Cyan,
                Language.GetPhrases()["account"].Replace("[username]", ClientSettings.PtcUsername));
            ColoredConsoleWrite(ConsoleColor.Cyan,
                Language.GetPhrases()["password"].Replace("[password]", ClientSettings.PtcPassword + "\n"));
            ColoredConsoleWrite(ConsoleColor.DarkGray,
                Language.GetPhrases()["latitude"].Replace("[latitude]", Convert.ToString(ClientSettings.DefaultLatitude)));
            ColoredConsoleWrite(ConsoleColor.DarkGray,
                Language.GetPhrases()["longtitude"].Replace("[longtitude]", Convert.ToString(ClientSettings.DefaultLongitude)));
            ColoredConsoleWrite(ConsoleColor.Yellow, "----------------------------");
            ColoredConsoleWrite(ConsoleColor.DarkGray, Language.GetPhrases()["your_account"] + "\n");
            ColoredConsoleWrite(ConsoleColor.DarkGray,
                Language.GetPhrases()["username"].Replace("[username]", profile.Profile.Username));
            ColoredConsoleWrite(ConsoleColor.DarkGray,
                Language.GetPhrases()["team"].Replace("[team]", Convert.ToString(profile.Profile.Team)));
            ColoredConsoleWrite(ConsoleColor.DarkGray,
                Language.GetPhrases()["stardust"].Replace("[stardust]",
                    Convert.ToString(profile.Profile.Currency.ToArray()[1].Amount)));

            ColoredConsoleWrite(ConsoleColor.Cyan, "\n" + Language.GetPhrases()["farming_started"]);
            ColoredConsoleWrite(ConsoleColor.Yellow, "----------------------------");
        }

        #region Event Handers
        private static void EvolutionStrategy_OnEvolveFail(object sender, PokemonData pokemonIn, EvolvePokemonOut pokemonOut)
        {
            //TODO: Figure out when to actually show failure.
            //ColoredConsoleWriteTimestamped(ConsoleColor.White, $"Failed to evolve {pokemonIn.PokemonId}. EvolvePokemonOutProto.Result was {pokemonOut.Result}");
        }

        private static void EvolutionStrategy_OnEvolve(object sender, PokemonData pokemonIn, EvolvePokemonOut pokemonOut)
        {
            ColoredConsoleWriteTimestamped(ConsoleColor.Cyan, $"Evolved {pokemonIn.PokemonId} successfully for {pokemonOut.ExpAwarded}xp");

            _totalExperience += pokemonOut.ExpAwarded;
        }
        private static void TransferStrategy_TransferIgnored(object sender, PokemonData pokemon)
        {
            //TODO: Figure out when to actually show failure.
            //ColoredConsoleWriteTimestamped(ConsoleColor.White, $"Pokemon ignored... Todo updat this message");
        }

        private static void TransferStrategy_TransferFailed(object sender, PokemonData pokemon)
        {
            ColoredConsoleWriteTimestamped(ConsoleColor.Red, $"{Language.GetPhrases()["transferred_pokemon_failed"].Replace("[pokemon]", Language.GetPokemons()[Convert.ToString(pokemon.PokemonId)]).Replace("[cp]", Convert.ToString(pokemon.Cp))}");
        }

        private static void TransferStrategy_TransferSuccess(object sender, PokemonData pokemon)
        {
            ColoredConsoleWriteTimestamped(ConsoleColor.Magenta, $"{Language.GetPhrases()["transferred_pokemon"].Replace("[pokemon]", Language.GetPokemons()[Convert.ToString(pokemon.PokemonId)]).Replace("[cp]", Convert.ToString(pokemon.Cp))}");
        }
        #endregion  

        private static void CommandIOThread()
        {
            string input;
            while (true)
            {
                input = System.Console.ReadLine();
                if (input == "exit")
                {
                    commandthread.Abort();
                    System.Environment.Exit(1);
                }
            } 
        }

        private static async Task ExecuteCatchAllNearbyPokemons(Client client)
        {
            var mapObjects = await client.GetMapObjects();

            var pokemons = mapObjects.MapCells.SelectMany(i => i.CatchablePokemons);

            var inventory2 = await client.GetInventory();
            var pokemons2 = inventory2.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Pokemon)
                .Where(p => p != null && p?.PokemonId > 0)
                .ToArray();

            foreach (var pokemon in pokemons)
            {
                var update = await client.UpdatePlayerLocation(pokemon.Latitude, pokemon.Longitude);
                
                var encounterPokemonResponse = await client.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnpointId);
                var pokemonCP = encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp;
                CatchPokemonResponse caughtPokemonResponse;
                do
                {
                    caughtPokemonResponse =
                        await
                            client.CatchPokemon(pokemon.EncounterId, pokemon.SpawnpointId, pokemon.Latitude,
                                pokemon.Longitude, MiscEnums.Item.ITEM_POKE_BALL, pokemonCP);
                    ; //note: reverted from settings because this should not be part of settings but part of logic
                } while (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchMissed);
                string pokemonName = Language.GetPokemons()[Convert.ToString(pokemon.PokemonId)];

                if (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess)
                {
                    ColoredConsoleWrite(ConsoleColor.Green, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["caught_pokemon"].Replace("[pokemon]", pokemonName).Replace("[cp]", Convert.ToString(encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp))}");
                    TotalPokemon++;
                }
                else
                    ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["pokemon_got_away"].Replace("[pokemon]", pokemonName).Replace("[cp]", Convert.ToString(encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp))}");
/*
                if (ClientSettings.TransferType == "leaveStrongest")
                    await TransferAllButStrongestUnwantedPokemon(client);
                else if (ClientSettings.TransferType == "all")
                    await TransferAllGivenPokemons(client, pokemons2);
                else if (ClientSettings.TransferType == "duplicate")
                    await TransferDuplicatePokemon(client);
                else if (ClientSettings.TransferType == "cp")
                    await TransferAllWeakPokemon(client, ClientSettings.TransferCPThreshold);
                    */
                await Task.Delay(3000);
            }
        }

        private static async Task ExecuteFarmingPokestopsAndPokemons(Client client)
        {
            var mapObjects = await client.GetMapObjects();

            var pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts).Where(i => i.Type == FortType.Checkpoint && i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime());

            foreach (var pokeStop in pokeStops)
            {
                var update = await client.UpdatePlayerLocation(pokeStop.Latitude, pokeStop.Longitude);
                var fortInfo = await client.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                var fortSearch = await client.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                StringWriter PokeStopOutput = new StringWriter();
                PokeStopOutput.Write($"[{DateTime.Now.ToString("HH:mm:ss")}] ");
                if (fortInfo.Name != Empty)
                    PokeStopOutput.Write(Language.GetPhrases()["pokestop"].Replace("[pokestop]", fortInfo.Name));
                if (fortSearch.ExperienceAwarded != 0)
                    PokeStopOutput.Write($", {Language.GetPhrases()["xp"].Replace("[xp]", Convert.ToString(fortSearch.ExperienceAwarded))}");
                if (fortSearch.GemsAwarded != 0)
                    PokeStopOutput.Write($", {Language.GetPhrases()["gem"].Replace("[gem]", Convert.ToString(fortSearch.GemsAwarded))}");
                if (fortSearch.PokemonDataEgg != null)
                    PokeStopOutput.Write($", {Language.GetPhrases()["egg"].Replace("[egg]", Convert.ToString(fortSearch.PokemonDataEgg))}");
                if (GetFriendlyItemsString(fortSearch.ItemsAwarded) != Empty)
                    PokeStopOutput.Write($", {Language.GetPhrases()["item"].Replace("[item]", GetFriendlyItemsString(fortSearch.ItemsAwarded))}");
                ColoredConsoleWrite(ConsoleColor.Cyan, PokeStopOutput.ToString());

                if (fortSearch.ExperienceAwarded != 0)
                    _totalExperience += (fortSearch.ExperienceAwarded);
                await Task.Delay(15000);
                await ExecuteCatchAllNearbyPokemons(client);
            }
        }

        private static string GetFriendlyItemsString(IEnumerable<FortSearchResponse.Types.ItemAward> items)
        {
            var enumerable = items as IList<FortSearchResponse.Types.ItemAward> ?? items.ToList();

            if (!enumerable.Any())
                return Empty;

            return
                enumerable.GroupBy(i => i.ItemId)
                    .Select(kvp => new { ItemName = kvp.Key.ToString(), Amount = kvp.Sum(x => x.ItemCount) })
                    .Select(y => $"{y.Amount} x {y.ItemName}")
                    .Aggregate((a, b) => $"{a}, {b}");
        }

        private static void Main(string[] args)
        {
            try
            {
                commandthread = new Thread(CommandIOThread);
                commandthread.Start();
            }
            catch (Exception ex)
            {
                ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] Unhandled exception: \n{ex}");
                ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Press any key to exit the program...");
                System.Console.ReadKey();
                System.Environment.Exit(1);
            }

            try
            {
                Language.LoadLanguageFile(ClientSettings.Language);
            }
            catch (Exception ex)
            {
                ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] Something's wrong when loading language file: \n{ex}");
                try
                {
                    ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Using default en_us instead.");
                    Language.LoadLanguageFile("en_us");
                }
                catch
                {
                    ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] Something's wrong when loading default language file again: \n{ex}");
                    ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Please check if your language files are valid. Press any key to exit the program...");
                    System.Console.ReadKey();
                    System.Environment.Exit(1);
                }

            }
            Task.Run(() =>
            {
                try
                {
                    //ColoredConsoleWrite(ConsoleColor.White, "Coded by Ferox - edited by NecronomiconCoding");
                    //CheckVersion();
                    Execute();
                }
                catch (PtcOfflineException)
                {
                    ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["ptc_server_down"]}");
                }
                catch (Exception ex)
                {
                    ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["unhandled_ex"].Replace("[ex]", Convert.ToString(ex))}");
                }
            });
            //System.Console.ReadLine();
        }




        public static async Task PrintLevel(Client client)
        {
            var inventory = await client.GetInventory();
            var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PlayerStats).ToArray();
            foreach (var v in stats)
                if (v != null)
                {
                    int XpDiff = GetXpDiff(client, v.Level);
                    if (ClientSettings.LevelOutput == "time")
                        ColoredConsoleWriteTimestamped(ConsoleColor.Yellow, $"{Language.GetPhrases()["current_lv"]} {v.Level} ({(v.Experience - v.PrevLevelXp - XpDiff) + "/" + (v.NextLevelXp - v.PrevLevelXp - XpDiff)})");
                    else if (ClientSettings.LevelOutput == "levelup")
                        if (Currentlevel != v.Level)
                        {
                            Currentlevel = v.Level;
                            ColoredConsoleWriteTimestamped(ConsoleColor.Magenta, $"{Language.GetPhrases()["current_lv"]}: {v.Level}. {Language.GetPhrases()["rpt"]} " + (v.NextLevelXp - v.Experience));
                        }
                }

            await Task.Delay(ClientSettings.LevelTimeInterval * 1000);
            PrintLevel(client);
        }

        public static async Task ConsoleLevelTitle(string Username, Client client)
        {
            var inventory = await client.GetInventory();
            var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PlayerStats).ToArray();
            var profile = await client.GetProfile();
            foreach (var v in stats)
                if (v != null)
                {
                    int XpDiff = GetXpDiff(client, v.Level);
                    System.Console.Title = Format(Username + " | Level: {0:0} - ({1:0} / {2:0}) | Stardust: {3:0}", v.Level, (v.Experience - v.PrevLevelXp - XpDiff), (v.NextLevelXp - v.PrevLevelXp - XpDiff), profile.Profile.Currency.ToArray()[1].Amount) + " | XP/Hour: " + Math.Round(_totalExperience / GetRuntime()) + " | Pokemon/Hour: " + Math.Round(TotalPokemon / GetRuntime())+ " | Lat/Lng: " + Convert.ToString(client.getCurrentLat()) + "/" + Convert.ToString(client.getCurrentLng());
                }
            await Task.Delay(1000);
            ConsoleLevelTitle(Username, client);
        }

        public static int GetXpDiff(Client client, int Level)
        {
            switch (Level)
            {
                case 1:
                    return 0;
                case 2:
                    return 1000;
                case 3:
                    return 2000;
                case 4:
                    return 3000;
                case 5:
                    return 4000;
                case 6:
                    return 5000;
                case 7:
                    return 6000;
                case 8:
                    return 7000;
                case 9:
                    return 8000;
                case 10:
                    return 9000;
                case 11:
                    return 10000;
                case 12:
                    return 10000;
                case 13:
                    return 10000;
                case 14:
                    return 10000;
                case 15:
                    return 15000;
                case 16:
                    return 20000;
                case 17:
                    return 20000;
                case 18:
                    return 20000;
                case 19:
                    return 25000;
                case 20:
                    return 25000;
                case 21:
                    return 50000;
                case 22:
                    return 75000;
                case 23:
                    return 100000;
                case 24:
                    return 125000;
                case 25:
                    return 150000;
                case 26:
                    return 190000;
                case 27:
                    return 200000;
                case 28:
                    return 250000;
                case 29:
                    return 300000;
                case 30:
                    return 350000;
                case 31:
                    return 500000;
                case 32:
                    return 500000;
                case 33:
                    return 750000;
                case 34:
                    return 1000000;
                case 35:
                    return 1250000;
                case 36:
                    return 1500000;
                case 37:
                    return 2000000;
                case 38:
                    return 2500000;
                case 39:
                    return 1000000;
                case 40:
                    return 1000000;
            }
            return 0;
        }
    }
}
