# Simple Pokemon Go Bot. C#.
##Features:
- [x] Farm Pokestops
- [x] Catch Pokemon
- [x] Transfer Pokemon
- [x] Refresh token support. (for google auth)
- [x] Login with PTC
- [x] Login with Google
- [x] Hatch Eggs
- [x] Evolve Pokemon
- [x] Add awesome colorZ
- [ ] Simple GUI
- [ ] Incubate New Eggs
- [ ] Ability to set IVs percentage, say 85%, everything below will be transferred.
- [ ] Ability to set walking speed.
- [ ] Add delay when you catch pokemons
- [ ] Configurable Custom Pathing with GPX

##Screenshots:
![img](http://i.imgur.com/jQrAMOdl.png)

##Requested Features:
- [ ] Implement seperate farming modes:
-       a. XP Mode: visits pokestops only (might farm pokemons that may spawn near pokestops)
-       b. Stardust Mode: Walks/Scans a large radius from given coordinates for pokemons, keeps repeating.

# Files
- [x] token.txt | Contains: 1 Line with Refresh Token | Don't delete!
- [x] external.config | Contains: configuration

# Setup Guide
- Download and extract the newest release [here](https://github.com/shiftcodeYT/PokeBot2/releases/latest)
- Edit external.config:
- Change AuthType to "Google" or "Ptc"
- If using "Ptc", change username and password to your login
- Change DefaultLatitude and DefaultLongitude to the GPS coords of your liking
- Change language to "english" or "german"
- Change transfertype to "none"/"cp"/"leaveStrongest"/"duplicate"/"all"
- If using "cp", change TransferCPtreshold to your liking
- Change EvolveAllGivenPokemons to "true" or "false"
- Run the bot :)
- If you don't know how to do that, Double-click on the file called "PokemonGo.RocketAPI.Console.exe". If you don't see a file ending with .exe, Look for a file "PokemonGo.RocketAPI.Console" that says "Application" under Type.
