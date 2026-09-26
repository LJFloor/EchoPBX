import { ref } from 'vue';

let context: AudioContext | undefined;

/**
 * Whether the browser still blocks sound. Browsers only allow audio after the user has
 * clicked or typed on the page, and an incoming call does not count.
 */
export const audioLocked = ref(true);

function audio() {
    if (!context) {
        context = new AudioContext();
        context.onstatechange = () => audioLocked.value = context!.state !== 'running';
    }

    if (context.state === 'suspended') context.resume();
    audioLocked.value = context.state !== 'running';
    return context;
}

/**
 * Unlock sound. Call from a click or key press, the only moments browsers allow it.
 */
export function unlockAudio() {
    audio();
}

/**
 * Schedule a tone of one or more mixed frequencies, with short fades so it does not click.
 */
function tone(output: AudioNode, frequencies: number[], start: number, duration: number, volume = 0.08) {
    const ctx = output.context;
    const gain = ctx.createGain();
    gain.gain.setValueAtTime(0, start);
    gain.gain.linearRampToValueAtTime(volume, start + 0.01);
    gain.gain.setValueAtTime(volume, start + duration - 0.01);
    gain.gain.linearRampToValueAtTime(0, start + duration);
    gain.connect(output);

    for (const frequency of frequencies) {
        const oscillator = ctx.createOscillator();
        oscillator.frequency.value = frequency;
        oscillator.connect(gain);
        oscillator.start(start);
        oscillator.stop(start + duration);
    }
}

/**
 * A repeating tone. All cycles are scheduled up front instead of with timers, since browsers
 * slow timers down in background windows, which is exactly where a ringing phone usually is.
 */
class Cadence {
    private output: GainNode | undefined;

    constructor(private cycle: (output: AudioNode, start: number) => void, private period: number, private repeat: number) {
    }

    start() {
        if (this.output) return;

        const ctx = audio();
        this.output = ctx.createGain();
        this.output.connect(ctx.destination);
        for (let i = 0; i < this.repeat; i++) {
            this.cycle(this.output, ctx.currentTime + i * this.period);
        }
    }

    stop() {
        this.output?.disconnect();
        this.output = undefined;
    }
}

/**
 * A classic trilling phone bell: two fast warbles, then a pause. Rings for up to two minutes.
 */
export const ringtone = new Cadence((output, start) => {
    for (const burst of [0, 0.6]) {
        for (let i = 0; i < 10; i++) {
            tone(output, [i % 2 ? 1250 : 1000], start + burst + i * 0.04, 0.04, 0.12);
        }
    }
}, 3, 40);

/**
 * The ringback tone heard while the other side rings, in the cadence of the user's country:
 * the US and Canada, the UK, or the European standard that most of the world uses.
 */
export const ringback = (() => {
    const region = navigator.language.split('-')[1]?.toUpperCase();
    if (region === 'US' || region === 'CA') {
        return new Cadence((output, start) => tone(output, [440, 480], start, 2), 6, 30);
    }

    if (region === 'GB') {
        return new Cadence((output, start) => {
            tone(output, [400, 450], start, 0.4);
            tone(output, [400, 450], start + 0.6, 0.4);
        }, 3, 60);
    }

    return new Cadence((output, start) => tone(output, [425], start, 1), 5, 36);
})();

// The standard DTMF frequency pair of every key: one from its row, one from its column
const dtmfFrequencies: Record<string, [number, number]> = {
    '1': [697, 1209], '2': [697, 1336], '3': [697, 1477],
    '4': [770, 1209], '5': [770, 1336], '6': [770, 1477],
    '7': [852, 1209], '8': [852, 1336], '9': [852, 1477],
    '*': [941, 1209], '0': [941, 1336], '#': [941, 1477],
};

/**
 * Play the tone of a keypad key.
 */
export function playDtmf(key: string) {
    const frequencies = dtmfFrequencies[key];
    if (!frequencies) return;

    const ctx = audio();
    tone(ctx.destination, frequencies, ctx.currentTime, 0.12);
}
