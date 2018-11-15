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
			const targetGuild = targetChannel.guild.id;
			client.on("presenceUpdate", (oldMember, newMember) => {
				if (newMember.guild.id != targetGuild) {
					return;
				}
				if (newMember.presence.game) {
					if (newMember.presence.game.streaming) {
						console.log(`${newMember.user.username} → ${newMember.user.presence.status}`);
						console.log("last seen: " + debouncer[newMember.user.id]);
						if (((Date.now() - (debouncer[newMember.user.id] || 0)) / 1000 / 60) >= 360) {
							let username = newMember.user.presence.game.url.substring(newMember.user.presence.game.url.lastIndexOf("/") + 1);
							if (this._twitchChannel == username) {
								targetChannel.send(`@everyone TYRON STREAM :D?\n**${newMember.presence.game.name}** — ${newMember.presence.game.url}`);
							} else {
								targetChannel.send(`**${newMember.user.username}** is now streaming: **${newMember.presence.game.name}** — ${newMember.presence.game.url}`, { disableEveryone: true });
							}
						} else {
							console.log("skipping because too soon");
						}
						debouncer[newMember.user.id] = Date.now();
					}
				} else {
					console.log("no game");
				}
			});
		});
	}
}

export default TwitchNotifier;