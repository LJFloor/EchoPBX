<script setup lang="ts">
defineProps<{
    title: string;
}>();

const emit = defineEmits<{
    (e: 'submit'): void;
}>();
</script>

<template>
    <form @submit.prevent="emit('submit')">
        <div class="h-screen w-screen flex items-center justify-center">
            <div class="h-[500px] w-[750px] bg-white flex flex-col rounded-lg shadow-lg">
                <div class="text-lg font-semibold px-4 py-2 border-b border-gray-200 flex gap-2 items-center">
                    <img src="/echopbx.svg" class="size-6" alt="">
                    {{ title }}
                </div>
                <div class="p-8 grow overflow-auto space-y-4">
                    <slot />
                </div>
                <div v-if="$slots.footer" class="px-4 py-2 border-t border-gray-200 flex justify-end gap-2">
                    <slot name="footer" />
                </div>
            </div>
        </div>
    </form>
</template>