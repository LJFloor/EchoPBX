<script setup lang="ts">
import { useRouter } from 'vue-router';
import Btn from '~/components/Button/Btn.vue';
import SetupLayout from '~/layouts/SetupLayout.vue';

const router = useRouter();
</script>

<template>
    <SetupLayout title="Setup is complete">
        <p>
            You're all set! The setup is complete.
        </p>
        <p>
            You can now proceed to the dashboard to start configuring your EchoPBX instance.
        </p>

        <template #footer>
            <Btn label="Go to dashboard" @click="router.push('/admin/dashboard')" />
        </template>
    </SetupLayout>
</template> 