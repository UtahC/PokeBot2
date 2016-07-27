using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AllEnum;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.GeneratedCode;
using System.Threading;
using System.Reflection;

namespace PokemonGo.RocketAPI.Window
{
    public partial class MainForm : Form
    {
        Dictionary<string, string> _indivConsoleText = new Dictionary<string, string>();
        string _totalConsoleText = "";

        public MainForm()
        {
            InitializeComponent();
            //BotConfig.Instance.Initialize();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
            
            IEnumerable<string> refreshTokens = File.ReadLines(@AppDomain.CurrentDomain.BaseDirectory + @"\token.txt");
            Thread.CurrentThread.Name = "-All-";
            comboBox1.Items.Add("-All-");
            comboBox1.SelectedIndex = 0;

            foreach (var loginString in refreshTokens)
            {
                Task.Run(() =>
                {
                    try
                    {
                        Execute(loginString);
                    }
                    catch (PtcOfflineException)
                    {
                        ColoredConsoleWrite(Color.Red, "PTC Servers are probably down OR your credentials are wrong. Try google", "-All-");
                    }
                    catch (Exception ex)
                    {
                        ColoredConsoleWrite(Color.Red, $"Unhandled exception: {ex}", "-All-");
                    }
                });
            }
        }

        public void ColoredConsoleWrite(Color color, string value, string userName)
        {

            if (InvokeRequired)
            {
                this.Invoke(new Action<Color, string, string>(ColoredConsoleWrite), new object[] { color, value, userName});
                return;
            }
            //string textToAppend = Thread.CurrentThread.Name == "main" ? string.Format($"[{Thread.CurrentThread.Name}] ") : "";
            string textToAppend = string.Format("[{0}] {1}\r\n", DateTime.Now.ToString("HH:mm:ss"), value);
            if (userName == comboBox1.SelectedItem.ToString() || comboBox1.SelectedItem.ToString() == "-All-")
            { 
                logTextBox.SelectionColor = color;
                if (comboBox1.SelectedItem.ToString() == "-All-")
                    logTextBox.AppendText(string.Format("[{0}] {1}", userName, textToAppend));
                else
                    logTextBox.AppendText(textToAppend);
            }
            if (userName != "-All-" && userName != null)
                _indivConsoleText[userName] += textToAppend;
            _totalConsoleText += string.Format("[{0}] {1}", userName, textToAppend);
        }

        private static readonly ISettings ClientSettings = new Settings();
        static int Currentlevel = -1;
        private static int TotalExperience = 0;
        private static int TotalPokemon = 0;
        private static DateTime TimeStarted = DateTime.Now;
        public static DateTime InitSessionDateTime = DateTime.Now;
        private Stopwatch stopwatch;

        public static double GetRuntime()
        {
            return ((DateTime.Now - TimeStarted).TotalSeconds) / 3600;
        }

        public static string _getSessionRuntimeInTimeFormat()
        {
            return (DateTime.Now - InitSessionDateTime).ToString(@"dd\.hh\:mm\:ss");
        }

        //public static void ColoredConsoleWrite(ConsoleColor color, string text)
        //{
        //    ConsoleColor originalColor = System.Console.ForegroundColor;
        //    System.Console.ForegroundColor = color;
        //    System.Console.WriteLine(text);
        //    System.Console.ForegroundColor = originalColor;
        //}

        private async Task EvolveAllGivenPokemons(Client client, IEnumerable<PokemonData> pokemonToEvolve)
        {
            foreach (var pokemon in pokemonToEvolve)
            {
                /*
                enum Holoholo.Rpc.Types.EvolvePokemonOutProto.Result {
	                UNSET = 0;
	                SUCCESS = 1;
	                FAILED_POKEMON_MISSING = 2;
	                FAILED_INSUFFICIENT_RESOURCES = 3;
	                FAILED_POKEMON_CANNOT_EVOLVE = 4;
	                FAILED_POKEMON_IS_DEPLOYED = 5;
                }
                }*/
                if (pokemon.PokemonId != PokemonId.Eevee &&
                    pokemon.PokemonId != PokemonId.Caterpie &&
                    pokemon.PokemonId != PokemonId.Slowpoke &&
                    pokemon.PokemonId != PokemonId.Spearow &&
                    pokemon.PokemonId != PokemonId.Zubat &&
                    pokemon.PokemonId != PokemonId.Pidgeot &&
                    pokemon.PokemonId != PokemonId.Rattata &&
                    pokemon.PokemonId != PokemonId.Jigglypuff &&
                    pokemon.PokemonId != PokemonId.Weedle &&
                    pokemon.PokemonId != PokemonId.Oddish &&
                    pokemon.PokemonId != PokemonId.Venonat &&
                    pokemon.PokemonId != PokemonId.Magikarp &&
                    pokemon.PokemonId != PokemonId.NidoranMale &&
                    pokemon.PokemonId != PokemonId.NidoranFemale &&
                    pokemon.PokemonId != PokemonId.Bellsprout)
                    continue;
                var countOfEvolvedUnits = 0;
                var xpCount = 0;

                EvolvePokemonOut evolvePokemonOutProto;
                do
                {
                    evolvePokemonOutProto = await client.EvolvePokemon(pokemon.Id);
                    //todo: someone check whether this still works

                    if (evolvePokemonOutProto.Result == 1)
                    {
                        ColoredConsoleWrite(Color.Cyan,
                            $"Evolved {pokemon.PokemonId} successfully for {evolvePokemonOutProto.ExpAwarded}xp", client.userName);

                        countOfEvolvedUnits++;
                        xpCount += evolvePokemonOutProto.ExpAwarded;
                    }
                    else
                    {
                        var result = evolvePokemonOutProto.Result;
                        /*
                        ColoredConsoleWrite(ConsoleColor.White, $"Failed to evolve {pokemon.PokemonId}. " +
                                                 $"EvolvePokemonOutProto.Result was {result}");

                        ColoredConsoleWrite(ConsoleColor.White, $"Due to above error, stopping evolving {pokemon.PokemonId}");
                        */
                    }
                } while (evolvePokemonOutProto.Result == 1);
                if (countOfEvolvedUnits > 0)
                    ColoredConsoleWrite(Color.Cyan,
                        $"Evolved {countOfEvolvedUnits} pieces of {pokemon.PokemonId} for {xpCount}xp", client.userName);

                await Task.Delay(3000);
            }
        }

        private delegate void SetControlThreadSafeDelegate(
            Control control,
            string propertyName,
            object propertyValue);

        public static void SetControlThreadSafe(
            Control control,
            string methodName,
            object propertyValue)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new SetControlThreadSafeDelegate
                (SetControlThreadSafe),
                new object[] { control, methodName, propertyValue });
            }
            else
            {
                control.GetType().InvokeMember(
                    methodName,
                    BindingFlags.InvokeMethod,
                    null,
                    control,
                    new object[] { propertyValue });
            }
        }

        private async void Execute(string loginString = "")
        {
            
            Client client = null;
            try
            {
                if (loginString.Contains(','))
                {
                    var ptcUsername = loginString.Substring(loginString.IndexOf(':') + 1, loginString.IndexOf(',') - loginString.IndexOf(':') - 1);
                    var ptcPassword = loginString.Substring(loginString.IndexOf(',') + 1);
                    client = new Client(ClientSettings);
                    await client.DoPtcLogin(ptcUsername, ptcPassword);
                }
                else
                {
                    client = new Client(ClientSettings, loginString.Substring(loginString.IndexOf(':') + 1));
                    await client.DoGoogleLogin();
                }

                await client.SetServer();
                var profile = await client.GetProfile();
                var settings = await client.GetSettings();
                var mapObjects = await client.GetMapObjects();
                var inventory = await client.GetInventory();
                var pokemons =
                    inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                        .Where(p => p != null && p?.PokemonId > 0);

                var userName = profile.Profile.Username;
                Thread.CurrentThread.Name = userName;
                if (!_indivConsoleText.ContainsKey(userName))
                {
                    _indivConsoleText.Add(userName, "");
                    comboBox1.Invoke((MethodInvoker)(() =>
                    {
                        comboBox1.Items.Add(userName);
                    }));
                }

                ColoredConsoleWrite(Color.Yellow, "----------------------------", client.userName);
                ColoredConsoleWrite(Color.Cyan, "Account: " + ClientSettings.PtcUsername, client.userName);
                ColoredConsoleWrite(Color.Cyan, "Password: " + ClientSettings.PtcPassword + "\n", client.userName);
                ColoredConsoleWrite(Color.DarkGray, "Latitude: " + ClientSettings.DefaultLatitude, client.userName);
                ColoredConsoleWrite(Color.DarkGray, "Longitude: " + ClientSettings.DefaultLongitude, client.userName);
                ColoredConsoleWrite(Color.Yellow, "----------------------------", client.userName);
                ColoredConsoleWrite(Color.DarkGray, "Your Account:\n", client.userName);
                ColoredConsoleWrite(Color.DarkGray, "Name: " + profile.Profile.Username, client.userName);
                ColoredConsoleWrite(Color.DarkGray, "Team: " + profile.Profile.Team, client.userName);
                ColoredConsoleWrite(Color.DarkGray, "Stardust: " + profile.Profile.Currency.ToArray()[1].Amount, client.userName);

                ColoredConsoleWrite(Color.Cyan, "\nFarming Started", client.userName);
                ColoredConsoleWrite(Color.Yellow, "----------------------------", client.userName);
                if (ClientSettings.TransferType == "leaveStrongest")
                    await TransferAllButStrongestUnwantedPokemon(client);
                else if (ClientSettings.TransferType == "all")
                    await TransferAllGivenPokemons(client, pokemons);
                else if (ClientSettings.TransferType == "duplicate")
                    await TransferDuplicatePokemon(client);
                else if (ClientSettings.TransferType == "cp")
                    await TransferAllWeakPokemon(client, ClientSettings.TransferCPThreshold);
                else
                    ColoredConsoleWrite(Color.DarkGray, $"Transfering pokemon disabled", client.userName);
                if (ClientSettings.EvolveAllGivenPokemons)
                    await EvolveAllGivenPokemons(client, pokemons);

                client.RecycleItems(client);

                await Task.Delay(5000);
                PrintLevel(client);
                ShowData(client);
                //ConsoleLevelTitle(profile.Profile.Username, client);
                await ExecuteFarmingPokestopsAndPokemons(client);
                ColoredConsoleWrite(Color.Red, $"No nearby usefull locations found. Please wait 10 seconds.", client.userName);
                await Task.Delay(10000);
                Execute(loginString);
            }
            catch (TaskCanceledException tce) { ColoredConsoleWrite(Color.White, "Task Canceled Exception - Restarting", client.userName); Execute(loginString); }
            catch (UriFormatException ufe) { ColoredConsoleWrite(Color.White, "System URI Format Exception - Restarting", client.userName); Execute(loginString); }
            catch (ArgumentOutOfRangeException aore) { ColoredConsoleWrite(Color.White, "ArgumentOutOfRangeException - Restarting", client.userName); Execute(loginString); }
            catch (ArgumentNullException ane) { ColoredConsoleWrite(Color.White, "Argument Null Refference - Restarting", client.userName); Execute(loginString); }
            catch (NullReferenceException nre) { ColoredConsoleWrite(Color.White, "Null Refference - Restarting", client.userName); Execute(loginString); }
            //await ExecuteCatchAllNearbyPokemons(client);
        }

        private async Task ExecuteCatchAllNearbyPokemons(Client client)
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
                string pokemonName;
                if (ClientSettings.Language == "german")
                {
                    string name_english = Convert.ToString(pokemon.PokemonId);
                    var request = (HttpWebRequest)WebRequest.Create("http://boosting-service.de/pokemon/index.php?pokeName=" + name_english);
                    var response = (HttpWebResponse)request.GetResponse();
                    pokemonName = new StreamReader(response.GetResponseStream()).ReadToEnd();
                }
                else
                    pokemonName = Convert.ToString(pokemon.PokemonId);
                if (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess)
                {
                    ColoredConsoleWrite(Color.Green, $"We caught a {pokemonName} with {encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp} CP", client.userName);
                }
                else
                    ColoredConsoleWrite(Color.Red, $"{pokemonName} with {encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp} CP got away..", client.userName);

                if (ClientSettings.TransferType == "leaveStrongest")
                    await TransferAllButStrongestUnwantedPokemon(client);
                else if (ClientSettings.TransferType == "all")
                    await TransferAllGivenPokemons(client, pokemons2);
                else if (ClientSettings.TransferType == "duplicate")
                    await TransferDuplicatePokemon(client);
                else if (ClientSettings.TransferType == "cp")
                    await TransferAllWeakPokemon(client, ClientSettings.TransferCPThreshold);

                await Task.Delay(3000);
            }
        }

        private async Task ExecuteFarmingPokestopsAndPokemons(Client client)
        {
            var mapObjects = await client.GetMapObjects();

            var pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts).Where(i => i.Type == FortType.Checkpoint && i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime());

            foreach (var pokeStop in pokeStops)
            {
                var update = await client.UpdatePlayerLocation(pokeStop.Latitude, pokeStop.Longitude);
                var fortInfo = await client.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                var fortSearch = await client.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                StringWriter PokeStopOutput = new StringWriter();
                PokeStopOutput.Write($"");
                if (fortInfo.Name != string.Empty)
                    PokeStopOutput.Write("PokeStop: " + fortInfo.Name);
                if (fortSearch.ExperienceAwarded != 0)
                    PokeStopOutput.Write($", XP: {fortSearch.ExperienceAwarded}");
                if (fortSearch.GemsAwarded != 0)
                    PokeStopOutput.Write($", Gems: {fortSearch.GemsAwarded}");
                if (fortSearch.PokemonDataEgg != null)
                    PokeStopOutput.Write($", Eggs: {fortSearch.PokemonDataEgg}");
                if (GetFriendlyItemsString(fortSearch.ItemsAwarded) != string.Empty)
                    PokeStopOutput.Write($", Items: {GetFriendlyItemsString(fortSearch.ItemsAwarded)} ");
                ColoredConsoleWrite(Color.Cyan, PokeStopOutput.ToString(), client.userName);

                if (fortSearch.ExperienceAwarded != 0)
                    TotalExperience += (fortSearch.ExperienceAwarded);
                await Task.Delay(15000);
                await ExecuteCatchAllNearbyPokemons(client);
            }
        }

        private string GetFriendlyItemsString(IEnumerable<FortSearchResponse.Types.ItemAward> items)
        {
            var enumerable = items as IList<FortSearchResponse.Types.ItemAward> ?? items.ToList();

            if (!enumerable.Any())
                return string.Empty;

            return
                enumerable.GroupBy(i => i.ItemId)
                    .Select(kvp => new { ItemName = kvp.Key.ToString(), Amount = kvp.Sum(x => x.ItemCount) })
                    .Select(y => $"{y.Amount} x {y.ItemName}")
                    .Aggregate((a, b) => $"{a}, {b}");
        }

        private async Task TransferAllButStrongestUnwantedPokemon(Client client)
        {
            //ColoredConsoleWrite(ConsoleColor.White, $"Firing up the meat grinder");

            var unwantedPokemonTypes = new[]
            {
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

            var inventory = await client.GetInventory();
            var pokemons = inventory.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Pokemon)
                .Where(p => p != null && p?.PokemonId > 0)
                .ToArray();

            foreach (var unwantedPokemonType in unwantedPokemonTypes)
            {
                var pokemonOfDesiredType = pokemons.Where(p => p.PokemonId == unwantedPokemonType)
                    .OrderByDescending(p => p.Cp)
                    .ToList();

                var unwantedPokemon =
                    pokemonOfDesiredType.Skip(1) // keep the strongest one for potential battle-evolving
                        .ToList();

                //ColoredConsoleWrite(ConsoleColor.White, $"Grinding {unwantedPokemon.Count} pokemons of type {unwantedPokemonType}");
                await TransferAllGivenPokemons(client, unwantedPokemon);
            }

            //ColoredConsoleWrite(ConsoleColor.White, $"Finished grinding all the meat");
        }

        public static float Perfect(PokemonData poke)
        {
            return ((float)(poke.IndividualAttack + poke.IndividualDefense + poke.IndividualStamina) / (3.0f * 15.0f)) * 100.0f;
        }

        private async Task TransferAllGivenPokemons(Client client, IEnumerable<PokemonData> unwantedPokemons, float keepPerfectPokemonLimit = 80.0f)
        {
            foreach (var pokemon in unwantedPokemons)
            {
                if (Perfect(pokemon) >= keepPerfectPokemonLimit) continue;
                ColoredConsoleWrite(Color.White, $"Pokemon {pokemon.PokemonId} with {pokemon.Cp} CP has IV percent less than {keepPerfectPokemonLimit}%", client.userName);

                if (pokemon.Favorite == 0)
                {
                    var transferPokemonResponse = await client.TransferPokemon(pokemon.Id);

                    /*
                    ReleasePokemonOutProto.Status {
                        UNSET = 0;
                        SUCCESS = 1;
                        POKEMON_DEPLOYED = 2;
                        FAILED = 3;
                        ERROR_POKEMON_IS_EGG = 4;
                    }*/

                    if (transferPokemonResponse.Status == 1)
                    {
                        ColoredConsoleWrite(Color.Magenta, $"Transferred {pokemon.PokemonId} with {pokemon.Cp} CP", client.userName);
                    }
                    else
                    {
                        var status = transferPokemonResponse.Status;

                        ColoredConsoleWrite(Color.Red, $"Somehow failed to transfer {pokemon.PokemonId} with {pokemon.Cp} CP. " +
                                                 $"ReleasePokemonOutProto.Status was {status}", client.userName);
                    }

                    await Task.Delay(3000);
                }
            }
        }

        private async Task TransferDuplicatePokemon(Client client)
        {

            //ColoredConsoleWrite(ConsoleColor.White, $"Check for duplicates");
            var inventory = await client.GetInventory();
            var allpokemons =
                inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                    .Where(p => p != null && p?.PokemonId > 0);

            var dupes = allpokemons.OrderBy(x => x.Cp).Select((x, i) => new { index = i, value = x })
                .GroupBy(x => x.value.PokemonId)
                .Where(x => x.Skip(1).Any());

            for (var i = 0; i < dupes.Count(); i++)
            {
                for (var j = 0; j < dupes.ElementAt(i).Count() - 1; j++)
                {
                    var dubpokemon = dupes.ElementAt(i).ElementAt(j).value;
                    if (dubpokemon.Favorite == 0)
                    {
                        var transfer = await client.TransferPokemon(dubpokemon.Id);
                        ColoredConsoleWrite(Color.DarkGreen,
                            $"Transferred {dubpokemon.PokemonId} with {dubpokemon.Cp} CP (Highest is {dupes.ElementAt(i).Last().value.Cp})", client.userName);

                    }
                }
            }
        }

        private async Task TransferAllWeakPokemon(Client client, int cpThreshold)
        {
            //ColoredConsoleWrite(ConsoleColor.White, $"Firing up the meat grinder");

            var doNotTransfer = new[] //these will not be transferred even when below the CP threshold
            {
                //PokemonId.Pidgey,
                //PokemonId.Rattata,
                //PokemonId.Weedle,
                //PokemonId.Zubat,
                //PokemonId.Caterpie,
                //PokemonId.Pidgeotto,
                //PokemonId.NidoranFemale,
                //PokemonId.Paras,
                //PokemonId.Venonat,
                //PokemonId.Psyduck,
                //PokemonId.Poliwag,
                //PokemonId.Slowpoke,
                //PokemonId.Drowzee,
                //PokemonId.Gastly,
                //PokemonId.Goldeen,
                //PokemonId.Staryu,
                PokemonId.Magikarp,
                PokemonId.Eevee,
                //PokemonId.Dratini
            };

            var inventory = await client.GetInventory();
            var pokemons = inventory.InventoryDelta.InventoryItems
                                .Select(i => i.InventoryItemData?.Pokemon)
                                .Where(p => p != null && p?.PokemonId > 0)
                                .ToArray();

            //foreach (var unwantedPokemonType in unwantedPokemonTypes)
            {
                var pokemonToDiscard = pokemons.Where(p => !doNotTransfer.Contains(p.PokemonId) && p.Cp < cpThreshold)
                                                   .OrderByDescending(p => p.Cp)
                                                   .ToList();

                //var unwantedPokemon = pokemonOfDesiredType.Skip(1) // keep the strongest one for potential battle-evolving
                //                                          .ToList();
                ColoredConsoleWrite(Color.Gray, $"Grinding {pokemonToDiscard.Count} pokemon below {cpThreshold} CP.", client.userName);
                await TransferAllGivenPokemons(client, pokemonToDiscard);

            }

            ColoredConsoleWrite(Color.Gray, $"Finished grinding all the meat", client.userName);
        }

        public async Task PrintLevel(Client client)
        {
            var inventory = await client.GetInventory();
            var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PlayerStats).ToArray();
            foreach (var v in stats)
                if (v != null)
                {
                    int XpDiff = GetXpDiff(client, v.Level);
                    if (ClientSettings.LevelOutput == "time")
                        ColoredConsoleWrite(Color.Yellow, $"Current Level: " + v.Level + " (" + (v.Experience - v.PrevLevelXp - XpDiff) + "/" + (v.NextLevelXp - v.PrevLevelXp - XpDiff) + ")", client.userName);
                    else if (ClientSettings.LevelOutput == "levelup")
                        if (Currentlevel != v.Level)
                        {
                            Currentlevel = v.Level;
                            ColoredConsoleWrite(Color.Magenta, $"Current Level: " + v.Level + ". XP needed for next Level: " + (v.NextLevelXp - v.Experience), client.userName);
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
                    System.Console.Title = string.Format(Username + " | Level: {0:0} - ({1:0} / {2:0}) | Stardust: {3:0}", v.Level, (v.Experience - v.PrevLevelXp - XpDiff), (v.NextLevelXp - v.PrevLevelXp - XpDiff), profile.Profile.Currency.ToArray()[1].Amount) + " | XP/Hour: " + Math.Round(TotalExperience / GetRuntime()) + " | Pokemon/Hour: " + Math.Round(TotalPokemon / GetRuntime());
                }
            await Task.Delay(1000);
            ConsoleLevelTitle(Username, client);
        }

        public async Task ShowData(Client client)
        {
            string selected = "";
            comboBox1.Invoke((MethodInvoker)(() => { selected = comboBox1.SelectedItem.ToString(); }));
            if (selected == client.userName)
            {
                var inventory = await client.GetInventory();
                var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PlayerStats).ToArray();
                var profile = await client.GetProfile();
                foreach (var v in stats)
                {
                    if (v != null)
                    {
                        int XpDiff = GetXpDiff(client, v.Level);
                        textBox1.Invoke((MethodInvoker)(() => { textBox1.Text = client.userName; }));
                        textBox2.Invoke((MethodInvoker)(() => { textBox2.Text = v.Level.ToString(); }));
                        textBox3.Invoke((MethodInvoker)(() => { textBox3.Text = string.Format($"{(v.Experience - v.PrevLevelXp - XpDiff)}/{ (v.NextLevelXp - v.PrevLevelXp - XpDiff)}"); }));
                        textBox4.Invoke((MethodInvoker)(() => { textBox4.Text = profile.Profile.Currency.ToArray()[1].Amount.ToString(); }));
                        textBox5.Invoke((MethodInvoker)(() => { textBox5.Text = client.getCurrentLat().ToString(); }));
                        textBox6.Invoke((MethodInvoker)(() => { textBox6.Text = client.getCurrentLng().ToString(); }));
                        textBox7.Invoke((MethodInvoker)(() => { textBox7.Text = Math.Round(TotalExperience / GetRuntime()).ToString(); }));
                    }
                }
            }
            await Task.Delay(1000);
            ShowData(client);
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

        private void logTextBox_TextChanged(object sender, EventArgs e)
        {
            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.ScrollToCaret();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem.ToString() == "-All-")
                logTextBox.Text = _totalConsoleText;
            else
                logTextBox.Text = _indivConsoleText[comboBox1.SelectedItem.ToString()];

        }
    }
}
