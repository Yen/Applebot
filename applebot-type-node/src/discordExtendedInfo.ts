import ExtendedInfo from "./extendedInfo";
import * as Discord from "discord.js";

export default interface DiscordExtendedInfo extends ExtendedInfo {
    message: Discord.Message;
}