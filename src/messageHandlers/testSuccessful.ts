import MessageHandler from "../messageHandler";
import ExtendedInfo from "../extendedInfo";
import TwitchExtendedInfo from "../extendedInfos/twitchExtendedInfo";
import MessageFloodgate from "../messageFloodgate";
import * as Discord from "discord.js";
import DiscordExtendedInfo from "../extendedInfos/discordExtendedInfo";

const reactions: string[] = ["✅", "🆗", "👌", "👍"];

class TestSuccessful implements MessageHandler {

	async handleMessage(responder: (content: string) => Promise<void>, content: string, info: ExtendedInfo | undefined) {
		if (/^test$/i.test(content)) {
			switch (info != undefined ? info.type : undefined) {
				case "DISCORD": {
					const discordInfo = info as DiscordExtendedInfo;
					await discordInfo.message.react(reactions[Math.floor(Math.random() * reactions.length)]);
					break;
				}
				default: {
					await responder(Math.random() > .1 ? "Test successful." : "Test failed.");
					break;
				}
			}
		}
	}

}

export default TestSuccessful;