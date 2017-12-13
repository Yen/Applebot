export default interface PersistentService {

	backendInitialized(type: string, backend: any): Promise<void>;

}