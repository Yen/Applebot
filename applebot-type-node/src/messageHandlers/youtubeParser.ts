import MessageHandler from "../messageHandler";
import ExtendedInfo from "../extendedInfo";
import { URL } from "url";
import fetch from "node-fetch";
import UserBaseExtendedInfo from "../extendedInfos/userBaseExtendedInfo";
import * as fs from "fs";

const regex = /((youtube\.com\/watch)|(youtu\.be\/))\S*/g;
const settings = JSON.parse(fs.readFileSync("resources/youtube.json", "utf8"));

export default class YoutubeParser implements MessageHandler {


	async handleMessage(responder: (content: string) => Promise<void>, content: string, info?: ExtendedInfo) {
		if (info == undefined || !(info.type == "TWITCH" || info.type == "USTREAM")) {
			return;
		}

		const matches = content.match(regex);
		if (matches == null) {
			return;
		}

		const videoIds = matches.reduce((accumulator, value) => {
			try {
				const url = new URL(`https://${value}`);
				const id = url.searchParams.get("v");
				if (id != null) {
					return [...accumulator, id];
				} else {
					return accumulator;
				}
			} catch (err) {
				if (err) {
					console.error(err);
				}
				return accumulator;
			}
		}, []);

		interface VideoInformation {
			title: string;
			channelTitle: string;
		}

		let informations: VideoInformation[] = [];

		for (const id of videoIds) {
			const queryUrl = new URL("https://www.googleapis.com/youtube/v3/videos");
			queryUrl.searchParams.set("part", "snippet");
			queryUrl.searchParams.set("key", settings.apikey);
			queryUrl.searchParams.set("id", id);

			try {
				const res = await fetch(queryUrl.toString());
				const json = await res.json();
				const snippet = json.items[0].snippet;

				const title: string = snippet.title;
				const channelTitle: string = snippet.channelTitle;

				informations = [...informations, { title, channelTitle }];
			} catch (err) {
				console.error("Error querying Youtube API");
				if (err) {
					console.error(err);
				}
			}
		}

		const userInfo = info as UserBaseExtendedInfo;
		for (const i of informations) {
			await responder(`${userInfo.username} linked a video, "${i.title}" by "${i.channelTitle}"`);
		}
	}

}