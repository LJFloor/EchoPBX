<script setup lang="ts">
import { computed, ref } from 'vue';
import InputBase from './InputBase.vue';

const modelValue = defineModel<any>();

const secretHidden = ref(true);

const props = defineProps<{
    type?: 'text' | 'password' | 'number' | 'secret';
    maxlength?: number;
    minlength?: number;
    required?: boolean;
    disabled?: boolean;
}>();

function onChange(event: Event) {
    const target = event.target as HTMLInputElement;
    let value: any = target.value;

    if (props.type === 'number') {
        value = parseFloat(value);
        if (isNaN(value)) {
            value = null;
        }
    }

    modelValue.value = value;
}

const computedType = computed(() => {
    if (props.type === 'secret') {
        return secretHidden.value ? 'password' : 'text';
    }

    if (props.type === 'number') {
        return 'text';
    }

    return props.type;
});

</script>

<template>
    <InputBase :disabled="disabled">
        <input :required="props.required" :maxlength="props.maxlength" :minlength="props.minlength" :value="modelValue" :type="computedType" class="w-full h-full px-2 outline-none bg-transparent" @focus="secretHidden = false" @blur="secretHidden = true" @change="onChange" @input="onChange" />
    </InputBase>
</template>