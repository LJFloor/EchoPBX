<script setup lang="ts">
import { onMounted, ref, useTemplateRef } from 'vue';
import { Column } from '~/components/DataGrid/Column';
import { useRouter } from 'vue-router';
import Btn from '~/components/Button/Btn.vue';
import DataGrid from '~/components/DataGrid/DataGrid.vue';
import type { Queue } from '~/types/Queue';
import DropdownButton from '~/components/Dropdown/DropdownButton.vue';
import Confirm from '~/components/Confirm/Confirm.vue';
import AdminLayout from '~/layouts/AdminLayout.vue';
import { useTranslation } from '~/composables/useTranslation';
import { useMemo } from '~/composables/useMemo';

const router = useRouter();
const { t } = useTranslation();
const columns = [
    new Column({ field: 'name', label: t('label.name'), width: 400 }),
    new Column({ field: 'extensions', label: t('label.extensions'), width: 100, type: 'number' }),
    new Column({ field: 'strategy', label: t('label.strategy'), width: 100 }),
    new Column({ field: 'timeout', label: t('label.timeout-for-extension-seconds'), width: 175 }),
    new Column({ field: 'wrapUpTime', label: t('label.wrap-up-time-for-extension-seconds'), width: 200 }),
];

const rows = useMemo<Queue[]>('queues', () => []);
const loading = ref(false);
const deleteConfirm = useTemplateRef('deleteConfirm');

async function refreshData() {
    loading.value = true;
    const response = await fetch('/api/queues');
    rows.value = await response.json();
    loading.value = false;
}

async function deleteQueue(row: Queue) {
    const confirmed = await deleteConfirm.value?.execute();
    if (!confirmed) return;

    const response = await fetch(`/api/queues/${row.id}`, {
        method: 'DELETE'
    });

    if (response.ok) {
        rows.value = rows.value.filter(r => r.id !== row.id);
    }
}

onMounted(refreshData);
document.title = t('label.queues');
</script>

<template>
    <AdminLayout>
        <Confirm ref="deleteConfirm" design="danger" :message="t('message.delete-queue-confirmation')" />
        <Btn design="primary" icon="mdi:plus" :label="t('button.new')" @click="router.push('/admin/queues/new')" />
        <DataGrid :loading="loading" allow-selection enable-card :columns="columns" :rows="rows"
            @row-click="row => router.push(`/admin/queues/${row.id}`)">
            <template #cell.extensions="{ row }">
                {{ row.extensions.length }}
            </template>
            <template #cell.timeout="{ row }">
                {{ row.timeout }} {{ t('label.seconds').toLowerCase() }}
            </template>
            <template #actions="{ row }">
                <DropdownButton icon="mdi:delete" :label="t('button.delete')" design="danger" @click="deleteQueue(row)" />
            </template>
        </DataGrid>
    </AdminLayout>
</template>