<script setup lang="ts" generic="T">
import { computed, nextTick, ref, useTemplateRef } from 'vue';
import InputBase from '~/components/Input/InputBase.vue';
import { Icon } from '@iconify/vue';


const props = defineProps<{
    required?: boolean;
    items: T[];
    disabled?: boolean;
    itemDisabled?: (item: T) => boolean | undefined;
}>();

const modelValue = defineModel<T>();

const buttonRef = useTemplateRef('button');
const menuRef = useTemplateRef('menu');

const menuStyle = ref<any>();
const isOpen = computed(() => !!menuStyle.value);

function open() {
    const rect = buttonRef.value?.getBoundingClientRect();
    if (rect) {
        menuStyle.value = {
            top: `${rect.bottom + window.scrollY + 2}px`,
            left: `${rect.left + window.scrollX}px`,
            width: `${rect.width}px`,
        };
    }

    nextTick(() => {
        document.addEventListener('click', onClickOutside);
    });
}

function onClickOutside(e: MouseEvent) {
    if (menuRef.value && !menuRef.value.contains(e.target as Node) && buttonRef.value && !buttonRef.value.contains(e.target as Node)) {
        menuStyle.value = undefined;
        document.removeEventListener('click', onClickOutside);
    }
}

function itemClick(e: MouseEvent, item: T) {
    if (props.itemDisabled && props.itemDisabled(item)) {
        e.stopPropagation();
        e.stopImmediatePropagation();
        return;
    }

    modelValue.value = item;
    menuStyle.value = undefined;
}
</script>

<template>
    <InputBase :disabled="disabled">
        <button ref="button" type="button" class="h-full w-full" @click="open">
            <div class="h-full w-full flex items-center px-3 gap-2.5 relative">
                <slot v-if="modelValue" name="item" :value="modelValue"></slot>
                <Icon icon="mdi:chevron-down" class="size-4 text-gray-400 absolute right-2" />
            </div>
        </button>
    </InputBase>

    <Teleport to="body">
        <div v-if="isOpen" ref="menu"
            class="absolute bg-white drop-shadow z-10 text-sm border border-gray-300 rounded overflow-hidden"
            :style="menuStyle">
            <div class="max-h-80 overflow-y-auto">
                <button v-for="item in items"
                    class="px-3 py-1 flex items-center gap-2.5 w-full text-left hover:bg-gray-100 cursor-pointer disabled:opacity-50 disabled:hover:bg-transparent disabled:pointer-events-none"
                    :disabled="itemDisabled ? itemDisabled(item) : false" @click="itemClick($event, item)">
                    <slot name="item" :value="item"></slot>
                </button>
            </div>
        </div>
    </Teleport>
</template>