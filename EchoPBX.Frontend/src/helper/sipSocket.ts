import { WebSocketInterface } from 'jssip';

// A host JsSIP can parse: an IP address, or a hostname whose last label starts with a letter
const validHost = /^(\d{1,3}(\.\d{1,3}){3}|\[[0-9a-f:.]+\]|([a-z0-9]([a-z0-9-]*[a-z0-9])?\.)*[a-z]([a-z0-9-]*[a-z0-9])?\.?)$/i;
const sipUriHost = /(sips?:(?:[^@\s<>;]*@)?)([^:;>\s,]+)/gi;

/**
 * A WebSocket for JsSIP that repairs the SIP URIs in the headers of incoming messages.
 *
 * Asterisk puts the machine's hostname in the Contact and From of requests it sends over a WebSocket.
 * When that hostname is not valid in SIP, such as a Docker container ID ("9fe551a47828"),
 * JsSIP drops the whole message and incoming calls never ring. Everything goes over this one
 * WebSocket anyway, so the host is swapped for the one the page was loaded from.
 */
export function createSipSocket(url: string) {
    const socket = new WebSocketInterface(url);

    let handler: (data: unknown) => void = () => {};
    Object.defineProperty(socket, 'ondata', {
        get: () => handler,
        set: (next: (data: unknown) => void) => {
            handler = (data) => next(typeof data === 'string' ? fixHosts(data) : data);
        },
    });

    return socket;
}

function fixHosts(message: string) {
    // Only the headers: the SDP body has no SIP URIs, and must be left exactly as it is
    const bodyStart = message.indexOf('\r\n\r\n');
    const headers = bodyStart === -1 ? message : message.slice(0, bodyStart);
    const body = bodyStart === -1 ? '' : message.slice(bodyStart);

    return headers.replace(sipUriHost, (match, prefix: string, host: string) =>
        validHost.test(host) ? match : prefix + window.location.hostname) + body;
}
