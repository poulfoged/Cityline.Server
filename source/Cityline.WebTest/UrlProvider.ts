export class UrlProvider {
    static get root() {
        if (window.location.host.toLowerCase().endsWith("eurowheels.dk"))
            return document.baseURI;
        
        return  window.location.origin;
    }
}