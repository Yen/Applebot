import TwitchClient from "./twitchClient";
import MessageHandler from "./messageHandler";
import ExtendedInfo from "./extendedInfo";
import DiscordExtendedInfo from "./discordExtendedInfo";
import TwitchExtendedInfo from "./twitchExtendedInfo";

import PingCommand from "./messageHandlers/pingCommand";
import ApplebotInfoCommand from "./messageHandlers/applebotInfoCommand";
import YoutubeParser from "./messageHandlers/youtubeParser";

import * as Discord from "discord.js";
import * as fs from "fs";

console.log("Applebot");

const twitchSettings = JSON.parse(fs.readFileSync("resources/twitch.json", "utf8"));
const discordSettings = JSON.parse(fs.readFileSync("resources/discord.json", "utf8"));

async function submitToHandlers(handlers: MessageHandler[], responder: (content: string) => Promise<void>, content: string, info?: ExtendedInfo) {
	for (const h of handlers) {
		h.handleMessage(responder, content, info)
			.catch(err => {
				console.error("Error occurred in handler");
				if (err) {
					console.error(err);
				}
			});
	}
}

async function prepareTwitch(handlers: MessageHandler[]) {
	const channels = twitchSettings.channels as string[];
	const moderators = new Map(channels.map(c => [c, new Set()] as [string, Set<string>]));

	const client = await TwitchClient.createClient(twitchSettings.username, twitchSettings.oauth);

	client.addOnMessageListener(async (username, channel, content) => {
		const responder = async (c: string) => {
			await client.sendMessage(channel, c);
		};

		const isModerator = (): boolean => {
			const moderatorsSet = moderators.get(channel);
			if (moderatorsSet == undefined) {
				// this should not really happen
				return false;
			}

			return moderatorsSet.has(username);
		};

		const extendedInfo: TwitchExtendedInfo = {
			type: "TWITCH",
			username,
			channel,
			moderator: isModerator(),
			sendMessage: client.sendMessage
		};

		await submitToHandlers(handlers, responder, content, extendedInfo);
	});

	client.addOnModeratorListener(async (username, channel, moderator) => {
		const set = moderators.get(channel);
		if (set == undefined) {
			throw new Error("Channel not found in moderators map");
		}
		if (moderator) {
			console.log(`Adding moderator "${username}" to channel #${channel}`);
			set.add(username);
		} else {
			console.log(`Removing moderator "${username}" from channel #${channel}`);
			set.delete(username);
		}
	});

	await Promise.all(channels.map(c => client.joinChannel(c)));
}

async function prepareDiscord(handlers: MessageHandler[]) {
	const client = new Discord.Client();

	client.on("ready", () => console.log("Discord client ready"));

	client.on("message", message => {
		const responder = async (content: string) => {
			await message.channel.send(content);
		};
		const info: DiscordExtendedInfo = {
			type: "DISCORD",
			message
		};
		submitToHandlers(handlers, responder, message.content, info)
			.catch(console.error);
	});

	await client.login(discordSettings.token);
}

(async () => {
	const handlers: MessageHandler[] = [
		new PingCommand(),
		new ApplebotInfoCommand(),
		new YoutubeParser()
	];

	await Promise.all([
		prepareTwitch(handlers),
		prepareDiscord(handlers)
	]);
})().catch(reason => {
	console.error("Error caused application to exit");
	if (reason) {
		console.error(reason);
	}
});
