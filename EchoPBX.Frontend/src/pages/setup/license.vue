<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import Btn from '~/components/Button/Btn.vue';
import Checkbox from '~/components/Checkbox/Checkbox.vue';
import { useLocalStorage } from '~/composables/useLocalStorage';
import SetupLayout from '~/layouts/SetupLayout.vue';
import { useTranslation } from '~/composables/useTranslation';

const { t } = useTranslation();

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
    <SetupLayout :title="t('setup.license-title')">
        <p>
            {{ t('setup.license-intro') }}
        </p>

        <div class="border border-gray-200 p-4 overflow-y-scroll whitespace-pre-wrap bg-gray-50">
            {{ licenseText || t('setup.license-loading') }}
        </div>

        <Checkbox v-if="licenseText" :label="t('setup.license-accept')" v-model:checked="licenseAccepted" />
        
        <template #footer>
            <Btn :disabled="!licenseAccepted" :label="t('button.next')" @click="router.push('/setup/create-admin')" />
        </template>
    </SetupLayout>
</template> 