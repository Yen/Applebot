using ApplebotAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JohnCommand
{
    public class JohnCommand : Command
    {

        // directly stolen from /u/Poyoarya http://poyoarya.github.io/john.html

        string[] johnPrefix = new string[] { "I lost because", "I only lost because", "They won because", "They only won because" };

        string[] johnSubject = new string[] {
    "my controller is",
    "the sun was",
    "my hands were",
    "everyone was",
    "the TV is",
    "the crowd was",
    "my opponent was",
    "my chair is",
    "their controller is",
    "Meta Knight is",
    "Roy's wavedash is",
    "my mother is",
    "my brain is",
    "my 3DS is",
    "Nintendo was",
    "my Twitter followers were",
    "my eyes are",
    "the DLC is",
    "the commentators are",
    "the music was",
    "Final Destination is",
    "Reggie Fils-Aimé is",
    "the venue is",
    "my skills were",
    "the stream was",
    "Sakurai was",
    "Brawl is",
    "Melee is",
    "reddit is",
    "the ledge was",
    "my foot is",
    "the C-stick was",
    "my analogue stick is",
    "Project M is",
    "your shoes are",
    "the hotel is",
    "my Playstation is",
    "my mother's basement is",
    "the USA is",
    "my IQ is",
    "the posts on Miiverse are",
    "tap jump was",
    "all 1540 matchups are",
    "Jigglypuff was",
    "I was",
    "PAC-MAN was",
    "Alex Strife is",
    "my scarf is"
        };

        string[] johnProblem = new string[] {
    "in my eyes",
    "broken",
    "laggy",
    "hacked",
    "too loud",
    "uncomfortable",
    "OP",
    "fraudulent",
    "disturbing me",
    "making me SD",
    "upside-down",
    "violating the rules",
    "too smelly for me",
    "totally spooking me out",
    "unnecessarily rude",
    "making funny faces at me",
    "trash-talking mid-match",
    "running a company for 16 hours a day",
    "making excuses",
    "spamming projectiles",
    "not fair",
    "way better than my character",
    "speaking Japanese",
    "too bright",
    "nerfed",
    "garbage",
    "not good enough",
    "too small",
    "too big",
    "on a bad day",
    "sandbagging",
    "using custom moves",
    "reminding me of my ex",
    "really annoying",
    "tired",
    "a big gimmick",
    "kinda sweaty",
    "drunk",
    "only using one move",
    "not listening to me",
    "sleeping",
    "cheap",
    "terrible for my character",
    "rated a 7.8 on IGN",
    "using infinite combos",
    "taunting",
    "cheating",
    "different because of the update",
    "using glitches",
    "too hard to reach",
    "a timed match",
    "using better moves than me",
    "using motion controls",
    "sitting slightly closer to the screen",
    "not wearing their glasses",
    "sober",
    "using items",
    "not letting me grab them",
    "shielding too much",
    "air dodging",
    "rolling",
    "pausing mid-match",
    "saving replays",
    "ethically superior to me",
    "only using the A button",
    "only using the B button",
    "Reggie Fils-Aimé",
    "picking stages that I don't like",
    "bad and should feel bad",
    "low on batteries",
    "cold",
    "sticky",
    "blocking the screen",
    "walking in front of the screen",
    "tangling my controller cable",
    "incapable of melting steel beams",
    "too attractive",
    "too fast",
    "using an ugly alternative costume",
    "using counters too much",
    "spamming PK Fire",
    "my b",
    "really hard to remember"
        };

        string[] elsSubject = new string[] {
    "my keyboard is",
    "the sun was",
    "my hands were",
    "everyone was",
    "my monitor is",
    "Twitch chat was",
    "my opponent was",
    "my chair is",
    "their keyboard is",
    "Yama Raja is",
    "Guard is",
    "my mother is",
    "my brain is",
    "my computer is",
    "KOG was",
    "Inspire is",
    "my eyes are",
    "Syndicate is",
    "the commentators are",
    "the music was",
    "Elrios Bay is",
    "Safety was",
    "the stage is",
    "my skills were",
    "Fendo's ego is",
    "Willb was",
    "Sword Shield is",
    "Entangle is",
    "the forums are",
    "the platform was",
    "my foot is",
    "my gear was",
    "Victal is",
    "the ready button is",
    "the connection is",
    "the sparring room is",
    "the server is",
    "Cargo Airship is",
    "KR is",
    "my FPS is",
    "Customer Service is",
    "Blessed Recovery was",
    "the setplay was",
    "Wind Sneaker is",
    "I was",
    "Diabolic Esper was",
    "Code Nemesis is",
    "GM Crow is",
    "Heavy Stance is",
    "Magirific is",
    "GameGuard is",
    "HailsNet is",
    "the bracket is"
        };

        string[] elsProblem = new string[] {
    "in my eyes",
    "broken",
    "laggy",
    "hacked",
    "too loud",
    "uncomfortable",
    "OP",
    "fraudulent",
    "disturbing me",
    "glitch abusing",
    "upside-down",
    "violating the rules",
    "cable pulling",
    "totally spooking me out",
    "unnecessarily rude",
    "making funny faces at me",
    "trash-talking mid-match",
    "using Painkiller",
    "making excuses",
    "spamming projectiles",
    "not fair",
    "way better than my character",
    "speaking Japanese",
    "too bright",
    "nerfed",
    "garbage",
    "not good enough",
    "too small",
    "too big",
    "on a bad day",
    "sandbagging",
    "springstepping",
    "reminding me of my ex",
    "really annoying",
    "tired",
    "a big gimmick",
    "kinda sweaty",
    "drunk",
    "only using one move",
    "not listening to me",
    "sleeping",
    "cheap",
    "terrible for my character",
    "title hacking",
    "using infinite combos",
    "taunting",
    "cheating",
    "nerfed in the last patch",
    "buffed in the last patch",
    "using glitches",
    "too hard to reach",
    "future hacking",
    "using better moves than me",
    "playing on an Alienware(TM) gaming computer",
    "sitting slightly closer to the screen",
    "not wearing their glasses",
    "sober",
    "using items",
    "Boom looping",
    "running",
    "in Australia",
    "on tilt",
    "X dropping",
    "mana breaking",
    "ethically superior to me",
    "only using the Z key",
    "only using the X key",
    "active spamming",
    "picking stages that I don't like",
    "bad and should feel bad",
    "FPS hacking",
    "cold",
    "sticky",
    "blocking the screen",
    "trash talking in megaphones",
    "dating LucyStarfia",
    "better than me",
    "outplaying me",
    "too fast",
    "wearing a polar bear suit",
    "using counters too much",
    "spamming Airelinna",
    "banned",
    "really hard to remember",
    "modded",
    "clocking",
    "sliding into stoic",
    "pressing too many buttons",
    "not pressing any buttons",
    "mashing",
    "wearing sparring set",
    "wearing a raid weapon",
    "updating",
    "circle camping on Cargo",
    "Vietnamese",
    "Mexican",
    "bailando la bamba",
    "dumb",
    "on HailsNet",
    "obviously rigged",
    "running torrents",
    "on Windows 8",
    "on Windows 7",
    "biased",
    "trying too hard"
        };

        Random random = new Random();


        public JohnCommand() : base("JohnCommand")
        {
            Expressions.Add(new Regex("(?i)^!john\\b"));
            Expressions.Add(new Regex("(?i)^!johnsword\\b"));
        }

        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            string[] parts = message.Content.Split(' ');

            string[] tempPrefix = johnPrefix;
            string[] tempSubject = johnSubject;
            string[] tempProblem = johnProblem;

            if (parts.Length > 1)
            {
                if (parts[1] == "elsword") {
                    tempSubject = elsSubject;
                    tempProblem = elsProblem;
                }
            }

            if (parts[0] == "!johnsword")
            {
                tempSubject = elsSubject;
                tempProblem = elsProblem;
            }


            int prefixIndex = random.Next(0, tempPrefix.Length);
            int subjectIndex = random.Next(0, tempSubject.Length);
            int problemIndex = random.Next(0, tempProblem.Length);

            string result = String.Format("{0} {1} {2}.", tempPrefix[prefixIndex], tempSubject[subjectIndex], tempProblem[problemIndex]);

            platform.Send(new SendData(result, false, message));
        }
    }
}
