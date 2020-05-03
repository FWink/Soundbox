/**
 * Provides persistent key-value storage
 * */
export abstract class StorageProvider {
    /**
     * Fetches the stored value for the given key. Returns null if there is no such mapping.
     * @param key
     */
    public abstract get(key: string): Promise<any>;
    /**
     * Stores (that is, inserts or updates) the given key-value pair.
     * @param key
     * @param value
     * @returns Promise that resolves once the value is savely stored.
     */
    public abstract set(key: string, value: any): Promise<any>;
    /**
     * Removes the key with its value from the storage.
     * @param key
     * @returns Promise that resolves once the value is savely removed.
     */
    public abstract delete(key: string): Promise<any>;
}