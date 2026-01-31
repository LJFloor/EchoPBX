<script setup lang="ts">
import { Icon } from '@iconify/vue';

defineProps<{
    title: string;
}>();

const emit = defineEmits<{
    (e: 'close'): void;
}>();
</script>

<template>
    <Teleport to="body">
        <div class="absolute inset-0 bg-black/50 flex pt-16 justify-center text-sm backdrop-blur-xs">
            <div class="bg-white rounded shadow-lg w-175 h-max overflow-hidden">
                <header class="pl-4 pr-2 py-2 border-b border-gray-300 font-semibold text-lg flex justify-between items-center bg-gray-100">
                    {{ title }}
                    <button @click="emit('close')" type="button" class="size-7 rounded hover:bg-gray-200 cursor-pointer flex">
                        <Icon icon="mdi:close" class="size-4 m-auto" />
                    </button>
                </header>
                <div class="p-8 space-y-4">
                    <slot></slot>
                </div>
                <footer v-if="$slots.footer" class="px-4 py-2 border-t border-gray-300 flex justify-end gap-2">
                    <slot name="footer"></slot>
                </footer>
            </div>
        </div>
    </Teleport>
</template>