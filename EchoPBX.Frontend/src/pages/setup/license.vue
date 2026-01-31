<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import Btn from '~/components/Button/Btn.vue';
import Checkbox from '~/components/Checkbox/Checkbox.vue';
import { useLocalStorage } from '~/composables/useLocalStorage';
import SetupLayout from '~/layouts/SetupLayout.vue';

const router = useRouter();
const licenseText = ref();
const licenseAccepted = useLocalStorage('setup:license:accepted', false);

onMounted(async () => {
    if (!licenseText.value) {
        const response = await fetch('/license.txt');
        licenseText.value = await response.text();
        sessionStorage.setItem('license:text', licenseText.value);
    }
})
</script>

<template>
    <SetupLayout title="License agreement">
        <p>
            Please accept the license agreement to continue the setup.
        </p>

        <div class="border border-gray-200 p-4 overflow-y-scroll whitespace-pre-wrap bg-gray-50">
            {{ licenseText || 'Loading license text...' }}
        </div>

        <Checkbox v-if="licenseText" label="I accept the license agreement" v-model:checked="licenseAccepted" />
        
        <template #footer>
            <Btn :disabled="!licenseAccepted" label="Next" @click="router.push('/setup/create-admin')" />
        </template>
    </SetupLayout>
</template> 