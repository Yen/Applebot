import ExtendedInfo from "./extendedInfo";

export default interface MessageHandler {

    handleMessage(responder: (content: string) => Promise<void>, content: string, info: ExtendedInfo | undefined): Promise<void>;

}