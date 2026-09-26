<script setup lang="ts">
import { useRouter } from 'vue-router';
import Btn from '~/components/Button/Btn.vue';
import SetupLayout from '~/layouts/SetupLayout.vue';
import { useTranslation } from '~/composables/useTranslation';

const { t } = useTranslation();

const router = useRouter();
</script>

<template>
    <SetupLayout :title="t('setup.finish-title')">
        <p>
            {{ t('setup.finish-done') }}
        </p>
        <p>
            {{ t('setup.finish-next') }}
        </p>

        <template #footer>
            <Btn :label="t('setup.go-to-dashboard')" @click="router.push('/admin/dashboard')" />
        </template>
    </SetupLayout>
</template> 