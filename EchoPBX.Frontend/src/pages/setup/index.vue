<script setup lang="ts">
import { Icon } from '@iconify/vue';
import { useRouter } from 'vue-router';
import Btn from '~/components/Button/Btn.vue';
import SetupLayout from '~/layouts/SetupLayout.vue';

const router = useRouter();
</script>

<template>
    <SetupLayout title="Welcome to EchoPBX!">
        <p>
            Hi! Thank you for trying out EchoPBX.
        </p>
        <p>
            You got through the docker installation part, so let's set up the initial configuration. Click "Next" to continue.
        </p>
        <p>
            Please note that <b>EchoPBX is still in BETA!</b> New features are being added,
            and new bugs may be introduced. Your feedback is very much appreciated!
        </p>
        <p class="flex items-center gap-0.5 text-blue-600">
            <Icon icon="mdi:open-in-new" />
            <a href="https://github.com/LJFloor/EchoPBX/issues" target="_blank" rel="noopener" class="underline">Report issues or suggest features on GitHub</a>
        </p>

        <template #footer>
            <Btn label="Next" @click="router.push('/setup/license')" />
        </template>
    </SetupLayout>
</template> 