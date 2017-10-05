import MessageHandler from "../messageHandler";
import ExtendedInfo from "../extendedInfo";
import TwitchExtendedInfo from "../extendedInfos/twitchExtendedInfo";
import MessageFloodgate from "../messageFloodgate";

class Fightan implements MessageHandler {

	private _fightan = ["Last Bronx", "Galaxy Fight", "Waku Waku 7", "Psychic Force 2012", "Rakuga Kids", "Breakers Revenge", "Real Bout Fatal Fury 2", "Super Dragon Ball Z", "Bleach: Dark Souls", "Jump Ultimate Stars", "Castlevania Judgement", "NeoGeo Battle Coliseum", "SNK Gals Fighters", "Battle Monsters", "Fight 'N' Jokes", "X-Men Next Dimension", "The King of Fighters EX2: Howling Blood", "Fatal Fury Wild Ambition", "Cartoon Network Punch Time Explosion XL", "Digimon Battle Spirit 1.5", "Karnov's Revenge", "Fighting Layer", "Godzilla: Destroy All Monsters Melee", "The Last Blade 2", "Savage Reign", "Buriki One", "Samurai Shodown 64: Warrior's Rage", "Battle Fantasia", "Rage of the Dragons", "World Heroes 2", "Ninja Masters Haoh Ninpo Cho", "Martial Masters", "Slap Happy Rhythm Busters", "Fighters Destiny", "Battle Arena Toshinden 3", "Guilty Gear Petit", "Mace: The Dark Age", "Eternal Champions"]

	async handleMessage(responder: (content: string) => Promise<void>, content: string, info: ExtendedInfo | undefined) {
		if (/^!fightan$/.test(content)) {
			responder("Why don't you play a real fighting game, like " + this._fightan[Math.floor(Math.random() * this._fightan.length)] + "?");
		}
	}

}

export default Fightan;