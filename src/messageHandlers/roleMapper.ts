import * as fs from "fs";
import PersistentService from "../persistentService";
import * as Discord from "discord.js";
import { setTimeout } from "timers";

interface MappingInfo {
	guild: string;
	roles: string[];
	target: string;
}

export default class RoleMapper implements PersistentService {

	private _settings: MappingInfo[];

	private constructor(settings: MappingInfo[]) {
		this._settings = settings;
	}

	static async create(): Promise<RoleMapper> {
		const readTask = new Promise<string>((resolve, reject) => fs.readFile("./resources/roleMapper.json", "utf8", (err, data) => {
			if (err) {
				reject(err);
			} else {
				resolve(data);
			}
		}));
		const info = JSON.parse(await readTask) as MappingInfo[];
		return new RoleMapper(info);
	}

	async backendInitialized(type: string, backend: any) {
		if (type != "DISCORD") {
			return;
		}
		if (this._settings.length == 0) {
			return;
		}

		setInterval(() => this._tick(backend).catch(console.error), 10000);
	}

	private async _tick(client: Discord.Client) {
		for (const info of this._settings) {

			const guild = client.guilds.get(info.guild);
			if (guild == undefined) {
				continue;
			}

			const target = guild.roles.get(info.target);
			if (target == undefined) {
				continue;
			}

			const roles = guild.roles.filter(r => info.roles.includes(r.id));
			const members = roles.reduce<Discord.Collection<string, Discord.GuildMember>>(
				(acc, r) => acc.concat(r.members),
				new Discord.Collection()
			);

			const toAdd = members.filter(m => !target.members.has(m.id));
			const toRemove = target.members.filter(m => !members.has(m.id));

			for (const m of toAdd.array()) {
				m.addRole(target);
			}

			for (const m of toRemove.array()) {
				m.removeRole(target);
			}
		}
	}

}