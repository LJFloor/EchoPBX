<script setup lang="ts">
import { Icon } from '@iconify/vue';
import { useRouter } from 'vue-router';
import Btn from '~/components/Button/Btn.vue';
import SetupLayout from '~/layouts/SetupLayout.vue';
import { useTranslation } from '~/composables/useTranslation';

const { t } = useTranslation();

const router = useRouter();
</script>

<template>
    <SetupLayout :title="t('setup.welcome-title')">
        <p>
            {{ t('setup.welcome-thanks') }}
        </p>
        <p>
            {{ t('setup.welcome-intro') }}
        </p>
        <p>
            <b>{{ t('setup.beta-notice') }}</b> {{ t('setup.beta-feedback') }}
        </p>
        <p class="flex items-center gap-0.5 text-blue-600">
            <Icon icon="mdi:open-in-new" />
            <a href="https://github.com/LJFloor/EchoPBX/issues" target="_blank" rel="noopener" class="underline">{{ t('setup.report-issues') }}</a>
        </p>

        <template #footer>
            <Btn :label="t('button.next')" @click="router.push('/setup/license')" />
        </template>
    </SetupLayout>
</template> 