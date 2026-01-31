<script setup lang="ts">
import { Icon } from '@iconify/vue';
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import Btn from '~/components/Button/Btn.vue';
import Textbox from '~/components/Input/Textbox.vue';

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
        error.value = 'Invalid username or password.';
    } else {
        error.value = await response.text() || 'An unknown error occurred.';
    }
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
                    <div class="text-lg font-semibold">Welcome!</div>
                    <div class="text-sm text-gray-600">Enter your username and password to sign in to the administration panel.</div>
                </div>

                <div class="space-y-2">
                    <div class="font-semibold">Username:</div>
                    <Textbox v-model="username" type="text" :required="true" />
                </div>

                <div class="space-y-2">
                    <div class="font-semibold">Password:</div>
                    <Textbox v-model="password" type="password" :required="true" />
                </div>

                <Btn type="submit" design="primary" :label="'Sign In'" :loading="loading" />
            </form>
        </div>

        <div>
            <p class="text-center text-sm text-gray-500">© 2025 EchoPBX. All rights reserved.</p>
        </div>
    </div>
</template>