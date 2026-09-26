<script setup lang="ts">
import { Icon } from '@iconify/vue';
import { computed, onMounted, onUnmounted, ref } from 'vue';
import Btn from '~/components/Button/Btn.vue';
import Textbox from '~/components/Input/Textbox.vue';
import { useSipPhone, type PhoneCredentials } from '~/composables/useSipPhone';
import { useTranslation } from '~/composables/useTranslation';
import { audioLocked, playDtmf, unlockAudio } from '~/helper/tones';

const { t } = useTranslation();
const phone = useSipPhone();

const credentialsKey = 'echopbx_phone';
const secureUrl = `https://${window.location.hostname}:8741/phone`;
const isSecure = window.isSecureContext;

const credentials = ref<PhoneCredentials | null>(readCredentials());
const extensionNumber = ref('');
const password = ref('');
const loginError = ref('');
const loading = ref(false);
const number = ref('');
const now = ref(Date.now());

const keys: [string, string][] = [
    ['1', ''], ['2', 'ABC'], ['3', 'DEF'],
    ['4', 'GHI'], ['5', 'JKL'], ['6', 'MNO'],
    ['7', 'PQRS'], ['8', 'TUV'], ['9', 'WXYZ'],
    ['*', ''], ['0', '+'], ['#', ''],
];

const statusColor = computed(() => ({
    online: 'bg-green-500',
    connecting: 'bg-yellow-400',
    failed: 'bg-red-500',
    offline: 'bg-gray-400',
})[phone.registration.value]);

const duration = computed(() => {
    if (!phone.callStartedAt.value) return '';
    const seconds = Math.max(0, Math.floor((now.value - phone.callStartedAt.value) / 1000));
    return `${Math.floor(seconds / 60)}:${(seconds % 60).toString().padStart(2, '0')}`;
});

const callStatus = computed(() => {
    if (phone.held.value) return t('phone.on-hold');
    return {
        idle: phone.lastError.value,
        incoming: t('phone.incoming'),
        outgoing: t('phone.calling'),
        active: duration.value,
    }[phone.callState.value];
});

// The password is kept per window, so it survives a reload but not closing the phone
function readCredentials(): PhoneCredentials | null {
    try {
        const stored = sessionStorage.getItem(credentialsKey);
        return stored ? JSON.parse(stored) : null;
    } catch {
        return null;
    }
}

async function login() {
    loading.value = true;
    loginError.value = '';
    const response = await fetch('/api/auth/phone/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ extensionNumber: parseInt(extensionNumber.value), password: password.value }),
    });
    loading.value = false;

    if (response.status === 401) {
        loginError.value = t('phone.invalid-login');
        return;
    }
    if (!response.ok) {
        loginError.value = await response.text() || t('message.something-went-wrong');
        return;
    }

    const body = await response.json();
    credentials.value = { extensionNumber: body.extensionNumber, displayName: body.displayName, password: password.value };
    try {
        sessionStorage.setItem(credentialsKey, JSON.stringify(credentials.value));
    } catch {
        // Without storage the phone still works, it just asks again after a reload
    }

    // Asked now, while the click still counts as a user gesture
    if ('Notification' in window && Notification.permission === 'default') {
        Notification.requestPermission();
    }

    password.value = '';
    phone.start(credentials.value);
}

function logout() {
    phone.stop();
    credentials.value = null;
    try {
        sessionStorage.removeItem(credentialsKey);
    } catch {
        // Nothing stored, nothing to remove
    }
}

function press(key: string) {
    playDtmf(key);
    if (phone.callState.value === 'active') phone.sendDtmf(key);
    else number.value += key;
}

function callOrAnswer() {
    if (phone.callState.value === 'incoming') phone.answer();
    else if (phone.callState.value === 'idle') phone.call(number.value.trim());
}

function onKeydown(event: KeyboardEvent) {
    if (!credentials.value || event.ctrlKey || event.metaKey || event.altKey) return;

    // The number input handles its own typing, it only needs the tone
    const typing = event.target instanceof HTMLInputElement && phone.callState.value === 'idle';
    if (/^[0-9*#]$/.test(event.key) && typing) {
        if (!event.repeat) playDtmf(event.key);
        return;
    }

    if (/^[0-9*#]$/.test(event.key)) press(event.key);
    else if (event.key === 'Backspace' && !typing) number.value = number.value.slice(0, -1);
    else if (event.key === 'Enter') callOrAnswer();
    else if (event.key === 'Escape') phone.hangup();
    else return;

    event.preventDefault();
}

function onBeforeUnload(event: BeforeUnloadEvent) {
    if (phone.callState.value === 'idle') return;
    event.preventDefault();
    event.returnValue = t('phone.leave-warning');
}

let timer: number | undefined;
let manifest: HTMLLinkElement | undefined;
const previousTitle = document.title;

onMounted(() => {
    document.title = t('phone.title');

    // Only the phone is installable as an app, not the admin panel
    manifest = document.createElement('link');
    manifest.rel = 'manifest';
    manifest.href = '/phone.webmanifest';
    document.head.appendChild(manifest);

    window.addEventListener('keydown', onKeydown);
    window.addEventListener('pointerdown', unlockAudio);
    window.addEventListener('keydown', unlockAudio);
    window.addEventListener('beforeunload', onBeforeUnload);
    timer = window.setInterval(() => now.value = Date.now(), 1000);

    if (credentials.value && isSecure) phone.start(credentials.value);
});

onUnmounted(() => {
    document.title = previousTitle;
    manifest?.remove();
    window.removeEventListener('keydown', onKeydown);
    window.removeEventListener('pointerdown', unlockAudio);
    window.removeEventListener('keydown', unlockAudio);
    window.removeEventListener('beforeunload', onBeforeUnload);
    window.clearInterval(timer);
    phone.stop();
});
</script>

<template>
    <div class="min-h-screen bg-gray-100 flex justify-center select-none">
        <div class="w-full max-w-80 flex flex-col bg-white">
            <!-- Not secure: browsers refuse microphone access -->
            <div v-if="!isSecure" class="p-4 space-y-4 text-sm">
                <img src="/echopbx.svg" class="size-16 mx-auto">
                <div class="bg-yellow-100 border border-yellow-400 text-yellow-800 px-3 py-2 rounded flex gap-2">
                    <Icon icon="mdi:lock-alert-outline" class="size-5 flex-none" />
                    {{ t('phone.https-required') }}
                </div>
                <a :href="secureUrl" class="block text-center text-sky-600 hover:underline">{{ t('phone.open-secure') }}</a>
            </div>

            <!-- Login -->
            <form v-else-if="!credentials" @submit.prevent="login" class="p-4 space-y-4 text-sm">
                <img src="/echopbx.svg" class="size-16 mx-auto">
                <div class="text-gray-600">{{ t('phone.intro') }}</div>

                <div v-if="loginError" class="bg-red-100 border border-red-400 text-red-700 px-3 py-2 rounded flex gap-2 items-center">
                    <Icon icon="mdi:alert-circle-outline" class="size-5 flex-none" />
                    {{ loginError }}
                </div>

                <div class="space-y-1">
                    <div class="font-semibold">{{ t('label.extension-number') }}:</div>
                    <Textbox v-model="extensionNumber" type="text" :required="true" />
                </div>

                <div class="space-y-1">
                    <div class="font-semibold">{{ t('label.password') }}:</div>
                    <Textbox v-model="password" type="password" :required="true" />
                </div>

                <Btn type="submit" design="primary" :label="t('button.login')" :loading="loading" />
            </form>

            <!-- Phone -->
            <template v-else>
                <div class="p-2 space-y-2 flex-1 flex flex-col">
                    <div class="flex h-10 border border-gray-300 rounded focus-within:border-sky-600">
                        <input v-model="number" type="tel" class="flex-1 min-w-0 px-2 text-lg outline-none bg-transparent" :readonly="phone.callState.value !== 'idle'" />
                        <button v-if="number && phone.callState.value === 'idle'" type="button" class="px-2 text-gray-500 hover:text-gray-800" @click="number = number.slice(0, -1)">
                            <Icon icon="mdi:backspace-outline" class="size-5" />
                        </button>
                    </div>

                    <div class="h-10 text-center leading-tight">
                        <div class="font-semibold truncate">{{ phone.remoteParty.value }}</div>
                        <div class="text-sm" :class="phone.callState.value === 'idle' ? 'text-red-600' : 'text-gray-500'">{{ callStatus }}</div>
                    </div>

                    <div class="grid grid-cols-3 gap-1">
                        <button v-for="[key, letters] in keys" :key="key" type="button"
                            class="h-14 border border-gray-200 rounded bg-gray-50 hover:bg-gray-100 active:bg-gray-200 flex flex-col items-center justify-center"
                            @click="press(key)">
                            <span class="text-xl leading-6">{{ key }}</span>
                            <!-- Always takes up its line, so every digit sits at the same height -->
                            <span class="text-[10px] leading-3 h-3 tracking-wider text-gray-500">{{ letters }}</span>
                        </button>
                    </div>

                    <div class="flex gap-1">
                        <template v-if="phone.callState.value === 'incoming'">
                            <button type="button" class="flex-1 h-11 rounded bg-green-600 hover:bg-green-700 text-white flex items-center justify-center gap-2" @click="phone.answer()">
                                <Icon icon="mdi:phone" class="size-5" /> {{ t('phone.answer') }}
                            </button>
                            <button type="button" class="flex-1 h-11 rounded bg-red-600 hover:bg-red-700 text-white flex items-center justify-center gap-2" @click="phone.hangup()">
                                <Icon icon="mdi:phone-hangup" class="size-5" /> {{ t('phone.reject') }}
                            </button>
                        </template>
                        <button v-else-if="phone.callState.value !== 'idle'" type="button" class="flex-1 h-11 rounded bg-red-600 hover:bg-red-700 text-white flex items-center justify-center gap-2" @click="phone.hangup()">
                            <Icon icon="mdi:phone-hangup" class="size-5" /> {{ t('phone.hangup') }}
                        </button>
                        <button v-else type="button" class="flex-1 h-11 rounded bg-green-600 hover:bg-green-700 text-white flex items-center justify-center gap-2 disabled:opacity-50"
                            :disabled="!number.trim() || phone.registration.value !== 'online'" @click="callOrAnswer()">
                            <Icon icon="mdi:phone" class="size-5" /> {{ t('phone.call') }}
                        </button>
                    </div>

                    <div class="grid grid-cols-3 gap-1">
                        <button type="button" class="h-8 border rounded flex items-center justify-center disabled:opacity-50" :disabled="phone.callState.value !== 'active'"
                            :class="phone.muted.value ? 'bg-sky-600 border-sky-600 text-white' : 'border-gray-300 hover:bg-gray-50'"
                            :title="t('phone.mute')" :aria-label="t('phone.mute')" :aria-pressed="phone.muted.value" @click="phone.toggleMute()">
                            <Icon :icon="phone.muted.value ? 'mdi:microphone-off' : 'mdi:microphone'" class="size-5" />
                        </button>
                        <button type="button" class="h-8 border rounded flex items-center justify-center disabled:opacity-50" :disabled="phone.callState.value !== 'active'"
                            :class="phone.held.value ? 'bg-sky-600 border-sky-600 text-white' : 'border-gray-300 hover:bg-gray-50'"
                            :title="t('phone.hold')" :aria-label="t('phone.hold')" :aria-pressed="phone.held.value" @click="phone.toggleHold()">
                            <Icon :icon="phone.held.value ? 'mdi:play' : 'mdi:pause'" class="size-5" />
                        </button>
                        <button type="button" class="h-8 border rounded flex items-center justify-center"
                            :class="phone.dnd.value ? 'bg-red-600 border-red-600 text-white' : 'border-gray-300 hover:bg-gray-50'"
                            :title="t('phone.dnd')" :aria-label="t('phone.dnd')" :aria-pressed="phone.dnd.value" @click="phone.dnd.value = !phone.dnd.value">
                            <Icon :icon="phone.dnd.value ? 'mdi:bell-off' : 'mdi:bell-outline'" class="size-5" />
                        </button>
                    </div>
                </div>

                <div class="flex items-center gap-2 px-2 h-7 border-t border-gray-200 bg-gray-50 text-xs">
                    <span class="size-2.5 rounded-full" :class="statusColor"></span>
                    <span class="flex-1 truncate">{{ t(`phone.status-${phone.registration.value}`) }}</span>
                    <Icon v-if="audioLocked" icon="mdi:volume-off" class="size-4 text-orange-500" :title="t('phone.sound-locked')" />
                    <span class="text-gray-500">{{ credentials.extensionNumber }}</span>
                    <button type="button" class="text-gray-500 hover:text-gray-800" :title="t('button.logout')" @click="logout()">
                        <Icon icon="mdi:logout" class="size-4" />
                    </button>
                </div>
            </template>
        </div>
    </div>
</template>
