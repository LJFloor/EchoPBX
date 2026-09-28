<script setup lang="ts">
import { Icon } from '@iconify/vue';
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import Btn from '~/components/Button/Btn.vue';
import Textbox from '~/components/Input/Textbox.vue';
import { useTranslation } from '~/composables/useTranslation';

const { t } = useTranslation();

const router = useRouter();

const error = ref('');
const username = ref('');
const password = ref('');
const loading = ref(false);

async function login() {
    loading.value = true;
    const response = await fetch('/api/auth/admin/login', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            username: username.value,
            password: password.value
        })
    });
    loading.value = false;

    if (response.ok) {
        router.push('/admin/dashboard');
    } else if (response.status === 401) {
        error.value = t('message.invalid-login');
    } else {
        error.value = await response.text() || t('message.something-went-wrong');
    }
}

const phoneUrl = window.isSecureContext ? '/phone' : `https://${window.location.hostname}:8741/phone`;

// Opened straight from the click handler, since browsers only allow popups during a user gesture
function openPhone() {
    // An empty URL finds the phone if it is already open, without reloading it and ending a call
    const popup = window.open('', 'echopbx-phone', 'popup,width=300,height=540');
    if (!popup) {
        window.location.href = phoneUrl;
        return;
    }

    try {
        if (popup.location.href === 'about:blank') popup.location.href = phoneUrl;
    } catch {
        // Cross-origin (the phone runs on the HTTPS port), so it is already open
    }

    popup.focus();
}

onMounted(async () => {
  const isSetupResponse = await fetch('/api/system/is-setup');
  const isSetup = await isSetupResponse.json();
  if (!isSetup) {
    window.location.href = '/setup';
  }
});
</script>

<template>
    <div class="mx-auto mt-24 w-[500px] space-y-4">
        <img src="/echopbx.svg" class="size-40 mx-auto">

        <div v-if="error" class="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded flex gap-2 items-center">
            <Icon icon="mdi:alert-circle-outline" class="inline size-5" />
            {{ error }}
        </div>
        <div class="bg-white rounded-bl rounded-br border border-gray-200 p-8">
            <form @submit.prevent="login" class="space-y-4">
                <div class="space-y-2">
                    <div class="text-lg font-semibold">{{ t('login.welcome') }}</div>
                    <div class="text-sm text-gray-600">{{ t('login.intro') }}</div>
                </div>

                <div class="space-y-2">
                    <div class="font-semibold">{{ t('label.username') }}:</div>
                    <Textbox v-model="username" type="text" :required="true" />
                </div>

                <div class="space-y-2">
                    <div class="font-semibold">{{ t('label.password') }}:</div>
                    <Textbox v-model="password" type="password" :required="true" />
                </div>

                <Btn type="submit" design="primary" :label="t('button.login')" :loading="loading" />
            </form>
        </div>

        <a :href="phoneUrl" class="flex items-center justify-center gap-1.5 text-sky-600 hover:underline" @click.prevent="openPhone">
            <Icon icon="mdi:phone" class="size-4" />
            {{ t('phone.open') }}
        </a>

        <div>
            <p class="text-center text-sm text-gray-500">© 2025 EchoPBX. All rights reserved.</p>
        </div>
    </div>
</template>