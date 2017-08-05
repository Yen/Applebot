import ExtendedInfo from "./extendedInfo";

export default interface TwitchExtendedInfo extends ExtendedInfo {
	clientUsername: string;

	username: string;
	channel: string;
	moderator: boolean;

	sendMessage: (clientUsername: string, channel: string, content: string) => Promise<void>;
}