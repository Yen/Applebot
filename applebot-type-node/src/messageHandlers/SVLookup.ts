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
	rarity: number
	char_type: number
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

	//https://shadowverse-portal.com/image/card/en/C_${card['card_id']}.png

class SVLookup implements MessageHandler {

	keywords = /(Storm|Rush|Bane|Drain|Spellboost|Ward|Fanfare|Last Words|Evolve|Earth Rite|Overflow|Vengeance|Evolve|Necromancy \((\d{1}|\d{2})\)|Enhance \((\d{1}|\d{2})\)|Countdown \((\d{1}|\d{2})\))/g

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
	
			let craftLine = Craft[card.clan] + " " + Rarity[card.rarity];
			if (card.tribe_name != "-")
				craftLine = Rarity[card.rarity] + " " + Craft[card.clan] + " " + card.tribe_name;
	
			card.skill_disc = card.skill_disc.replace(/<br>/g, "\n");
			card.skill_disc = card.skill_disc.replace(/\\\\/g, "");
			card.skill_disc = card.skill_disc.replace(this.keywords, "**$&**");
			card.evo_skill_disc = card.evo_skill_disc.replace(/<br>/g, "\n");
			card.evo_skill_disc = card.evo_skill_disc.replace(/\\\\/g, "");
			card.evo_skill_disc = card.evo_skill_disc.replace(this.keywords, "**$&**");
	
			const embed = new Discord.RichEmbed()
				.setTitle(card.card_name + ` - ${card.cost} PP`)
				.setURL(`http://sv.bagoum.com/cards/${card.card_id}`)
				.setThumbnail(`https://shadowverse-portal.com/image/card/en/C_${card.card_id}.png`);
	
			switch (card.rarity) {
				case 1: {
					embed.setColor(0xCD7F32);
					break;
				}
				case 2: {
					embed.setColor(0xC0C0C0);
					break;
				}
				case 3: {
					embed.setColor(0xFFD700);
					break;
				}
				case 4: {
					embed.setColor(0xB9F2FF);
					break;
				}
			}
	
			switch (card.char_type) {
				case 1: {
					embed.setDescription(`${card.atk}/${card.life} ➤ ${card.evo_atk}/${card.evo_life} - ${craftLine}\n\n${card.skill_disc}`)
					console.log(card);
					if (card.evo_skill_disc != card.skill_disc && card.evo_skill_disc != "")
						embed.addField("Evolved", card.evo_skill_disc, true);
					break;
				}
				case 2: {
					embed.setDescription("Amulet - " + craftLine + "\n\n" + card.skill_disc);
					break;
				}
				case 3: {
					embed.setDescription("Amulet - " + craftLine + "\n\n" + card.skill_disc);
					break;
				}
				case 4: {
					embed.setDescription("Spell - " + craftLine + "\n\n" + card.skill_disc);
					break;
				}
			}
	
			await discordInfo.message.channel.send({embed});
		}
	}

}

export default SVLookup;