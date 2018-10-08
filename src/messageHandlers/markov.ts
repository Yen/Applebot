import MessageHandler from "../messageHandler";
import ExtendedInfo from "../extendedInfo";
import TwitchExtendedInfo from "../extendedInfos/twitchExtendedInfo";
import MessageFloodgate from "../messageFloodgate";
import * as sqlite from 'sqlite';
import * as Discord from "discord.js";
import DiscordExtendedInfo from "../extendedInfos/discordExtendedInfo";
var syllable = require('syllable');

class Markov implements MessageHandler {

	private _data: sqlite.Database;
	private _trainWords: sqlite.Statement;
	private _trainRelations: sqlite.Statement;
	private _getStarter: sqlite.Statement;
	private _getNext: sqlite.Statement;
	private _getWord: sqlite.Statement;

	private _wordArray: {
		[id: number]: string
	};
	private _idArray: {
		[word: string]: number
	};

	private _emotes: string[] = [":o", ":0", ":v", ":u", ":?", ":o", ":I"];
	private _names: string[] = ["applebot", "appleb0t", "<@213048998678888448>"];
	private _channels: string[] = ["spam", "shitposting", "bot-disaster"];
	private _targetLength: number = 8;

	public static async create() {
		const markov = new Markov();
		markov._data = await sqlite.open("resources/markov.db");
		markov._trainWords = await markov._data.prepare("INSERT INTO words (word) VALUES (?)");
		markov._trainRelations = await markov._data.prepare(`INSERT INTO relations (first_word_id, second_word_id, result_word_id) VALUES (?, ?, ?)`);
		markov._getStarter = await markov._data.prepare("SELECT second_word_id FROM relations WHERE first_word_id = ? ORDER BY RANDOM() LIMIT 1");
		markov._getNext = await markov._data.prepare("SELECT result_word_id FROM relations WHERE first_word_id = ? AND second_word_id = ? ORDER BY RANDOM() LIMIT 1");
		markov._getWord = await markov._data.prepare("SELECT * FROM words WHERE word = ? LIMIT 1");

		markov._wordArray = {};
		markov._idArray = {};
		for (const row of await markov._data.all("SELECT id, word FROM words")) {
			markov._wordArray[row.id] = row.word;
			markov._idArray[row.word] = row.id;
		}

		return markov;
	}

	async handleMessage(responder: (content: string) => Promise < void > , content: string, info: ExtendedInfo | undefined) {
		content = content.toLowerCase();
		var haiku = false;
		if (/^!haiku/gm)
			.test(content) {
				haiku = true;
			}
		let args = content.split(" ");

		//behavior
		let train = false;
		let called = false;
		let forceResponse = false;
		const names = args.filter(arg => !this._names.includes(arg));
		if (names.length != args.length)
			called = true;
		args = names;
		if (info != undefined) {
			if (info.type == "DISCORD") {
				train = true;
				if (called) {
					const discordInfo = info as DiscordExtendedInfo;
					const channel = discordInfo.message.channel as Discord.TextChannel;
					if (this._channels.includes(channel.name)) {
						if (args.length == 0) {
							await responder("!");
							return;
						}
						forceResponse = true;
					}
				}
			}
		}

		//drop weird message
		for (let arg of args) {
			if (/^[a-zA-Z0-9\.,!\?’'\- ]+$/.test(arg) == false) {
				if (forceResponse)
					await responder("?");
				return;
			}
		}

		//training
		if (train) {
			await Promise.all(args
				.filter(w => this._idArray[w] === undefined)
				.map(async(w) => {
					await this._trainWords.run([w]);
					const insertedWord = await this._getWord.get(w);
					this._wordArray[insertedWord.id] = w;
					this._idArray[w] = insertedWord.id;
				}));

			await this._data.run("BEGIN TRANSACTION");
			const trainingPromises = [];
			const trainingWordCache = args.map(a => this._idArray[a]);
			for (let i = 0; i < args.length - 2; i++) {
				const firstWordId = trainingWordCache[i];
				const secondWordId = trainingWordCache[i + 1];
				const resultWordId = i < args.length - 2 ? trainingWordCache[i + 2] : null;

				trainingPromises.push(this._trainRelations.run(firstWordId, secondWordId, resultWordId));
			}
			await Promise.all(trainingPromises);
			await this._data.run("COMMIT TRANSACTION");
		}

		//early out if message doesn't need to be generated
		if (!forceResponse && !haiku && Math.random() > 0.01) {
			return;
		}

		//generate candidates
		let candidates = [];
		for (let arg of args) {
			let targetID = this._idArray[arg];
			let chainQuery = await this._getStarter.get(targetID);
			if (chainQuery != undefined) {
				let sequence = [targetID, chainQuery.second_word_id];
				for (let i = 0; true; i++) {
					let nextQuery = await this._getNext.get([sequence[sequence.length - 2], sequence[sequence.length - 1]]);
					if (nextQuery == undefined || i > 20)
						break;
					sequence.push(nextQuery.result_word_id);
				}
				if (sequence[sequence.length - 1] == null)
					sequence.pop();
				let message = [];
				let line0 = [];
				let line1 = [];
				let line2 = [];
				let linelen = [0, 0, 0];
				for (let id of sequence) {
					if (haiku) {
						if (linelen[0] < 5) {
							linelen[0] += syllable(this._wordArray[id]);
							if (linelen[0] > 5) {
								linelen[0] -= syllable(this._wordArray[id]);
								continue;
							} else {
								line0.push(this._wordArray[id]);
							}
						} else if (linelen[1] < 7) {
							linelen[1] += syllable(this._wordArray[id]);
							if (linelen[1] > 7) {
								linelen[1] -= syllable(this._wordArray[id]);
								continue;
							} else {
								line1.push(this._wordArray[id]);
							}

						} else if (linelen[2] < 5) {
							linelen[2] += syllable(this._wordArray[id]);
							if (linelen[2] > 5) {
								linelen[2] -= syllable(this._wordArray[id]);
								continue;
							} else {
								line2.push(this._wordArray[id]);
							}
						} else {
							break;
						}
					} else {
						message.push(this._wordArray[id]);
					}
				}
				if (!haiku){
				candidates.push(message.join(" "));
				} else {
					message = [line0.join(" "), line1.join(" "), line2.join(" ")];
					candidates.push(message.join("\n"));
				}
			}
		}

		//strip low quality messages
		candidates = candidates.filter(c => !content.includes(c));

		if (candidates.length == 0) {
			if (forceResponse)
				await responder(":D?");
			return;
		}

		//selection

		const distances = candidates.map(c => Math.abs(this._targetLength - (c.split(" ").length - 1)));
		const maxDistance = distances.reduce(function (a, b) {
				return Math.max(a, b);
			});
		const weightedCandidates = []

		for (let i in candidates) {
			for (let j = 0; j <= maxDistance - distances[i]; j++) {
				weightedCandidates.push(candidates[i]);
			}
		}

		let response = weightedCandidates[Math.floor(Math.random() * weightedCandidates.length)]; // this might change LOL
		if (haiku) {
			var choices = weightedCandidates.map(function (item) {
					return (item.match(/[a-zA-Z]+/gm).map(function (item) {
							return syllable(item);
						})).reduce((a, b) => a + b, 0);
				});
			if (choices.indexOf(17) != -1) {
				response = weightedCandidates[choices.indexOf(17)];
			}

		}
		//cleanup
		response = response.replace(/,$/, "");
		if (/[!?\.]$/.test(response) == false)
			response = response + ".";
		response = response + " " + this._emotes[Math.floor(Math.random() * this._emotes.length)]
			await responder(response);
	}
}

export defaultMarkov;
