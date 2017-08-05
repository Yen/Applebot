import TwitchClient from "./twitchClient";
import TwitchExtendedInfo from "./twitchExtendedInfo";

interface TwitchLoginInfo {
	username: string;
	oauth: string;
}

export type TwitchManagerMessageListener = (content: string, extendedInfo: TwitchExtendedInfo) => Promise<void>;

interface TwitchManagerMetaClient {
	clientPromise: Promise<TwitchClient>;
}

// TODO: error handlers with reconnect
class TwitchManager {

	private _clients: Map<string, TwitchManagerMetaClient> = new Map();
	private _moderators: Map<string, Set<string>> = new Map();

	private _onMessageListeners: TwitchManagerMessageListener[] = [];

	// TODO: secondary login infos
	constructor(info: TwitchLoginInfo, channels: string[]) {
		// initialize moderators map
		for (const c of channels) {
			this._moderators.set(c, new Set());
		}

		const clientPromise = TwitchClient.createClient(info.username, info.oauth);
		this._clients.set(info.username, {
			clientPromise
		});

		(async () => {
			const client = await clientPromise;
			client.addOnMessageListener(async (username, channel, content) => {
				await this._handleClientMessage(info.username, username, channel, content);
			});
			client.addOnModeratorListener(async (username, channel, moderator) => {
				const moderatorsSet = this._moderators.get(channel);
				if (moderatorsSet == undefined) {
					throw new Error("Channel not found in moderators map");
				}
				if (moderator) {
					console.log(`Adding moderator permission "${username}" to channel "#${channel}"`);
					moderatorsSet.add(username);
				} else {
					console.log(`Removing moderator permission "${username}" from channel "#${channel}"`);
					moderatorsSet.delete(username);
				}
			});

			const promises = channels.map(c => client.joinChannel(c));
			await Promise.all(promises);
		})().catch(console.error);
	}

	async sendMessage(clientUsername: string, channel: string, content: string) {
		const metaClient = this._clients.get(clientUsername);
		if (metaClient == undefined) {
			throw new Error("Client with source username not found in list");
		}

		const client = await metaClient.clientPromise;
		await client.sendMessage(channel, content);
	}

	addOnMessageListener(listener: TwitchManagerMessageListener) {
		this._onMessageListeners = [...this._onMessageListeners, listener];
	}

	private async _handleClientMessage(clientUsername: string, username: string, channel: string, content: string) {
		const twitchSendMessage = async (scopeClientUsername: string, scopeChannel: string, scopeContent: string) => {
			const client = await this._clients.get(scopeClientUsername)!.clientPromise;
			await client.sendMessage(scopeChannel, scopeContent);
		};

		const extendedInfo: TwitchExtendedInfo = {
			type: "TWITCH",
			clientUsername,
			username,
			channel,
			// TODO: error check
			moderator: this._moderators.get(channel)!.has(username),
			sendMessage: twitchSendMessage
		};
		await Promise.all(this._onMessageListeners.map(l => l(content, extendedInfo)));
	}

}

export default TwitchManager;
