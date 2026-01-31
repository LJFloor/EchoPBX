<script setup lang="ts">
import { onMounted, ref, useTemplateRef } from 'vue';
import { Column } from '~/components/DataGrid/Column';
import { useRouter } from 'vue-router';
import Btn from '~/components/Button/Btn.vue';
import DataGrid from '~/components/DataGrid/DataGrid.vue';
import type { Trunk } from '~/types/Trunk';
import Card from '~/components/Card/Card.vue';
import { Icon } from '@iconify/vue';
import AdminLayout from '~/layouts/AdminLayout.vue';
import { useTranslation } from '~/composables/useTranslation';
import { useMemo } from '~/composables/useMemo';
import DropdownButton from '~/components/Dropdown/DropdownButton.vue';
import Confirm from '~/components/Confirm/Confirm.vue';

const router = useRouter();
const { t } = useTranslation();
const columns = [
    new Column({ field: 'connected', label: ' ', width: 30 }),
    new Column({ field: 'name', label: t('label.name'), width: 400 }),
    new Column({ field: 'host', label: t('label.host'), width: 200 }),
    new Column({ field: 'cid', label: t('label.cid'), width: 200 }),
    new Column({ field: 'codecs', label: t('label.codecs'), width: 250 }),
];

const rows = useMemo<Trunk[]>('trunks', () => []);
const loading = ref(false);
const deleteConfirm = useTemplateRef('deleteConfirm');

async function refreshData() {
    loading.value = true;
    const response = await fetch('/api/trunks');
    rows.value = await response.json();
    loading.value = false;
}

async function deleteTrunk(row: Trunk) {
    const confirmed = await deleteConfirm.value?.execute();
    if (!confirmed) return;

    const response = await fetch(`/api/trunks/${row.id}`, {
        method: 'DELETE'
    });

    if (response.ok) {
        rows.value = rows.value.filter(x => x.id !== row.id);
    }
}

onMounted(refreshData);
document.title = t('label.trunks');
</script>

<template>
    <AdminLayout>
        <Confirm ref="deleteConfirm" design="danger" :message="t('message.delete-trunk-confirmation')" />
        <Btn design="primary" icon="mdi:plus" :label="t('button.new')" @click="router.push('/admin/trunks/new')" />
        <div class="flex gap-4">

            <DataGrid :loading="loading" allow-selection enable-card :columns="columns" :rows="rows"
                @row-click="row => router.push(`/admin/trunks/${row.id}`)">
                <template #cell.codecs="{ row }">
                    {{ row.codecs.join(', ') }}
                </template>
                <template #cell.cid="{ row }">
                    <span v-if="row.cid">{{ row.cid }}</span>
                    <span v-else>-</span>
                </template>
                <template #cell.connected="{ row }">
                    <span class="size-2.5 rounded-full inline-block" :class="row.connected ? 'bg-green-500' : 'bg-gray-500'"
                        :title="row.connected ? t('label.connected') : t('label.not-connected')"></span>
                </template>
                <template #actions="{ row }">
                    <DropdownButton icon="mdi:delete" :label="t('button.delete')" design="danger" @click="deleteTrunk(row)" />
                </template>
            </DataGrid>

            <Card class="w-72 flex-none">
                <h2 class="text-base font-semibold text-slate-700 flex gap-2 items-center">
                    <Icon icon="mdi:connection" /> {{ t('label.trunks') }}
                </h2>
                <p>
                    {{ t('label.trunks-instructions-1') }}
                </p>

                <p>
                    {{ t('label.trunks-instructions-2') }}
                </p>

                <p>
                    {{ t('label.trunks-instructions-3') }}
                </p>
            </Card>
        </div>
    </AdminLayout>
</template>