import MessageHandler from "../messageHandler";
import ExtendedInfo from "../extendedInfo";
import TwitchExtendedInfo from "../twitchExtendedInfo";
import MessageFloodgate from "../messageFloodgate";

class PingCommand implements MessageHandler {

	private _floodgate = new MessageFloodgate(10);

	async handleMessage(responder: (content: string) => Promise<void>, content: string, info: ExtendedInfo | undefined) {
		const resp = this._floodgate.createResponder(responder);

		if (/^!ping$/.test(content)) {
			switch (info != undefined ? info.type : undefined) {
				case "TWITCH": {
					const twitchInfo = info as TwitchExtendedInfo;
					if (twitchInfo.moderator) {
						await resp(`Pong! MrDestructoid You are a moderator | ${new Date()}`, true);
					} else {
						await resp(`Pong! MrDestructoid | ${new Date()}`, false);
					}
					break;
				}
				case "DISCORD": {
					await resp(`Pong! :robot: | ${new Date()}`, false);
					break;
				}
				default: {
					await resp(`Pong! | ${new Date()}`, false);
					break;
				}
			}
		}
	}

}

export default PingCommand;