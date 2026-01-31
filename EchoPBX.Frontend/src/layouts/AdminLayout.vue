<script setup lang="ts">
import NavButton from '~/components/NavButton.vue';
import { useTranslation } from '~/composables/useTranslation';

const props = withDefaults(defineProps<{
    padding?: boolean;
}>(), {
    padding: true,
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
        </div>
    </div>
    <div class="overflow-y-scroll h-[calc(100vh-40px)]">
        <div class="space-y-2" :class="props.padding ? 'w-[1200px] py-8 mx-auto' : ''">
            <slot />
        </div>
    </div>
</template>