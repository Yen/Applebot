import MessageHandler from "../messageHandler";
import ExtendedInfo from "../extendedInfo";
import DiscordExtendedInfo from "../extendedInfos/discordExtendedInfo";
import { promisify } from "util";
import * as fs from "fs";

interface ReactionSet {
	reaction: string;
	patterns: RegExp[];
}

class DiscordRegexReactions implements MessageHandler {
	private _reactionSets: ReactionSet[];

	private constructor(reactionSets: ReactionSet[]) {
		this._reactionSets = reactionSets;
	}

	public static async create() {
		const data = await promisify(fs.readFile)("resources/discordRegexReactions.json", "utf8");
		// convert string patterns into RegExp instances
		const sets = JSON.parse(data).map((s: any) => ({
			patterns: s.patterns.map((p: any) => new RegExp(p)),
			reaction: s.reaction
		})) as ReactionSet[];
		return new DiscordRegexReactions(sets);
	}

	async handleMessage(responder: (content: string) => Promise<void>, content: string, info: ExtendedInfo | undefined) {
		if (info == undefined || info.type != "DISCORD")
			return;

		const discordInfo = info as DiscordExtendedInfo;
		const reactions = new Set<string>();

		for (const set of this._reactionSets) {
			if (set.patterns.find(p => p.test(content)) != undefined) {
				reactions.add(set.reaction);
			}
		}

		for (const reaction in reactions) {
			await discordInfo.message.react(reaction);
		}
	}

}

export default DiscordRegexReactions;