<script setup lang="ts">
import { inject } from 'vue';
import { Handle, Position } from '@vue-flow/core';
import { Icon } from '@iconify/vue';
import { flowContextKey } from './flowContext';
import { useTranslation } from '~/composables/useTranslation';

/**
 * One output of a step with several, such as a key of a phone menu. Only exists on the canvas:
 * the stored edge runs straight from the step to whatever follows the branch.
 */
const props = defineProps<{
    data: {
        source: string;
        handle: string;
        label: string;
    };
}>();

const { t } = useTranslation();
const flow = inject(flowContextKey)!;
</script>

<template>
    <div class="relative pointer-events-auto">
        <Handle type="target" :position="Position.Top" class="!opacity-0" />

        <div class="w-[110px] h-[30px] rounded-full bg-sky-50 border border-sky-200 text-sky-700 text-xs font-semibold flex items-center justify-center truncate px-2">
            {{ props.data.label }}
        </div>

        <Handle type="source" :position="Position.Bottom" class="!opacity-0" />

        <button type="button"
            class="nopan absolute -bottom-9 left-1/2 -translate-x-1/2 size-6 rounded-full bg-white border border-gray-300 text-slate-500 flex items-center justify-center hover:border-sky-500 hover:text-sky-600 cursor-pointer z-10"
            :title="t('button.add-step')" @click.stop="flow.addAfter(props.data.source, props.data.handle)">
            <Icon icon="mdi:plus" class="size-4" />
        </button>
    </div>
</template>
