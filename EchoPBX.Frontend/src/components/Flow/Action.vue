<script setup lang="ts">
import { computed, inject } from 'vue';
import { Handle, Position } from '@vue-flow/core';
import { Icon } from '@iconify/vue';
import { nodeType } from './nodeTypes';
import { flowContextKey } from './flowContext';
import { useTranslation } from '~/composables/useTranslation';
import type { CallFlowNode } from '~/types/CallFlow';

const props = defineProps<{
    data: {
        node: CallFlowNode;
        subtitle: string;
        canAddAfter: boolean;
    };
}>();

const { t } = useTranslation();
const flow = inject(flowContextKey)!;

const definition = computed(() => nodeType(props.data.node.type));
const isStart = computed(() => props.data.node.type === 'start');
const isConfigurable = computed(() => definition.value.configurable);
const hasOutputs = computed(() => definition.value.outputs(props.data.node).length > 0);
</script>

<template>
    <!-- Vue Flow sets pointer-events: none on nodes that are neither selectable nor draggable,
         which would make every button on the card dead. -->
    <div class="relative pointer-events-auto">
        <Handle v-if="!isStart" type="target" :position="Position.Top" class="!opacity-0" />

        <div class="w-[260px] bg-white rounded shadow border flex items-center gap-3 px-3 py-2.5 text-left"
            :class="[
                isStart ? 'border-sky-600' : 'border-gray-200',
                isConfigurable ? 'cursor-pointer hover:border-sky-400' : 'cursor-default',
            ]"
            @click="isConfigurable && flow.editNode(props.data.node.id)">
            <div class="size-9 flex-none rounded flex items-center justify-center"
                :class="isStart ? 'bg-sky-50 text-sky-600' : 'bg-gray-100 text-slate-600'">
                <Icon :icon="definition.icon" class="size-5" />
            </div>
            <div class="min-w-0 flex-1">
                <div class="font-semibold text-sm truncate">{{ t(definition.labelKey) }}</div>
                <div class="text-xs text-slate-500 truncate">{{ props.data.subtitle }}</div>
            </div>
            <button v-if="!isStart" type="button"
                class="nopan size-7 flex-none rounded flex items-center justify-center text-slate-400 hover:bg-red-50 hover:text-red-600 cursor-pointer"
                :title="t('button.delete')" @click.stop="flow.deleteNode(props.data.node.id)">
                <Icon icon="mdi:delete" class="size-4" />
            </button>
        </div>

        <Handle v-if="hasOutputs" type="source" :position="Position.Bottom" class="!opacity-0" />

        <!-- Inserts a step into the line leaving this card, so a flow is built without ever
             dragging a connection. -->
        <button v-if="props.data.canAddAfter" type="button"
            class="nopan absolute -bottom-9 left-1/2 -translate-x-1/2 size-6 rounded-full bg-white border border-gray-300 text-slate-500 flex items-center justify-center hover:border-sky-500 hover:text-sky-600 cursor-pointer z-10"
            :title="t('button.add-step')" @click.stop="flow.addAfter(props.data.node.id)">
            <Icon icon="mdi:plus" class="size-4" />
        </button>
    </div>
</template>
