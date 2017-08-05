import * as net from "net";
import * as readline from "readline";

// TODO: does not work with multispaces
function remainderSplit(source: string, limit?: number): string[] {
	const parts = source.split(/\s/);
	if (limit == undefined) {
		return parts;
	} else if (limit > parts.length) {
		return parts;
	}

	let limited = parts.slice(0, limit - 1);
	limited = [...limited, source.substring(limited.join(" ").length + 1)];
	return limited;
}

type TwitchClientMessageListener = (username: string, channel: string, content: string) => Promise<void>;
type TwitchClientModeratorListener = (username: string, channel: string, moderator: boolean) => Promise<void>;

class TwitchClient {

	private _socket: net.Socket;
	private _reader: readline.ReadLine;

	// numbers relate to seconds time stamp
	private _floodQueue: number[] = [];
	private static readonly _floodSpan = 30;
	private static readonly _floodMax = 10;
	private static readonly _floodMaxPriority = 15;

	private _onMessageListeners: TwitchClientMessageListener[] = [];
	private _onModeratorListeners: TwitchClientModeratorListener[] = [];

	static async createClient(username: string, oauth: string): Promise<TwitchClient> {
		console.log(`Creating new twitch client for user "${username}"`);

		return new Promise<TwitchClient>((resolve, reject) => {
			const socket = net.createConnection(6667, "irc.chat.twitch.tv");
			socket.addListener("error", reject);
			socket.addListener("close", reject);

			const reader = readline.createInterface({ input: socket });
			const client = new TwitchClient(socket, reader);

			const onLine = (line: string) => {
				reader.removeListener("line", onLine);

				socket.removeListener("error", reject);
				socket.removeListener("close", reject);

				const parts = line.split(/\s/);
				if (parts.length < 2 || parts[1] != "001") {
					reject("Login failed");
				} else {
					console.log(`Twitch client for user "${username}" login success`);
					// TODO: other memberships for things like subscribers
					// should be added here or refactored all together

					// makes twitch send information about moderators
					client._sendLineRaw("CAP REQ :twitch.tv/membership")
						.then(() => resolve(client))
						.catch(reject);
				}
			};
			reader.addListener("line", onLine);

			socket.addListener("connect", () => {
				socket.write(`PASS ${oauth}\nNICK ${username}\n`);
			});
		});
	}

	private constructor(socket: net.Socket, reader: readline.ReadLine) {
		this._socket = socket;
		this._reader = reader;

		this._reader.addListener("line", line => {
			this._onLine(line).catch(console.error);
		});

		// just dump everything
		//this._reader.addListener("line", console.log);
	}

	addOnMessageListener(listener: TwitchClientMessageListener) {
		this._onMessageListeners = [...this._onMessageListeners, listener];
	}

	addOnModeratorListener(listener: TwitchClientModeratorListener) {
		this._onModeratorListeners = [...this._onModeratorListeners, listener];
	}

	async sendMessage(channel: string, content: string) {
		console.log(`> #${channel}: ${content}`);
		await this._sendLineRaw(`PRIVMSG #${channel} :${content}`);
	}

	async joinChannel(channel: string) {
		console.log(`Joining channel #${channel}`);
		await this._sendLineRaw(`JOIN #${channel}`, true);
	}

	private async _onLine(line: string) {
		const parts = line.split(/\s/);
		if (parts.length < 2) {
			return;
		}

		if (parts[0] == "PING") {
			await this._sendLineRaw("PONG applebot", true);
			return;
		}

		switch (parts[1]) {
			case "PRIVMSG": {
				await this._onPRIVMSG(line);
				break;
			}
			case "JOIN": {
				await this._onJOIN(line);
				break;
			}
			case "MODE": {
				await this._onMODE(line);
				break;
			}
		}
	}

	private async _onPRIVMSG(line: string) {
		const parts = remainderSplit(line, 4);
		if (parts.length < 4) {
			return;
		}

		const username = parts[0].split("!")[0].substring(1);
		const channel = parts[2].substring(1);
		const content = parts[3].substring(1);

		await Promise.all(this._onMessageListeners.map(l => l(username, channel, content)));
	}

	private async _onJOIN(line: string) {
		const parts = remainderSplit(line, 3);
		if (parts.length < 3) {
			return;
		}

		const channel = parts[2].substring(1);
		console.log(`Joined channel #${channel}`);
	}

	private async _onMODE(line: string) {
		const parts = line.split(/\s/, 5);
		if (parts.length < 5) {
			return;
		}

		const channel = parts[2].substring(1);
		const moderator = parts[3] == "+o";
		const username = parts[4];

		await Promise.all(this._onModeratorListeners.map(l => l(username, channel, moderator)));
	}

	private async _sendLineRaw(line: string, priority = false) {
		const doSend = (): Promise<void> => new Promise((resolve, reject) => {
			this._socket.addListener("error", reject);
			this._socket.addListener("close", reject);
			try {
				this._socket.write(`${line}\n`, () => {
					this._socket.removeListener("error", reject);
					this._socket.removeListener("close", reject);
					resolve();
				});
			} catch (err) {
				reject(err);
			}
		});

		while (true) {
			// remove any entries that are older than the flood span
			this._floodQueue = this._floodQueue.filter(x => process.hrtime()[0] - x < TwitchClient._floodSpan);

			// if the queue is full, wait one second then try again
			if (this._floodQueue.length >= (priority ? TwitchClient._floodMaxPriority : TwitchClient._floodMax)) {
				await new Promise(resolve => setTimeout(resolve, 1000));
				continue;
			}

			break;
		}

		this._floodQueue = [...this._floodQueue, process.hrtime()[0]];
		await doSend();
	}

}

export default TwitchClient;