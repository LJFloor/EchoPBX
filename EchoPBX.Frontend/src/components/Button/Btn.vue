<script setup lang="ts">
import { Icon } from '@iconify/vue';

const props = withDefaults(defineProps<{
    label?: string
    icon?: string
    type?: 'button' | 'submit' | 'reset'
    design?: 'primary' | 'secondary' | 'danger' | 'banner' | 'icon-primary' | 'icon-secondary' | 'icon-danger-secondary'
    loading?: boolean
    disabled?: boolean
    roundedLeft?: boolean
    roundedRight?: boolean
}>(), {
    design: 'primary',
    roundedLeft: true,
    roundedRight: true,
    loading: false,
    disabled: false,
    type: 'button'
});

const emit = defineEmits<{
    (e: 'click', event: MouseEvent): void
}>();
</script>

<template>
    <button :type="type" class="text-left flex-none flex h-8 items-center relative duration-100"
        @click="emit('click', $event)" :class="{
            'px-5 bg-sky-600 text-white hover:bg-sky-700 disabled:hover:bg-sky-600': design === 'primary',
            'px-5 bg-gray-50 text-black border border-gray-300 hover:border-gray-400 disabled:hover:border-gray-300': design === 'secondary',
            'px-5 bg-red-600 text-white hover:bg-red-700 disabled:hover:bg-red-600': design === 'danger',
            'px-5 w-full justify-center border text-slate-500 border-slate-300 border-dashed hover:bg-gray-50 disabled:hover:bg-transparent': design === 'banner',
            'w-8 justify-center bg-sky-100 text-sky-600 hover:bg-sky-200 disabled:hover:bg-sky-100': design === 'icon-primary',
            'w-8 justify-center bg-gray-100 text-gray-600 hover:bg-gray-200 disabled:hover:bg-gray-100': design === 'icon-secondary',
            'w-8 justify-center bg-red-100 text-red-600 hover:bg-red-200 disabled:hover:bg-red-100': design === 'icon-danger-secondary',
            'rounded-l': roundedLeft,
            'rounded-r': roundedRight,
            'opacity-50 cursor-default': disabled || loading
        }" :disabled="disabled || loading">
        <template v-if="loading">
            <span
                class="loader-border size-5 border-2 border-t-transparent rounded-full animate-spin absolute -translate-x-1/2 left-1/2"></span>
        </template>
        <div class="gap-2 flex items-center" :class="loading ? 'opacity-0' : ''">
            <Icon v-if="props.icon" :icon="props.icon" class="size-4.5" />
            {{ props.label }}
        </div>
    </button>
</template>