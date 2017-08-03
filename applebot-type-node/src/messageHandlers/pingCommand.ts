import MessageHandler from "../messageHandler";
import ExtendedInfo from "../extendedInfo";
import TwitchExtendedInfo from "../twitchExtendedInfo";
import MessageFloodgate from "../messageFloodgate";

class PingCommand implements MessageHandler {

    private _floodgate = new MessageFloodgate(10);

    async handleMessage(responder: (content: string) => Promise<void>, content: string, info: ExtendedInfo | undefined) {
        const resp = async (content: string, bypass = false) => await this._floodgate.post(async () => await responder(content), bypass);

        if (/^!ping$/.test(content)) {
            switch (info != undefined ? info.type : undefined) {
                case "TWITCH": {
                    const twitchInfo = info as TwitchExtendedInfo;
                    if (twitchInfo.moderator) {
                        await resp(`Pong! MrDestructoid You are a moderator | ${new Date()}`, true);
                    } else {
                        await resp(`Pong! MrDestructoid | ${new Date()}`);
                    }
                    break;
                }
                case "DISCORD": {
                    await resp(`Pong! :robot: | ${new Date()}`);
                    break;
                }
                default: {
                    await resp(`Pong! | ${new Date()}`);
                    break;
                }
            }
        }
    }

}

export default PingCommand;