import MessageHandler from "../messageHandler";
import ExtendedInfo from "../extendedInfo";
import TwitchExtendedInfo from "../extendedInfos/twitchExtendedInfo";
import MessageFloodgate from "../messageFloodgate";
import * as Discord from "discord.js";
import DiscordExtendedInfo from "../extendedInfos/discordExtendedInfo";
import fetch from "node-fetch";

interface Card {
	card_id: number,
	card_name: string,
	clan: number,
	tribe_name: string,
	skill_disc: string,
	evo_skill_disc: string,
	cost: number,
	atk: number,
	life: number,
	evo_atk: number,
	evo_life: number,
	rarity: number,
	char_type: number,
	card_set_id: number,
	description: string,
	evo_description: string,
	base_card_id: number,
	normal_card_id: number
}

enum Craft {
    Neutral = 0,
	Forestcraft,
	Swordcraft,
	Runecraft,
	Dragoncraft,
	Shadowcraft,
	Bloodcraft,
	Havencraft
}

enum Rarity {
	Bronze = 1,
	Silver,
	Gold,
	Legendary
}

enum Set {
	"Basic Card" = 10000,
	"Standard",
	"Darkness Evolved",
	"Rise of Bahamut",
	"Tempest of the Gods",
	"Wonderland Dreams",
	"Starforged Legends",
	"Token" = 90000
}

class SVLookup implements MessageHandler {

	static keywords = /(Clash:?|Storm:?|Rush:?|Bane:?|Drain:?|Spellboost:?|Ward:?|Fanfare:?|Last Words:?|Evolve:|Earth Rite:?|Overflow:?|Vengeance:?|Evolve:?|Necromancy \((\d{1}|\d{2})\):?|Enhance \((\d{1}|\d{2})\):?|Countdown \((\d{1}|\d{2})\):?|Necromancy:?|Enhance:?|Countdown:?)/g

	private _cards: Card[];
	
	private constructor(cards: Card[]) {
		this._cards = cards;
	}

	public static async create() {
		const request = await fetch(`http://sv.kaze.rip/cards/`);
		const json = await request.json();
		const cards = json as Card[];

		for (let c of cards) {
			c.skill_disc = SVLookup.escape(c.skill_disc).replace(SVLookup.keywords, "**$&**");
			c.evo_skill_disc = SVLookup.escape(c.evo_skill_disc).replace(SVLookup.keywords, "**$&**");
			c.description = SVLookup.escape(c.description);
			c.evo_description = SVLookup.escape(c.evo_description);
		}

		console.log(`Starting SVLookup with ${cards.length} cards`);
		return new SVLookup(cards);
	}
	
	static escape(text: String) { // the api uses like 7 different encodings and it's a trash fire
		let r = /\\u([\d\w]{4})/gi;
		text = text.replace(/<br>/g, "\n")
			.replace(/\\n/g, "\n")
			.replace(/\\\\/g, "")
			.replace("&#169;", "©")
			.replace(r, function (match, grp) {
				return String.fromCharCode(parseInt(grp, 16));
			});
		return decodeURIComponent(text as string);
	}
	
	async sendError(error: String, discordInfo: DiscordExtendedInfo) {
		await discordInfo.message.channel.send({embed: {
			color: 0xD00000,
			title: error
		}});
	}
	
	async handleMessage(responder: (content: string) => Promise<void>, content: string, info: ExtendedInfo | undefined) {
		if (info == undefined || info.type != "DISCORD")
			return;
		
		content = content.toLowerCase();
		const matches = content.match(/{{[a-z-,\?\/\s]+}}/g);
		if (matches == null)
			return;

		for (let m of matches) {
			const optionMatches = m.match(/[a-z]+(?=\/)/);
			let options = "";
			if (optionMatches != null)
				options = optionMatches[0].toString();
			const target = m.slice(2, -2).replace(options + "/", "");
			
			const discordInfo = info as DiscordExtendedInfo;

			let cards = this._cards.filter(x => x.card_name.toLowerCase().includes(target));

			if (cards.length < 1) {
				this.sendError(`"${target}" doesn't match any cards. Check for spelling errors?`, discordInfo);
				continue;
			}
	
			const uniqueCards = cards.reduce<Card[]>((acc, val) => acc.find(x => x.card_name == val.card_name) ? acc : [...acc, val], [])

			let card;
			if (uniqueCards.length > 1) {
				const exactMatches = uniqueCards.filter(x => x.card_name.toLowerCase() == target.toLowerCase());
				if (exactMatches.length == 1)
					card = exactMatches[0];
				else {
					if (uniqueCards.length <= 6) {
						const matchTitles = uniqueCards.reduce<string>((acc, val) => acc + "- " + val.card_name + "\n", "");
						await discordInfo.message.channel.send({embed: {
							color: 0xD00000,
							description: matchTitles,
							title: `"${target}" matches multiple cards. Could you be more specific?`
						}});
					} else {
						this.sendError(`"${target}" matches a large number of cards. Could you be more specific?`, discordInfo);
					}
					continue;
				}
			} else {
				card = uniqueCards[0];
			}

			if (card.card_name == "Jolly Rogers") {
				card.card_name = "Bane Rogers";
				card.skill_disc = "Randomly gain Bane, Bane or Bane.";
			}

			let cardname = card.card_name; // TODO: figure out why i can't access the card object from filter statements

			let sanitizedTribe = "";
			if (card.tribe_name != "-")
				sanitizedTribe = `(${card.tribe_name})`

			const legality = [10001, 10002].includes(card.card_set_id) ? "Unlimited" : "Rotation";

			let embed = new Discord.RichEmbed();

			switch (card.rarity) {
				case Rarity.Bronze: {
					embed.setColor(0xCD7F32);
					break;
				}
				case Rarity.Silver: {
					embed.setColor(0xC0C0C0);
					break;
				}
				case Rarity.Gold: {
					embed.setColor(0xFFD700);
					break;
				}
				case Rarity.Legendary: {
					embed.setColor(0xB9F2FF);
					break;
				}
			}
			switch (options) {
				case "a":
				case "art":
					if (card.base_card_id != card.normal_card_id) {
						let baseID = card.base_card_id; // TODO: same shit
						card = this._cards.filter(x => x.card_id == baseID)[0];
						let a = card.card_name.toLowerCase().replace(/\W/g, '');
						embed.setImage("http://sv.bagoum.com/getRawImage/0/1/" + a);
					} else {
						let a = card.card_name.toLowerCase().replace(/\W/g, '');
						embed.setImage("http://sv.bagoum.com/getRawImage/0/0/" + a);
						if (cards.filter(x => x.card_name == cardname).length > 1)
							embed.setFooter(`For alt art, try "aa/${target}".`);
					}
					break;
				case "e":
				case "evo":
					if (card.char_type != 1) {
						this.sendError(`"${card.card_name}" doesn't have evolved art.`, discordInfo);
						return;
					}
					if (card.base_card_id != card.normal_card_id) {
						let baseID = card.base_card_id; // TODO: same shit
						card = this._cards.filter(x => x.card_id == baseID)[0];
						let a = card.card_name.toLowerCase().replace(/\W/g, '');
						embed.setImage("http://sv.bagoum.com/getRawImage/1/1/" + a);
					} else {
						let e = card.card_name.toLowerCase().replace(/\W/g, '');
						embed.setImage("http://sv.bagoum.com/getRawImage/1/0/" + e);
						if (cards.filter(x => x.card_name == cardname).length > 1)
							embed.setFooter(`For alt art, try "aa/${target}".`);
					}

					break;
				case "aa":
				case "altart":
					if (cards.filter(x => x.card_name == cardname).length < 2) {
						this.sendError(`"${card.card_name}" doesn't have alternate art.`, discordInfo);
						return;
					}
					let aa = card.card_name.toLowerCase().replace(/\W/g, '');
					embed.setImage("http://sv.bagoum.com/getRawImage/0/1/" + aa);
					break;
				case "ae":
				case "ea":
				case "evoalt":
				case "altevo":
					if (cards.filter(x => x.card_name == cardname).length < 2) {
						this.sendError(`"${card.card_name}" doesn't have alternate art.`, discordInfo);
						return;
					}
					if (card.char_type != 1) {
						this.sendError(`"${card.card_name}" doesn't have evolved art.`, discordInfo);
						return;
					}
					let ae = card.card_name.toLowerCase().replace(/\W/g, '');
					embed.setImage("http://sv.bagoum.com/getRawImage/1/1/" + ae);
					break;
				case "f":
				case "flavor":
				case "l":
				case "lore":
					embed.setThumbnail(`https://shadowverse-portal.com/image/card/en/C_${card.card_id}.png`).setTitle(card.card_name);
					console.log(card.description);
					if (card.char_type == 1) {
						embed.setDescription("*" + card.description + "\n\n" + card.evo_description + "*");
					} else {
						embed.setDescription("*" + card.description + "*");
					}
					break;
				default:
					embed.setTitle(card.card_name + ` - ${card.cost} PP`)
					.setURL(`http://sv.bagoum.com/cards/${card.card_id}`)
					.setThumbnail(`https://shadowverse-portal.com/image/card/en/C_${card.card_id}.png`)
					.setFooter(Craft[card.clan] + " " + Rarity[card.rarity] + " - " + Set[card.card_set_id] + " (" + legality + ")");
					switch (card.char_type) {
						case 1: {
							embed.setDescription(`${card.atk}/${card.life} ➤ ${card.evo_atk}/${card.evo_life} - Follower ${sanitizedTribe}\n\n${card.skill_disc}`)
							if (card.evo_skill_disc != card.skill_disc && card.evo_skill_disc != "" && !(card.skill_disc.includes(card.evo_skill_disc)))
								embed.addField("Evolved", card.evo_skill_disc, true);
							break;
						}
						case 2: {
							embed.setDescription(`Amulet ${sanitizedTribe}\n\n` + card.skill_disc);
							break;
						}
						case 3: {
							embed.setDescription(`Amulet ${sanitizedTribe}\n\n` + card.skill_disc);
							break;
						}
						case 4: {
							embed.setDescription(`Spell ${sanitizedTribe}\n\n` + card.skill_disc);
							break;
						}
					}
			}
	
			await discordInfo.message.channel.send({embed});
		}
	}

}

export default SVLookup;