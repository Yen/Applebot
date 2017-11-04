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
	card_set_id: number
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

	//https://shadowverse-portal.com/image/card/en/C_${card['card_id']}.png

class SVLookup implements MessageHandler {

	keywords = /(Clash|Storm|Rush|Bane|Drain|Spellboost|Ward|Fanfare|Last Words|Evolve|Earth Rite|Overflow|Vengeance|Evolve|Necromancy \((\d{1}|\d{2})\)|Enhance \((\d{1}|\d{2})\)|Countdown \((\d{1}|\d{2})\))/g

	async handleMessage(responder: (content: string) => Promise<void>, content: string, info: ExtendedInfo | undefined) {
		if (info == undefined || info.type != "DISCORD")
			return;

		const matches = content.match(/{{[A-Za-z-,\s]*}}/g);
		if (matches == null)
			return;

		for (let m of matches) {
			const target = m.slice(2, -2);
			console.log(target);
			const discordInfo = info as DiscordExtendedInfo;
			const request = await fetch(`http://sv.kaze.rip/cards/${target}`);
			const json = await request.json();
			const cards = json as Card[];

			if (cards.length < 1) {
				await discordInfo.message.channel.send({embed: {
					color: 0xD00000,
					title: "That search doesn't match any cards. Check for spelling errors?"
				}});
				return;
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
							title: "That search matches multiple cards. Could you be more specific?"
						}});
					} else {
						await discordInfo.message.channel.send({embed: {
							color: 0xD00000,
							title: "That search matches a large number of cards. Could you be more specific?"
						}});
					}
					return;
				}
			} else {
				card = uniqueCards[0];
			}

			if (card.card_name == "Jolly Rogers") {
				card.card_name = "Bane Rogers";
				card.skill_disc = "Randomly gain Bane, Bane or Bane.";
			}

			let sanitizedTribe = "";
			if (card.tribe_name != "-")
				sanitizedTribe = `(${card.tribe_name})`
	
			card.skill_disc = card.skill_disc.replace(/<br>/g, "\n");
			card.skill_disc = card.skill_disc.replace(/\\\\/g, "");
			card.skill_disc = card.skill_disc.replace(this.keywords, "**$&**");
			card.evo_skill_disc = card.evo_skill_disc.replace(/<br>/g, "\n");
			card.evo_skill_disc = card.evo_skill_disc.replace(/\\\\/g, "");
			card.evo_skill_disc = card.evo_skill_disc.replace(this.keywords, "**$&**");
	
			const embed = new Discord.RichEmbed()
				.setTitle(card.card_name + ` - ${card.cost} PP`)
				.setURL(`http://sv.bagoum.com/cards/${card.card_id}`)
				.setThumbnail(`https://shadowverse-portal.com/image/card/en/C_${card.card_id}.png`)
				.setFooter(Craft[card.clan] + " " + Rarity[card.rarity] + " - " + Set[card.card_set_id]);
	
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
	
			switch (card.char_type) {
				case 1: {
					embed.setDescription(`${card.atk}/${card.life} ➤ ${card.evo_atk}/${card.evo_life} - Follower ${sanitizedTribe}\n\n${card.skill_disc}`)
					console.log(card);
					if (card.evo_skill_disc != card.skill_disc && card.evo_skill_disc != "")
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
	
			await discordInfo.message.channel.send({embed});
		}
	}

}

export default SVLookup;