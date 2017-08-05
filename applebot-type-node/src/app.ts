import TwitchManager from "./twitchManager";
import MessageHandler from "./messageHandler";
import ExtendedInfo from "./extendedInfo";
import DiscordExtendedInfo from "./discordExtendedInfo";

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
	const loginInfo = {
		username: twitchSettings.username,
		oauth: twitchSettings.oauth
	};
	const twitchManager = new TwitchManager(loginInfo, twitchSettings.channels);

	twitchManager.addOnMessageListener(async (content, info) => {
		const responder = async (c: string) => {
			await twitchManager.sendMessage(info.clientUsername, info.channel, c);
		};

		await submitToHandlers(handlers, responder, content, info);

		console.log(`< ${info.clientUsername} #${info.channel} ${info.username}: ${content}`);
	});
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
