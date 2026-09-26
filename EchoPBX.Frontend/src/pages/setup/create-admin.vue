<script setup lang="ts">
import { Icon } from '@iconify/vue';
import { computed, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import Btn from '~/components/Button/Btn.vue';
import Textbox from '~/components/Input/Textbox.vue';
import { useLocalStorage } from '~/composables/useLocalStorage';
import SetupLayout from '~/layouts/SetupLayout.vue';
import { useTranslation } from '~/composables/useTranslation';

const { t } = useTranslation();

const router = useRouter();

const username = useLocalStorage('setup:admin:username', 'admin');
const password = useLocalStorage('setup:admin:password', '');
const confirmPassword = useLocalStorage('setup:admin:confirmPassword', '');

const isValid = ref(false);
const loading = ref(false);

const hasLetter = computed(() => /[a-zA-Z]/.test(password.value));
const hasNumber = computed(() => /[0-9]/.test(password.value));
const hasSpecialChar = computed(() => /[!@#$%^&*(),.?":{}|<>]/.test(password.value));
const isMinLength = computed(() => password.value.length >= 10);

function validate() {
    if (!username.value || !password.value || !confirmPassword.value) {
        isValid.value = false;
        return;
    }


    if (password.value !== confirmPassword.value) {
        isValid.value = false;
        return;
    }

    if (!hasLetter.value || !hasNumber.value || !hasSpecialChar.value || !isMinLength.value) {
        isValid.value = false;
        return;
    }

    isValid.value = true;
}

async function setup() {
    if (!isValid.value) {
        return;
    }

    loading.value = true;
    const setupResponse = await fetch('/api/system/setup', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            adminUsername: username.value,
            adminPassword: password.value,
        }),
    });

    if (!setupResponse.ok) {
        loading.value = false;
        const text = await setupResponse.text();
        alert(text || 'An unknown error occurred during setup.');
        return;
    }

    localStorage.removeItem('setup:admin:username');
    localStorage.removeItem('setup:admin:password');
    localStorage.removeItem('setup:admin:confirmPassword');

    const loginResponse = await fetch('/api/auth/admin/login', {
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

    if (loginResponse.ok) {
        router.push('/setup/finish');
    } else {
        router.push('/');
    }
}

watch([username, password, confirmPassword], validate, { immediate: true });
</script>

<template>
    <SetupLayout :title="t('setup.admin-title')">
        <p>
            {{ t('setup.admin-intro') }}
        </p>

        <div class="grid grid-cols-2 gap-4">
            <div>{{ t('label.username') }}:</div>
            <Textbox required v-model="username" />

            <div>{{ t('label.password') }}:</div>
            <Textbox required type="password" v-model="password" />

            <div>{{ t('password.confirm') }}:</div>
            <Textbox required type="password" v-model="confirmPassword" />

            <div></div>
            <div v-if="password">
                <ul class="list-inside">
                    <li :class="hasLetter ? 'text-green-600' : 'text-red-600'">
                        <Icon :icon="hasLetter ? 'mdi:check' : 'mdi:close'" class="inline size-4 mr-1" />
                        {{ t('password.rule-letter') }}
                    </li>
                    <li :class="hasNumber ? 'text-green-600' : 'text-red-600'">
                        <Icon :icon="hasNumber ? 'mdi:check' : 'mdi:close'" class="inline size-4 mr-1" />
                        {{ t('password.rule-number') }}
                    </li>
                    <li :class="hasSpecialChar ? 'text-green-600' : 'text-red-600'">
                        <Icon :icon="hasSpecialChar ? 'mdi:check' : 'mdi:close'" class="inline size-4 mr-1" />
                        {{ t('password.rule-special-char') }}
                    </li>
                    <li :class="isMinLength ? 'text-green-600' : 'text-red-600'">
                        <Icon :icon="isMinLength ? 'mdi:check' : 'mdi:close'" class="inline size-4 mr-1" />
                        {{ t('password.rule-min-length') }}
                    </li>
                    <li :class="password === confirmPassword ? 'text-green-600' : 'text-red-600'">
                        <Icon :icon="password === confirmPassword ? 'mdi:check' : 'mdi:close'"
                            class="inline size-4 mr-1" />
                        {{ t('password.rule-match') }}
                    </li>
                </ul>
            </div>
        </div>


        <template #footer>
            <Btn type="submit" :label="t('button.next')" :loading="loading" :disabled="!isValid" @click="setup" />
        </template>
    </SetupLayout>
</template>