<script setup lang="ts">
import { onMounted, ref, useTemplateRef } from 'vue';
import { Column } from '~/components/DataGrid/Column';
import { type Extension } from '~/types/Extension';
import { useRouter } from 'vue-router';
import Btn from '~/components/Button/Btn.vue';
import DataGrid from '~/components/DataGrid/DataGrid.vue';
import DropdownButton from '~/components/Dropdown/DropdownButton.vue';
import { Icon } from '@iconify/vue';
import Confirm from '~/components/Confirm/Confirm.vue';
import type { Trunk } from '~/types/Trunk';
import Card from '~/components/Card/Card.vue';
import { useTranslation } from '~/composables/useTranslation';
import AdminLayout from '~/layouts/AdminLayout.vue';
import { useMemo } from '~/composables/useMemo';

const hostname = window.location.hostname;
const router = useRouter();
const { t } = useTranslation();

const columns = [
    new Column({ field: 'connected', label: ' ', width: 30 }),
    new Column({ field: 'extensionNumber', label: t('label.extension'), width: 100 }),
    new Column({ field: 'displayName', label: t('label.name'), width: 300 }),
    new Column({ field: 'outgoingTrunk', label: t('label.outgoing-trunk'), width: 200 }),
];

const rows = useMemo<Extension[]>('extensions', () => []);
const trunks = useMemo<Trunk[]>('trunks', () => []);
const loading = ref(false);
const deleteConfirm = useTemplateRef('deleteConfirm');

async function refreshData() {
    loading.value = true;
    await fetch('/api/trunks')
        .then(res => res.json())
        .then(data => {
            trunks.value = data;
        });

    await fetch('/api/extensions')
        .then(res => res.json())
        .then(data => {
            rows.value = data;
        });

    loading.value = false;
}

async function ring(row: Extension) {
    await fetch(`/api/extensions/${row.extensionNumber}/ring`, {
        method: 'POST'
    });
}

async function deleteExtension(row: Extension) {
    const confirmed = await deleteConfirm.value?.execute();
    if (!confirmed) {
        return;
    }

    const response = await fetch(`/api/extensions/${row.extensionNumber}`, {
        method: 'DELETE'
    });

    if (response.ok) {
        rows.value = rows.value.filter(x => x.extensionNumber !== row.extensionNumber);
    }
}

onMounted(() => {
    refreshData();
});
document.title = t('label.extensions');
</script>

<template>
    <AdminLayout>
        <Confirm ref="deleteConfirm" design="danger" :message="t('message.delete-extension-confirmation')" />
        <Btn design="primary" icon="mdi:plus" :label="t('button.new')" @click="router.push('/admin/extensions/new')" />
        <div class="flex gap-4">
            <DataGrid :loading="loading" allow-selection enable-card :columns="columns" :rows="rows"
                @row-click="row => router.push(`/admin/extensions/${row.extensionNumber}`)">
                <template #cell.connected="{ row }">
                    <span class="size-2.5 rounded-full inline-block" :class="row.connected ? 'bg-green-500' : 'bg-gray-500'"
                        :title="row.connected ? t('label.connected') : t('label.not-connected')"></span>
                </template>
                <template #cell.outgoingTrunk="{ row }">
                    {{trunks.find(t => t.id === row.outgoingTrunkId)?.name }}
                </template>
                <template #actions="{ row }">
                    <DropdownButton :disabled="!row?.connected" icon="mdi:crosshairs-question" label="Identificeren"
                        title="Belt de extensie voor een paar seconden, zodat u kunt horen welk apparaat verbonden is"
                        design="primary" @click="ring(row)" />
                    <DropdownButton icon="mdi:delete" label="Verwijderen" design="danger" @click="deleteExtension(row)" />
                </template>
            </DataGrid>

            <Card class="w-72 flex-none">
                <h2 class="text-base font-semibold text-slate-700 flex gap-2 items-center">
                    <Icon icon="mdi:phone-plus" /> {{ t('label.extensions') }}
                </h2>
                <p>
                    {{ t('label.extensions-instructions-1') }}
                </p>
                <div class="space-y-1">
                    <div>
                        <span class="font-bold">{{ t('label.sip-server') }}:</span> {{ hostname }}
                    </div>
                    <div>
                        <span class="font-bold">{{ t('label.port') }}:</span> 5060
                    </div>
                    <div>
                        <span class="font-bold">{{ t('label.transport') }}:</span> UDP
                    </div>
                    <div>
                        <span class="font-bold">{{ t('label.codecs') }}:</span> alaw, ulaw, g729
                    </div>
                    <div>
                        <span class="font-bold">{{ t('label.domain') }}:</span> {{ hostname }}
                    </div>
                    <div>
                        <span class="font-bold">{{ t('label.username') }}:</span> &lt;{{ t('label.extension-number').toLowerCase() }}&gt;
                    </div>
                    <div>
                        <span class="font-bold">{{ t('label.password') }}:</span> &lt;{{ t('label.password').toLowerCase() }}&gt;
                    </div>
                </div>

                <p>
                    {{ t('label.extensions-instructions-2') }}
                </p>
            </Card>
        </div>
    </AdminLayout>
</template>