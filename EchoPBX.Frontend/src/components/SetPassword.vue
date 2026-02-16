<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import Textbox from './Input/Textbox.vue';
import { Icon } from '@iconify/vue';

const txtPassword = ref('');
const txtConfirmPassword = ref('');
const password = defineModel('password');
const isValid = defineModel('valid', { default: false });

const hasLetter = computed(() => /[a-zA-Z]/.test(txtPassword.value));
const hasNumber = computed(() => /[0-9]/.test(txtPassword.value));
const hasSpecialChar = computed(() => /[!@#$%^&*(),.?":{}|<>]/.test(txtPassword.value));
const isMinLength = computed(() => txtPassword.value.length >= 10);

function validate() {
    if (!txtPassword.value || !txtConfirmPassword.value) {
        isValid.value = false;
        return;
    }


    if (txtPassword.value !== txtConfirmPassword.value) {
        isValid.value = false;
        return;
    }

    if (!hasLetter.value || !hasNumber.value || !hasSpecialChar.value || !isMinLength.value) {
        isValid.value = false;
        return;
    }

    isValid.value = true;
    password.value = txtPassword.value;
}

watch([txtPassword, txtConfirmPassword], validate, { immediate: true });
</script>

<template>
    <div class="grid grid-cols-2 gap-4">
        <div>Password:</div>
        <Textbox required type="password" v-model="txtPassword" />

        <div>Confirm Password:</div>
        <Textbox required type="password" v-model="txtConfirmPassword" />

        <div></div>
        <div v-if="txtPassword">
            <ul class="list-inside">
                <li :class="hasLetter ? 'text-green-600' : 'text-red-600'">
                    <Icon :icon="hasLetter ? 'mdi:check' : 'mdi:close'" class="inline size-4 mr-1" />
                    At least one letter
                </li>
                <li :class="hasNumber ? 'text-green-600' : 'text-red-600'">
                    <Icon :icon="hasNumber ? 'mdi:check' : 'mdi:close'" class="inline size-4 mr-1" />
                    At least one number
                </li>
                <li :class="hasSpecialChar ? 'text-green-600' : 'text-red-600'">
                    <Icon :icon="hasSpecialChar ? 'mdi:check' : 'mdi:close'" class="inline size-4 mr-1" />
                    At least one special character
                </li>
                <li :class="isMinLength ? 'text-green-600' : 'text-red-600'">
                    <Icon :icon="isMinLength ? 'mdi:check' : 'mdi:close'" class="inline size-4 mr-1" />
                    Minimum length of 10 characters
                </li>
                <li :class="txtPassword === txtConfirmPassword ? 'text-green-600' : 'text-red-600'">
                    <Icon :icon="txtPassword === txtConfirmPassword ? 'mdi:check' : 'mdi:close'" class="inline size-4 mr-1" />
                    Passwords match
                </li>
            </ul>
        </div>
    </div>
</template>