import UserBaseExtendedInfo from "./userBaseExtendedInfo";
import * as Discord from "discord.js";

export default interface DiscordExtendedInfo extends UserBaseExtendedInfo {
	message: Discord.Message;
}