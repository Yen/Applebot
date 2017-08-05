import MessageHandler from "../messageHandler";
import * as os from "os";

class ApplebotInfoCommand implements MessageHandler {

	async handleMessage(responder: (content: string) => Promise<void>, content: string) {
		if (/^!applebot_info$/.test(content)) {
			await responder(`System -> ${os.platform()} ${os.release()}`);
		}
	}

}

export default ApplebotInfoCommand;