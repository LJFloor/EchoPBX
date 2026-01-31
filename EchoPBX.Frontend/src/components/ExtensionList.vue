<script setup lang="ts">
import { Icon } from '@iconify/vue';
import type { Extension } from '~/types/Extension';
import Select from './Select/Select.vue';
import Btn from './Button/Btn.vue';
import { ref, watch } from 'vue';

const extensions = defineModel<number[]>({ default: () => [] });

const displayExtensions = ref<(number | null)[]>([...extensions.value]);

defineProps<{
    availableExtensions: Extension[];
    disabled?: boolean;
}>();

function addNewExtension() {
    displayExtensions.value.push(null);
}

function removeExtension(index: number) {
    displayExtensions.value.splice(index, 1);
}

function moveUp(index: number) {
    if (index === 0) return;
    const ext = displayExtensions.value[index];
    displayExtensions.value.splice(index, 1);
    displayExtensions.value.splice(index - 1, 0, ext!);
}

function moveDown(index: number) {
    if (index === displayExtensions.value.length - 1) return;
    const ext = displayExtensions.value[index];
    displayExtensions.value.splice(index, 1);
    displayExtensions.value.splice(index + 1, 0, ext!);
}

watch(displayExtensions, (newVal) => {
    extensions.value = newVal.filter(x => x !== null) as number[];
}, { deep: true });
</script>

<template>
    <div class="space-y-2" :class="{ 'opacity-50 pointer-events-none': disabled }">
        <div v-for="_, i in displayExtensions" class="flex items-center gap-2" id="extensions-list">
            <Select v-model="displayExtensions[i]" :items="availableExtensions.map(x => x.extensionNumber).filter(x => !displayExtensions.includes(x) || x === displayExtensions[i])">
                <template #item="{ value }">
                    <template v-if="value">
                        <Icon v-if="availableExtensions.find(x => x.extensionNumber === value)?.connected"
                            icon="mdi:phone" class="size-4.5 text-green-500" />
                        <Icon v-else icon="mdi:phone" class="size-4.5 text-gray-500" />
                        <b>{{ value }}</b> {{availableExtensions.find(x => x.extensionNumber === value)?.displayName}}
                    </template>
                </template>
            </Select>
            <Btn design="icon-danger-secondary" icon="mdi:delete" @click="removeExtension(i)" />
            <Btn design="icon-secondary" icon="mdi:arrow-up" :disabled="i === 0" @click="moveUp(i)" />
            <Btn design="icon-secondary" icon="mdi:arrow-down" :disabled="i === extensions.length - 1"
                @click="moveDown(i)" />
        </div>
        <Btn :disabled="displayExtensions.length >= availableExtensions.length" design="banner" label="Extensie toevoegen" icon="mdi:plus" @click="addNewExtension" />

    </div>
</template>