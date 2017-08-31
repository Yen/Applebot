import MessageHandler from "../messageHandler";
import ExtendedInfo from "../extendedInfo";
import TwitchExtendedInfo from "../extendedInfos/twitchExtendedInfo";
import DiscordExtendedInfo from "../extendedInfos/discordExtendedInfo";
import * as fs from "fs";

interface Quote {
	"response": string;
	"added_by": string;
}

function readQuotesFile(): Promise<string> | undefined {
	return new Promise((resolve, reject) => {
		fs.readFile("resources/quotes.json", "utf8", (err, data) => {
			if (err) {
				if (err.code === "ENOENT") {
					resolve(undefined);
				} else {
					reject(err);
				}
			} else {
				resolve(data);
			}
		})
	});
}

function writeQuotesFile(quotes: Quote[]): Promise<string> {
	const content = JSON.stringify(quotes);
	return new Promise((resolve, reject) => {
		fs.writeFile("resources/quotes.json", content, "utf8", function (err) {
			if (err) {
				reject(err);
			} else {
				resolve();
			}
		})
	});
}

class Quotes implements MessageHandler {

	private _quotes: Quote[];

	private constructor(quotes: Quote[]) {
		this._quotes = quotes;
	}

	public static async create() {
		const data = await readQuotesFile() || "[]";
		const quotes = JSON.parse(data) as Quote[];
		return new Quotes(quotes);
	}

	async handleMessage(responder: (content: string) => Promise<void>, content: string, info: ExtendedInfo | undefined) {
		let elevated = false;
		let username = "unknown";
		switch (info != undefined ? info.type : undefined) {
			case "TWITCH": {
				const twitchInfo = info as TwitchExtendedInfo;
				elevated = twitchInfo.moderator;
				username = twitchInfo.username;
				break;
			}
			case "DISCORD": {
				const discordInfo = info as DiscordExtendedInfo;
				elevated = discordInfo.message.member.hasPermission("BAN_MEMBERS");
				username = discordInfo.message.member.nickname;
				break;
			}
		}
		if (/^!quote/.test(content)) {
			let args = content.split(" ");
			if (args.length == 1) {
				if (this._quotes.length > 0) {
					const randomIndex = Math.floor(Math.random() * this._quotes.length);
					const randomQuote = this._quotes[randomIndex];
					await responder(`(#${randomIndex + 1}) ${randomQuote.response}`);
				} else {
					await responder('There are no saved quotes. Add one with "!quote add".');
				}
			} else {
				switch (args[1].toLowerCase()) {
					case "add": {
						if (!elevated) {
							break;
						}
						if (args.length >= 3) {
							const quoteText = args.slice(2).join(" ");
							const newQuote = {response: quoteText, added_by: "TODO"};
							await responder(`Added quote #${this._quotes.push(newQuote)}.`);
							await writeQuotesFile(this._quotes);
							break;
						} else {
							await responder('Usage: "!quote add [quote]"');
							break;
						}
					}
					case "remove": {
						if (!elevated) {
							break;
						}
						if (args.length >= 3) {
							const targetIndex = Number(args[2]);
							if (Number.isInteger(targetIndex) && targetIndex > 0 && targetIndex <= this._quotes.length) {
								this._quotes.splice(targetIndex - 1, 1);
								await responder(`Removed quote #${targetIndex}.`);
								break;
							} else {
								await responder("That quote doesn't exist.");
								break;
							}
						} else {
							await responder('Usage: "!quote remove [number]"');
							break;
						}
					}
					case "undo": {
						if (!elevated) {
							break;
						}
						if (this._quotes.length > 0) {
							this._quotes.pop();
							await responder(`Removed quote #${this._quotes.length + 1}.`);
							await writeQuotesFile(this._quotes);
							break;

						} else {
							await responder("There are no quotes to remove.");
							break;
						}
					}
					case "count": {
						await responder(`There are ${this._quotes.length} saved quotes.`);
						break;
					}
					default: {
						if (args[1].substring(0,1) == "#") { // specific
							const targetIndex = Number(args[1].substring(1));
							if (Number.isInteger(targetIndex) && targetIndex > 0 && targetIndex <= this._quotes.length) {
								await responder(`(#${targetIndex}) ${this._quotes[targetIndex - 1].response}`);
								break;
							} else {
								await responder("Couldn't find a quote with that index.");
								break;
							}
						} else { // search
							const target = args.slice(1).join(" ").toLowerCase();
							const matches = this._quotes.filter(x => x.response.toLowerCase().includes(target));
							if (matches.length > 0) {
								const randomIndex = Math.floor(Math.random() * matches.length);
								const randomQuote = matches[randomIndex];
								await responder(`(#${randomIndex + 1}) ${randomQuote.response}`);
							} else {
								await responder("No quotes found. For a specific quote, use the # symbol.");
								break;
							}
						}
					}
				}
			}
		}
	}
}

export default Quotes;