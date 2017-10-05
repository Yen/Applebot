import MessageHandler from "../messageHandler";
import ExtendedInfo from "../extendedInfo";
import TwitchExtendedInfo from "../extendedInfos/twitchExtendedInfo";
import DiscordExtendedInfo from "../extendedInfos/discordExtendedInfo";
import * as fs from "fs";

interface Pattern {
	trigger: string;
	response: string;
}

function readPatternFile(): Promise<string | undefined> {
	return new Promise((resolve, reject) => {
		fs.readFile("resources/dynamicResponse.json", "utf8", (err, data) => {
			if (err) {
				if (err.code === "ENOENT") {
					resolve();
				} else {
					reject(err);
				}
			} else {
				resolve(data);
			}
		})
	});
}

function writePatternFile(patterns: Pattern[]): Promise<string> {
	return new Promise((resolve, reject) => {
		const content = JSON.stringify(patterns);
		fs.writeFile("resources/dynamicResponse.json", content, "utf8", function (err) {
			if (err) {
				reject(err);
			} else {
				resolve();
			}
		})
	});
}

class DynamicResponse implements MessageHandler {

	private _patterns: Pattern[];

	private constructor(patterns: Pattern[]) {
		this._patterns = patterns;
	}

	public static async create() {
		const data = await readPatternFile() || "[]";
		const patterns = JSON.parse(data) as Pattern[];
		return new DynamicResponse(patterns);
	}

	async handleMessage(responder: (content: string) => Promise<void>, content: string, info: ExtendedInfo | undefined) {
		let args = content.split(" ");
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

		if (args[0][0] != "!") {
			return;
		}

		const target = this._patterns.findIndex(x => x.trigger == args[0].substring(1))
		if (target != -1) {
			await responder(this._patterns[target].response);
		}

		if (elevated == false) {
			return;
		}

		if (args[0] == "!dynamic") {
			if (args.length == 1) {
				await responder('Usage: "dynamic [add|remove|list]"');
				return;
			}
			switch (args[1].toLowerCase()) {
				case "add": {
					if (args.length > 3) {
						const duplicates = this._patterns.filter(x => x.trigger == args[2]);
						const responseText = args.slice(3).join(" ");
						const newPattern = {trigger: args[2], response: responseText};
						this._patterns.push(newPattern);
						await writePatternFile(this._patterns);
						if (duplicates.length > 0) {
							await responder(`Replaced command "!${args[2]}".`);
						} else {
							await responder(`Added command !${args[2]}.`);
						}
					} else {
						await responder('Usage: "!dynamic add [trigger] [response]"');
					}
					break;
				}
				case "remove": {
					if (args.length > 2) {
						if (this._patterns.filter(x => x.trigger == args[2]).length > 0) {
							this._patterns = this._patterns.filter(x => x.trigger != args[2]);
							await writePatternFile(this._patterns);
							await responder(`Command !${args[2]} removed.`);
						} else {
							await responder("No command with that trigger exists.");
						}
					} else {
						await responder('Usage: "!dynamic remove [trigger]"');
					}
					break;
				}
				case "list": {
					const commandList = this._patterns.map(x => x.trigger).join(" | ");
					await responder(commandList);
				}
			}
		}
	}
}

export default DynamicResponse;