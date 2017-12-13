import MessageHandler from "../messageHandler";
import ExtendedInfo from "../extendedInfo";
import TwitchExtendedInfo from "../extendedInfos/twitchExtendedInfo";
import MessageFloodgate from "../messageFloodgate";

class TrueHit implements MessageHandler {

	async handleMessage(responder: (content: string) => Promise<void>, content: string, info: ExtendedInfo | undefined) {
		let args = content.split(" ");
		if (args[0] != "!truehit")
			return;
		if (args.length == 2 || args.length == 3) {		
			const readHit = Number(args[1]);
			if (Number.isInteger(readHit) && readHit >= 0 && readHit <= 100) {
				let trueHit: number;
				if (readHit == 69) {
					trueHit = 10000;
				} else if (readHit <= 50) {
					trueHit = readHit * (readHit * 2 + 1);
				} else {
					trueHit = -2 * readHit * readHit + 399 * readHit - 9900
				}
				trueHit = trueHit / 100;
				if (args.length == 2) {
					responder(`${readHit} Hit = ${trueHit.toString()}% True Hit`);
				} else {
					const readCrit = Number(args[2]);
					if (Number.isInteger(readCrit) && readCrit >= 0 && readCrit <= 100) {
						const trueCrit = trueHit * readCrit / 100;
						responder(`${readHit} Hit ${readCrit} Crit = ${trueHit.toString()}% True Hit, ${trueCrit.toString()}% True Crit`);
					} else {
						responder(`Invalid displayed crit.`);
					}
				}
			} else {
				responder("Invalid displayed hit.");
			}
		} else {
			responder("Usage: !truehit displayed_hit [displayed_crit]");
		}
	}

}

export default TrueHit;