<script setup lang="ts">
import { onMounted, ref } from 'vue';
import Btn from '~/components/Button/Btn.vue';
import Modal from '~/components/Modal/Modal.vue';
import NavButton from '~/components/NavButton.vue';
import SetPassword from '~/components/SetPassword.vue';
import { useTranslation } from '~/composables/useTranslation';

const props = withDefaults(defineProps<{
    padding?: boolean;
}>(), {
    padding: true,
});

const username = ref('');
const newPassword = ref('');
const changePasswordModalOpen = ref(false);
const changePasswordValid = ref(false);
const changePasswordLoading = ref(false);

function setUsername() {
    const cookieUsername = document.cookie.split('; ').find(row => row.startsWith('echopbx_username='));
    if (cookieUsername) {
        username.value = cookieUsername.split('=')[1] || '';
    }
}

function closeChangePasswordModal() {
    changePasswordModalOpen.value = false;
}

async function putNewPassword() {
    if (!changePasswordValid.value || !newPassword.value) {
        return;
    }

    changePasswordLoading.value = true;

    const response = await fetch('/api/auth/admin/password', {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            password: newPassword.value,
        }),
    });

    changePasswordLoading.value = false;

    if (response.ok) {
        alert('Password changed successfully, you will now be logged out');
        newPassword.value = '';
        changePasswordValid.value = false;
        await logout();
    } else {
        alert('Failed to change password');
    }
}

async function logout() {
    const response = await fetch('/api/auth/admin/logout', {
        method: 'POST',
    });

    if (response.ok) {
        window.location.href = '/';
    } else {
        alert('Logout failed');
    }
}

onMounted(() => {
    setUsername(); 
    setInterval(setUsername, 1000);
});

const { t } = useTranslation();
</script>

<template>
    <div class="w-full bg-sky-600">
        <div class="w-[1200px] h-10 mx-auto flex">
            <NavButton href="/admin/dashboard" :label="t('label.dashboard')" />
            <NavButton href="/admin/extensions" :label="t('label.extensions')" />
            <NavButton href="/admin/trunks" :label="t('label.trunks')" />
            <NavButton href="/admin/queues" :label="t('label.queues')" />
            <div class="grow"></div>
            <NavButton :label="t('button.change-password')" @click="changePasswordModalOpen = true" />
            <NavButton :label="t('button.logout')" @click="logout" />
        </div>
    </div>
    <div class="overflow-y-scroll h-[calc(100vh-40px)]">
        <div class="space-y-2" :class="props.padding ? 'w-[1200px] py-8 mx-auto' : ''">
            <slot />
        </div>
    </div>

    <Modal v-if="changePasswordModalOpen" @close="changePasswordModalOpen = false" :title="t('button.change-password')"">
        <SetPassword v-model:password="newPassword" v-model:valid="changePasswordValid" />
        <template #footer>
            <Btn :loading="changePasswordLoading" :disabled="!changePasswordValid" @click="putNewPassword" :label="t('button.save')" />
        </template>
    </Modal>
</template>