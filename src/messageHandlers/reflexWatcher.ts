import PersistentService from "../persistentService";
import ExtendedInfo from "../extendedInfo";
import TwitchExtendedInfo from "../extendedInfos/twitchExtendedInfo";
import MessageFloodgate from "../messageFloodgate";
import DiscordExtendedInfo from "../extendedInfos/discordExtendedInfo";
import * as fs from "fs";
import * as Discord from "discord.js";
import fetch from "node-fetch";

interface ReflexServerPlayer {
	name: string;
	score: number;
}

interface ReflexServer {
	address: string;
	ip: string;
	port: number;

	info: {
			serverName: string;
			map: string;
			gameTypeShort: string;
			gameTypeFull: string;
			players: number;
			maxPlayers: number;
	};

	players: ReflexServerPlayer[];
	filteredPlayers: {
			count: number;
			players: ReflexServerPlayer[];
	}
}

async function checkStream(type: string, backend: any, discordChannel: string, serverURL: string, baseTopic: string, baseTitle: string, activeTitle: string) {
	if (type != "DISCORD")
		return;
	const client = backend as Discord.Client;
	const targetChannel = client.channels.filter(x => x.id == discordChannel).first() as Discord.TextChannel;
	try {
		const dateThen = Date.now();
		const request = await fetch(serverURL);
		const json = await request.json();
		const servers = (json.servers as ReflexServer[]).sort((a, b) => {return a.port - b.port;});

		let active = false;

		let response = baseTopic + "\n\n***:warning: REFLEX SERVERS:***\n\n";
		for (let s of servers) {
			response += `**${s.info.serverName.split(" |")[0]}** - ${s.info.players}/${s.info.maxPlayers} playing ${s.info.gameTypeFull} on ${s.info.map}\n`;
			if (s.filteredPlayers.count != 0)
				active = true;
			for (let p of s.players) {
				if (s.info.gameTypeFull == "Race") {
					response += `${p.name} - ${new Date(p.score).toISOString().slice(14, -1)}\n`;
				} else {
					response += `${p.score} - ${p.name}\n`;
				}
			}
			response += `**steam://connect/${s.address}**\n`
			response += "\n";
		}
		await targetChannel.setTopic(response);
		await targetChannel.setName(active ? activeTitle : baseTitle);
	} catch (err) {
		console.error(err);
	}
}

function readSettings(): Promise<string> {
	return new Promise((resolve, reject) => {
		fs.readFile("resources/reflexWatcher.json", "utf8", (err, data) => {
			if (err) {
				reject(err);
			} else {
				resolve(data);
			}
		})
	});
}

const setAsyncInterval = (callback: () => Promise<void>, delay: number) => setInterval(() => callback().catch(console.error), delay);

class ReflexWatcher implements PersistentService {

	private _discordChannel: string;
	private _serverURL: string;
	private _baseTopic: string;
	private _baseTitle: string;
	private _activeTitle: string;
	
	public static async create() {
		const data = await readSettings();
		const watcher = new ReflexWatcher();
		watcher._discordChannel = JSON.parse(data).discordChannel;
		watcher._serverURL = JSON.parse(data).serverURL;
		watcher._baseTopic = JSON.parse(data).baseTopic;
		watcher._baseTitle = JSON.parse(data).baseTitle;
		watcher._activeTitle = JSON.parse(data).activeTitle;
		return watcher;
	}

	async backendInitialized(type: string, backend: any) {
		setAsyncInterval(() => checkStream(type, backend, this._discordChannel, this._serverURL, this._baseTopic, this._baseTitle, this._activeTitle), 5000);
	}

}

export default ReflexWatcher;