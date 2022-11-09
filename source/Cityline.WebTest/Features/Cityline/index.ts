import { UrlProvider } from "../../UrlProvider";

import { CitylineClient } from "./CitylineClient";

export class CitylineFactory {
    private static _client: CitylineClient;
    get client() {
        return CitylineFactory._client || (CitylineFactory._client = new CitylineClient(() => ({
            apiEndpoint: `${UrlProvider.root}/cityline`,
            socketEndpoint: `${UrlProvider.root}/cityline`.replace("https", "wss")
        })));
    }
}

var citylineFactory = new CitylineFactory();
export { citylineFactory };

citylineFactory.client.start();
