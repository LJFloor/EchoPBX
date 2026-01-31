<script setup lang="ts">
import { Icon } from '@iconify/vue';

withDefaults(defineProps<{
    label: string
    icon?: string
    disabled?: boolean
    design?: 'primary' | 'danger'
}>(), {
    disabled: false,
    design: 'primary'
});

const emit = defineEmits<{
    (e: 'click', event: MouseEvent): void
}>();
</script>

<template>
    <button type="button" @click="emit('click', $event)" :class="['flex items-center gap-2 w-full px-4 h-8 rounded ',
        design === 'primary' ? 'hover:bg-gray-200' : '',
        design === 'danger' ? 'text-red-600 hover:bg-red-700/30' : '',
        disabled ? 'cursor-not-allowed opacity-50' : '']" :disabled="disabled">
        <Icon v-if="icon" :icon="icon" class="size-4" />
        <span>{{ label }}</span>
    </button>
</template>