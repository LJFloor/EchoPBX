<script setup lang="ts">
import { Icon } from '@iconify/vue';
import type { Column } from './Column';

const numberFormatter = new Intl.NumberFormat('nl-NL', {
    maximumFractionDigits: 2,
    minimumFractionDigits: 0
});

defineProps<{
    value: any
    column: Column
}>()
</script>

<template>
    <span v-if="column.type === 'number'">
        {{ numberFormatter.format(value) }}
    </span>
    <template v-else-if="column.type === 'image'">
        <Icon class="size-7 -m-1" v-if="value?.startsWith('mdi:')" :icon="value" />
        <img class="size-7 rounded-full -m-1" v-if="value?.startsWith('http') || value?.startsWith('data:image')" :src="value" />
        <img class="size-7 rounded-full -m-1" v-else-if="value" :src="`data:image/png;base64,${value}`" />
    </template>
    <span v-else-if="column.type === 'boolean'">
        <Icon v-if="value" icon="mdi:check-circle" class="text-green-600 size-5" />
        <Icon v-else icon="mdi:close-circle" class="text-gray-300 size-5" />
    </span>
    <span v-else>
        {{ value }}
    </span>
</template>