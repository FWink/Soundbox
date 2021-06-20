import { StorageProvider } from './StorageProvider';

/**
 * A {@link StorageProvider} that uses the page's local storage.
 * */
export class DefaultStorageProvider extends StorageProvider {

    protected readonly prefix: string = "";
    protected readonly webStorage: Storage = window.localStorage;

    /**
     * 
     * @param prefix Prefix to apply to keys managed with this class.
     */
    public constructor(prefix?: string) {
        super();

        if (prefix)
            this.prefix = prefix;
    }

    public get(key: string): Promise<any> {
        key = this.prefix + key;

        return new Promise<any>((resolve, reject) => {
            const value = this.webStorage.getItem(key);

            if (value) {
                resolve(JSON.parse(value));
            }
            else {
                //no mapping
                resolve(null);
            }
        });
    }

    public set(key: string, value: any): Promise<any> {
        key = this.prefix + key;

        return new Promise<any>((resolve, reject) => {
            this.webStorage.setItem(key, JSON.stringify(value));
            resolve(undefined);
        });
    }

    public delete(key: string): Promise<any> {
        key = this.prefix + key;

        return new Promise<any>((resolve, reject) => {
            this.webStorage.removeItem(key);
            resolve(undefined);
        });
    }
}