<script setup lang="ts">
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

        <template #footer>
            <Btn label="Next" @click="router.push('/setup/license')" />
        </template>
    </SetupLayout>
</template> 