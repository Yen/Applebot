import PersistentService from "../persistentService";
import ExtendedInfo from "../extendedInfo";
import TwitchExtendedInfo from "../extendedInfos/twitchExtendedInfo";
import MessageFloodgate from "../messageFloodgate";
import DiscordExtendedInfo from "../extendedInfos/discordExtendedInfo";
import * as fs from "fs";
import * as Discord from "discord.js";
import fetch from "node-fetch";

const offlineThreshold: number = 60;
let offlinePolls: number = offlineThreshold;
let lastGame: string;

async function checkStream(type: string, backend: any, discordChannel: string, clientID: string, twitchChannel: string) {
	if (type != "DISCORD")
		return;
	const client = backend as Discord.Client;
	const targetChannel = client.channels.filter(x => x.id == discordChannel).first() as Discord.TextChannel;
	try {
		const dateThen = Date.now();
		const request = await fetch(`https://api.twitch.tv/kraken/streams/${twitchChannel}`, 
			{method: "GET", headers: {"Client-ID": clientID}
		});
		const json = await request.json();
		if (json.stream == null) {
			offlinePolls++;
			if (offlinePolls == offlineThreshold) {
				// await targetChannel.send("Stream offline.");
			}
		} else {
			if (offlinePolls >= offlineThreshold) {
				await targetChannel.send(`@everyone Now live: **${json.stream.game}** - *${json.stream.channel.status}*\nWatch at https://twitch.tv/${twitchChannel}`);
			} else {
				if (json.stream.game != lastGame)
					await targetChannel.send(`@here Game changed: **${json.stream.game}** - *${json.stream.channel.status}*\nWatch at https://twitch.tv/${twitchChannel}`);
			}
			lastGame = json.stream.game;
			offlinePolls = 0;
		}
	} catch (err) {
		console.error(err);
	}
}

function readSettings(): Promise<string> {
	return new Promise((resolve, reject) => {
		fs.readFile("resources/twitchNotifier.json", "utf8", (err, data) => {
			if (err) {
				reject(err);
			} else {
				resolve(data);
			}
		})
	});
}

const setAsyncInterval = (callback: () => Promise<void>, delay: number) => setInterval(() => callback().catch(console.error), delay);

class TwitchNotifier implements PersistentService {

	private _discordChannel: string;
	private _clientID: string;
	private _twitchChannel: string;
	
	private constructor(discordChannel: string, clientID: string, twitchChannel: string) {
		this._discordChannel = discordChannel;
		this._clientID = clientID;
		this._twitchChannel = twitchChannel;
	}
	
	public static async create() {
		const data = await readSettings();
		const discordChannel = JSON.parse(data).discordChannel;
		const clientID = JSON.parse(data).clientID;
		const twitchChannel = JSON.parse(data).twitchChannel;
		return new TwitchNotifier(discordChannel, clientID, twitchChannel);
	}

	async backendInitialized(type: string, backend: any) {
		setAsyncInterval(() => checkStream(type, backend, this._discordChannel, this._clientID, this._twitchChannel), 30000);
	}

}

export default TwitchNotifier;