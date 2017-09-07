export default interface PersistantService {

	backendInitialized(type: string, backend: any): Promise<void>;

}