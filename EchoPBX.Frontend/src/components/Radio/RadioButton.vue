<script setup lang="ts" generic="T">
import { useTemplateRef } from 'vue';

const emit = defineEmits<{
    (e: 'click'): void;
}>();

const modelValue = defineModel<T>();

const inputRef = useTemplateRef('input');

defineProps<{
    label: string;
    value: T;
    description?: string;
    selected?: boolean;
    disabled?: boolean;
}>();

</script>

<template>
    <div type="button" class="flex text-left items-start py-2 w-full">
        <div class="w-8 flex-none">
            <input ref="input" :value="value" v-model="modelValue" class="size-5 cursor-pointer accent-sky-600" type="radio" :disabled="disabled" />
        </div>
        <div>
            <span class="font-semibold select-none cursor-pointer" @click="inputRef?.click()">{{ label }}</span>
            <div class="text-sm text-gray-600">{{ description }}</div>
        </div>
    </div>
</template>