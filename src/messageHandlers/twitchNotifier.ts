import PersistentService from "../persistentService";
import ExtendedInfo from "../extendedInfo";
import TwitchExtendedInfo from "../extendedInfos/twitchExtendedInfo";
import MessageFloodgate from "../messageFloodgate";
import DiscordExtendedInfo from "../extendedInfos/discordExtendedInfo";
import * as fs from "fs";
import * as Discord from "discord.js";

function readSettings(): Promise<string | undefined> {
	return new Promise(resolve => {
		fs.readFile("resources/twitchNotifier.json", "utf8", (err, data) => {
			if (err) {
				resolve();
			} else {
				resolve(data);
			}
		})
	});
}

const setAsyncInterval = (callback: () => Promise<void>, delay: number) => setInterval(() => callback().catch(console.error), delay);

class TwitchNotifier implements PersistentService {

	private _discordChannel: string;
	private _twitchChannel: string;

	private constructor(discordChannel: string, twitchChannel: string) {
		this._discordChannel = discordChannel;
		this._twitchChannel = twitchChannel;
	}

	public static async create() {
		const data = await readSettings();
		if (data == undefined) {
			return undefined;
		}
		const discordChannel = JSON.parse(data).discordChannel;
		const twitchChannel = JSON.parse(data).twitchChannel;
		return new TwitchNotifier(discordChannel, twitchChannel);
	}

	async backendInitialized(type: string, backend: any) {
		const client = backend as Discord.Client;
		let debouncer: { [user: string]: number } = {};
		client.on('ready', () => {
			const targetChannel = client.channels.filter(x => x.id == this._discordChannel).first() as Discord.TextChannel;
			client.on("presenceUpdate", (oldMember, newMember) => {
				console.log(`${newMember.user.username} → ${newMember.user.presence.status}`);
				console.log("last seen: " + debouncer[newMember.user.id]);
				let askMeIfIGiveAFuck = false;
				if (oldMember.presence.game) {
					if (!oldMember.presence.game.streaming)
						askMeIfIGiveAFuck = true;
				} else {
					askMeIfIGiveAFuck = true;
				}
				if (newMember.presence.game && askMeIfIGiveAFuck) {
					if (newMember.presence.game.streaming) {
						if (((Date.now() - (debouncer[newMember.user.id] || 0)) / 1000 / 60) <= 30) {
							let username = newMember.user.presence.game.url.substring(newMember.user.presence.game.url.lastIndexOf("/") + 1);
							if (this._twitchChannel == username) {
								targetChannel.send(`@everyone :tyroneW: STRIM: **${newMember.presence.game.name}** — ${newMember.presence.game.url}`);
							} else {
								targetChannel.send(`**${newMember.user.username}** is now streaming: **${newMember.presence.game.name}** — ${newMember.presence.game.url}`, { disableEveryone: true });
							}
						} else {
							console.log("skipping because too soon");
						}
					}
					debouncer[newMember.user.id] = Date.now();
				}
			});
		});
	}
}

export default TwitchNotifier;