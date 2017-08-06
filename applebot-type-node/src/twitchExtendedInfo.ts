import ExtendedInfo from "./extendedInfo";

export default interface TwitchExtendedInfo extends ExtendedInfo {
	username: string;
	channel: string;
	moderator: boolean;

	sendMessage: (channel: string, content: string) => Promise<void>;
}