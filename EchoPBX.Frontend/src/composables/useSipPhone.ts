import { ref, shallowRef } from 'vue';
import { UA } from 'jssip';
import type { OutgoingEvent, RTCSession } from 'jssip/lib/RTCSession';
import type { RTCSessionEvent } from 'jssip/lib/UA';
import { ringback, ringtone } from '~/helper/tones';
import { createSipSocket } from '~/helper/sipSocket';

export type RegistrationState = 'offline' | 'connecting' | 'online' | 'failed';
export type CallState = 'idle' | 'incoming' | 'outgoing' | 'active';

export interface PhoneCredentials {
    extensionNumber: number;
    password: string;
    displayName: string | null;
}

// One phone per window, shared by every component that uses it
const registration = ref<RegistrationState>('offline');
const callState = ref<CallState>('idle');
const remoteParty = ref('');
const muted = ref(false);
const held = ref(false);
const dnd = ref(false);
const callStartedAt = ref<number | null>(null);
const lastError = ref(''); // Why the last call failed, e.g. "Busy" or "User Denied Media Access"

let ua: UA | undefined;
const session = shallowRef<RTCSession>();
const remoteAudio = new Audio();
remoteAudio.autoplay = true;

const mediaConstraints = { audio: true, video: false };

// Media only goes between the browser and the PBX, so host candidates are all ICE needs
const pcConfig: RTCConfiguration = { iceServers: [] };

/**
 * Wraps a JsSIP user agent that registers as the webphone endpoint of an extension.
 */
export function useSipPhone() {
    function start(credentials: PhoneCredentials) {
        stop();

        const host = window.location.hostname;
        // Same origin as the page, so it works behind a reverse proxy and on http://localhost too
        const socketProtocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        const socket = createSipSocket(`${socketProtocol}//${window.location.host}/api/ws`);
        ua = new UA({
            sockets: [socket],
            uri: `sip:web-${credentials.extensionNumber}@${host}`,
            authorization_user: credentials.extensionNumber.toString(),
            password: credentials.password,
            display_name: credentials.displayName ?? undefined,
            register: true,
            session_timers: false,
            user_agent: 'EchoPBX Webphone',
        });

        registration.value = 'connecting';
        ua.on('registered', () => registration.value = 'online');
        ua.on('unregistered', () => registration.value = 'offline');
        ua.on('registrationFailed', () => registration.value = 'failed');
        ua.on('disconnected', () => registration.value = 'connecting');
        ua.on('newRTCSession', ({ session: newSession, originator }: RTCSessionEvent) => {
            if (originator === 'remote') {
                // No call waiting yet: a second call gets a busy signal, just like DND
                if (dnd.value || session.value) {
                    newSession.terminate({ status_code: 486, reason_phrase: 'Busy Here' });
                    return;
                }

                attach(newSession, 'incoming');
                ringtone.start();
                notifyIncoming();
            } else {
                attach(newSession, 'outgoing');
            }
        });

        ua.start();
    }

    function stop() {
        session.value?.terminate();
        ua?.stop();
        ua = undefined;
        registration.value = 'offline';
    }

    function call(number: string) {
        if (!ua || session.value || !number) return;

        lastError.value = '';
        ua.call(`sip:${number}@${window.location.hostname}`, { mediaConstraints, pcConfig });
        remoteParty.value = number;
    }

    function answer() {
        if (callState.value !== 'incoming') return;

        ringtone.stop();
        session.value?.answer({ mediaConstraints, pcConfig });
    }

    function hangup() {
        session.value?.terminate();
    }

    function toggleMute() {
        if (!session.value) return;

        if (muted.value) session.value.unmute({ audio: true });
        else session.value.mute({ audio: true });
        muted.value = !muted.value;
    }

    function toggleHold() {
        if (!session.value || callState.value !== 'active') return;

        if (held.value) session.value.unhold();
        else session.value.hold();
        held.value = !held.value;
    }

    function sendDtmf(tone: string) {
        if (callState.value === 'active') session.value?.sendDTMF(tone);
    }

    return {
        registration, callState, remoteParty, muted, held, dnd, callStartedAt, lastError,
        start, stop, call, answer, hangup, toggleMute, toggleHold, sendDtmf,
    };
}

function attach(newSession: RTCSession, state: CallState) {
    session.value = newSession;
    callState.value = state;
    remoteParty.value = newSession.remote_identity.display_name || newSession.remote_identity.uri.user;

    // An outgoing session already has its connection, an incoming one gets it when answered
    if (newSession.connection) playRemoteAudio(newSession.connection);
    newSession.on('peerconnection', ({ peerconnection }) => playRemoteAudio(peerconnection));

    // Do not wait for every candidate: on a LAN the first ones are enough, and waiting for
    // unreachable interfaces can delay the call by seconds
    let iceTimeout: number | undefined;
    newSession.on('icecandidate', ({ ready }) => {
        window.clearTimeout(iceTimeout);
        iceTimeout = window.setTimeout(ready, 500);
    });

    // Play our own ringback, unless the other side sends its own audio (early media)
    newSession.on('progress', ({ response }: OutgoingEvent) => {
        if (newSession.direction !== 'outgoing') return;
        if (response.status_code === 180 && !response.body) ringback.start();
        else if (response.body) ringback.stop();
    });

    newSession.on('confirmed', () => {
        ringtone.stop();
        ringback.stop();
        callState.value = 'active';
        callStartedAt.value = Date.now();
    });
    newSession.on('ended', reset);
    newSession.on('failed', ({ cause }) => {
        // A canceled call was hung up on purpose, by either side, so there is nothing to explain
        if (cause !== 'Canceled') lastError.value = cause;
        reset();
    });
}

function playRemoteAudio(connection: RTCPeerConnection) {
    connection.addEventListener('track', (event) => {
        remoteAudio.srcObject = event.streams[0] ?? new MediaStream([event.track]);
    });
}

function reset() {
    ringtone.stop();
    ringback.stop();
    session.value = undefined;
    callState.value = 'idle';
    remoteParty.value = '';
    muted.value = false;
    held.value = false;
    callStartedAt.value = null;
    remoteAudio.srcObject = null;
}

function notifyIncoming() {
    if (!('Notification' in window) || Notification.permission !== 'granted' || document.hasFocus()) return;

    const notification = new Notification('EchoPBX', { body: remoteParty.value, icon: '/echopbx.svg', tag: 'incoming-call' });
    notification.onclick = () => window.focus();
}
