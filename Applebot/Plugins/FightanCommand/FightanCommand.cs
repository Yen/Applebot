using ApplebotAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FightanCommand
{
    [PlatformRegistrar(typeof(TwitchPlatform.TwitchPlatform))]
    public class FightanCommand : Command
    {
        // list of Quality Fighting Games provided by @DudewitdaK
        string[] games = new string[] { "Last Bronx", "Galaxy Fight", "Waku Waku 7", "Psychic Force 2012", "Rakuga Kids", "Breakers Revenge", "Real Bout Fatal Fury 2", "Super Dragon Ball Z", "Bleach: Dark Souls", "Jump Ultimate Stars", "Castlevania Judgement", "NeoGeo Battle Coliseum", "SNK Gals Fighters", "Battle Monsters", "Fight 'N' Jokes", "X-Men Next Dimension", "The King of Fighters EX2: Howling Blood", "Fatal Fury Wild Ambition", "Cartoon Network Punch Time Explosion XL", "Digimon Battle Spirit 1.5", "Karnov's Revenge", "Fighting Layer", "Godzilla: Destroy All Monsters Melee", "The Last Blade 2", "Savage Reign", "Buriki One", "Samurai Shodown 64: Warrior's Rage", "Battle Fantasia", "Rage of the Dragons", "World Heroes 2", "Ninja Masters Haoh Ninpo Cho", "Martial Masters", "Slap Happy Rhythm Busters", "Fighters Destiny", "Battle Arena Toshinden 3", "Guilty Gear Petit", "Mace: The Dark Age", "Eternal Champions" };
        Random random = new Random();

        public FightanCommand() : base("FightanCommand")
        {
            Expressions.Add(new Regex("(?i)^!fightan\\b"));
        }

        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            platform.Send(new SendData(string.Format("Why don't you play a real fighting game, like {0}?", games[random.Next(0, games.Length)]), false, message));
        }
    }
}
