import UserBaseExtendedInfo from "./userBaseExtendedInfo";

export default interface TwitchExtendedInfo extends UserBaseExtendedInfo {
	channel: string;
	moderator: boolean;

	sendMessage: (channel: string, content: string) => Promise<void>;
}