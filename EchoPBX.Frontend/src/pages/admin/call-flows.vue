<script setup lang="ts">
import { onMounted, ref, useTemplateRef } from 'vue';
import { Column } from '~/components/DataGrid/Column';
import { useRouter } from 'vue-router';
import Btn from '~/components/Button/Btn.vue';
import DataGrid from '~/components/DataGrid/DataGrid.vue';
import type { CallFlow } from '~/types/CallFlow';
import DropdownButton from '~/components/Dropdown/DropdownButton.vue';
import Confirm from '~/components/Confirm/Confirm.vue';
import AdminLayout from '~/layouts/AdminLayout.vue';
import { useTranslation } from '~/composables/useTranslation';
import { useMemo } from '~/composables/useMemo';

const router = useRouter();
const { t } = useTranslation();
const columns = [
    new Column({ field: 'name', label: t('label.name'), width: 300 }),
    new Column({ field: 'slug', label: t('label.slug'), width: 200 }),
    new Column({ field: 'internalNumber', label: t('label.internal-number'), width: 150 }),
    new Column({ field: 'steps', label: t('label.steps'), width: 100, type: 'number' }),
];

const rows = useMemo<CallFlow[]>('call-flows', () => []);
const loading = ref(false);
const deleteConfirm = useTemplateRef('deleteConfirm');

async function refreshData() {
    loading.value = true;
    const response = await fetch('/api/call-flows');
    rows.value = await response.json();
    loading.value = false;
}

async function deleteCallFlow(row: CallFlow) {
    const confirmed = await deleteConfirm.value?.execute();
    if (!confirmed) return;

    const response = await fetch(`/api/call-flows/${row.id}`, {
        method: 'DELETE'
    });

    if (response.ok) {
        rows.value = rows.value.filter(r => r.id !== row.id);
    }
}

onMounted(refreshData);
document.title = t('label.call-flows');
</script>

<template>
    <AdminLayout>
        <Confirm ref="deleteConfirm" design="danger" :message="t('message.delete-call-flow-confirmation')" />
        <Btn design="primary" icon="mdi:plus" :label="t('button.new')" @click="router.push('/admin/call-flows/new')" />
        <DataGrid :loading="loading" allow-selection enable-card :columns="columns" :rows="rows"
            @row-click="row => router.push(`/admin/call-flows/${row.slug}`)">
            <template #cell.internalNumber="{ row }">
                {{ row.internalNumber ?? '-' }}
            </template>
            <template #actions="{ row }">
                <DropdownButton icon="mdi:delete" :label="t('button.delete')" design="danger"
                    @click="deleteCallFlow(row)" />
            </template>
        </DataGrid>
    </AdminLayout>
</template>
