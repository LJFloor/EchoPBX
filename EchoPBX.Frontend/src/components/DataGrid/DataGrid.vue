<script setup lang="ts" generic="T">
import { Icon } from '@iconify/vue';
import type { Column } from './Column';
import ValueDisplay from './ValueDisplay.vue';
import { ref } from 'vue';
import { useTranslation } from '~/composables/useTranslation';

const props = defineProps<{
    rows: T[],
    columns: Column[],
    enableCard?: boolean,
    allowSelection?: boolean,
    loading?: boolean
}>();

const { t } = useTranslation();

const dropdownRight = ref<number>();
const dropdownTop = ref<number>();
const dropdownRow = ref<T>();

const checkedRows = defineModel<number[]>('checkedRows', { default: () => [] });

const emit = defineEmits<{
    (e: 'rowClick', row: T): void
}>();

function onRowClick(rowIndex: number) {
    emit('rowClick', props.rows[rowIndex]!);
}

function onDocumentClick() {
    dropdownRow.value = undefined;
    document.removeEventListener('click', onDocumentClick);
}

function onActionsClick(row: T, event: MouseEvent) {
    // get the bottom right of the button that was clicked
    const target = event.currentTarget as HTMLElement;
    const rect = target.getBoundingClientRect();
    dropdownRight.value = window.innerWidth - rect.right;
    dropdownTop.value = rect.bottom;
    dropdownRow.value = row;
    document.addEventListener('click', onDocumentClick);

}
</script>

<template>
    <table class="w-full bg-white h-max" :class="{ 'drop-shadow-sm rounded': enableCard }">
        <thead>
            <tr>
                <th v-for="column in columns" :key="column.field" :style="{ width: column.width + 'px' }"
                    class="text-left py-1 px-2 border-b border-gray-500 text-slate-500">
                    {{ column.label }}
                </th>
                <th class="text-left py-1 px-2 border-b border-gray-500 text-slate-500"></th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="(row, rowIndex) in rows" :key="rowIndex" class="cursor-pointer" :class="{
                'bg-gray-200': checkedRows.includes(rowIndex),
                'hover:bg-gray-100': !checkedRows.includes(rowIndex) && allowSelection

            }">
                <td v-for="column in columns" :key="column.field" class="px-2 h-9 border-b border-gray-300 select-none"
                    @click="onRowClick(rowIndex)">
                    <slot name="cell" :row="row" :column="column">
                        <slot :name="`cell.${column.field}`" :row="row" :column="column">
                            <ValueDisplay :value="(row as any)[column.field]" :column="column" />
                        </slot>
                    </slot>
                </td>
                <td class="px-2 h-9 border-b border-gray-300 select-none" @click="onRowClick(rowIndex)">
                    <div v-if="$slots.actions" class="flex justify-end">
                        <button type="button" @click.stop="onActionsClick(row, $event)"
                            class="rounded-full size-7 flex items-center justify-center hover:bg-gray-200">
                            <Icon icon="mdi:dots-vertical" class="size-4" />
                        </button>
                    </div>
                </td>
            </tr>
            <tr v-if="rows.length === 0">
                <td :colspan="columns.length + 1">
                    <div class="text-slate-500 py-4 min-h-32 flex items-center justify-center">
                        <slot name="loading" v-if="loading">
                            {{ t('label.loading') }}...
                        </slot>
                        <slot v-else name="empty">
                            {{ t('label.no-data-available') }}
                        </slot>
                    </div>
                </td>
            </tr>
        </tbody>
    </table>

    <Teleport to="body">
        <div v-if="dropdownRow" :style="{ top: dropdownTop + 'px', right: dropdownRight + 'px' }"
            class="absolute z-50 dropdown">
            <div class="bg-white border border-gray-300 rounded shadow-md p-1 grid w-48 text-sm">
                <slot name="actions" :row="dropdownRow">
                    Acties
                </slot>
            </div>
        </div>
    </Teleport>
</template>