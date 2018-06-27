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
let lastCommunityStreams: Stream[] =[];

interface Stream {
	channel: Channel,
	game: string,
}

interface Channel {
	name: string;
	status: string;
	url: string;
}

async function checkStream(type: string, backend: any, discordChannel: string, clientID: string, twitchChannel: string, community: string) {
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
	try {
		const request = await fetch(`https://api.twitch.tv/kraken/streams?community_id=${community}`, 
			{method: "GET", headers: {"Client-ID": clientID, "Accept": "application/vnd.twitchtv.v5+json"}
		});
		const json = await request.json();
		const streams = (json.streams as Stream[]);
		for (let s of streams) {
			if ((lastCommunityStreams.filter(x => x.channel.name == s.channel.name).length == 0) && s.channel.name != twitchChannel) {
				await targetChannel.send(`Community stream: **${s.game}** - *${s.channel.status}*\nWatch at ${s.channel.url}`, {disableEveryone: true});
			}
		}
		lastCommunityStreams = streams;
	} catch (err) {
		console.error(err);
	}
}

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
	private _clientID: string;
	private _twitchChannel: string;
	private _community: string;
	
	private constructor(discordChannel: string, clientID: string, twitchChannel: string, community: string) {
		this._discordChannel = discordChannel;
		this._clientID = clientID;
		this._twitchChannel = twitchChannel;
		this._community = community;
	}
	
	public static async create() {
		const data = await readSettings();
		if (data == undefined) {
			return undefined;
		}
		const discordChannel = JSON.parse(data).discordChannel;
		const clientID = JSON.parse(data).clientID;
		const twitchChannel = JSON.parse(data).twitchChannel;
		const community = JSON.parse(data).community;
		return new TwitchNotifier(discordChannel, clientID, twitchChannel, community);
	}

	async backendInitialized(type: string, backend: any) {
		const request = await fetch(`https://api.twitch.tv/kraken/communities?name=${encodeURIComponent(this._community)}`, 
			{method: "GET", headers: {"Client-ID": this._clientID, "Accept": "application/vnd.twitchtv.v5+json"}
		});
		const json = await request.json();
		setAsyncInterval(() => checkStream(type, backend, this._discordChannel, this._clientID, this._twitchChannel, json._id), 30000);
	}

}

export default TwitchNotifier;