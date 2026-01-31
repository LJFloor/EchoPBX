<script setup lang="ts" generic="T">
const emit = defineEmits<{
    (e: 'click'): void;
}>();

const modelValue = defineModel<T[]>();
const checked = defineModel<boolean>('checked');

defineProps<{
    label?: string;
    value?: T;
    description?: string;
    selected?: boolean;
    disabled?: boolean;
}>();

function onInput(e: InputEvent) {
    const checkboxElement = e.target as HTMLInputElement;
    checked.value = checkboxElement.checked;
}

</script>

<template>
    <label class="flex cursor-pointer items-center select-none w-max">
        <div class="w-8 flex">
            <input class="flex-none my-auto size-4.5 accent-sky-600" type="checkbox" :value="value" :checked="checked" v-model="modelValue" @input="onInput" :disabled="disabled" />
        </div>
        <div>
            <div>{{ label }}</div>
            <div v-if="description" class="text-sm text-gray-600">{{ description }}</div>
        </div>
    </label>
</template>