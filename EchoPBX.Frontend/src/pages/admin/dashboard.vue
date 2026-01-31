<script setup lang="ts">
import { Icon } from '@iconify/vue';
import { onMounted, ref } from 'vue';
import { CallDirection,CallState, type  OngoingCall } from '~/types/OngoingCall';
import { Column } from '~/components/DataGrid/Column';
import DataGrid from '~/components/DataGrid/DataGrid.vue';
import AdminLayout from '~/layouts/AdminLayout.vue';
import { useTranslation } from '~/composables/useTranslation';


const { t } = useTranslation();
const callColumns = [
    new Column({ field: 'directionIcon', label: ' ', width: 35 }),
    new Column({ field: 'direction', label: t('label.type'), width: 150 }),
    new Column({ field: 'externalNumber', label: t('label.phone-number'), width: 350 }),
    new Column({ field: 'extensionNumber', label: t('label.extension-number'), width: 300 }),
    new Column({ field: 'state', label: t('label.status'), width: 150 }),
    new Column({ field: 'duration', label: t('label.duration') }),
];

// Uniqueid => duration string
const pickupDurations = ref<Record<string, string>>({});

const calls = ref<OngoingCall[]>([]);

document.title = t('label.dashboard');

function formatDuration(timestamp: number): string {
    const durationMs = Date.now() - timestamp;
    const durationDate = new Date(durationMs);
    return durationDate.toISOString().substr(11, 8);
}

onMounted(async () => {
    const callsResponse = await fetch('/api/asterisk/ongoing-calls');
    calls.value = await callsResponse.json();

    const websocket = new WebSocket(`${window.location.protocol.replace('http', 'ws')}//${window.location.host}/api/asterisk/ongoing-calls/live`);
    websocket.onmessage = (event) => {
        const data = JSON.parse(event.data) as OngoingCall[];
        calls.value = data;
    };

    setInterval(() => {
        const newDurations: Record<string, string> = {};
        for (const call of calls.value.filter(c => c.state === CallState.Ongoing)) {
            const timestamp = call.pickupTime ?? call.startTime;
            newDurations[call.uniqueId] = formatDuration(timestamp);
        }
        pickupDurations.value = newDurations;
    }, 100);
});
</script>

<template>
    <AdminLayout>
        <div class="grid grid-cols-3 gap-4">
            <div class="bg-white p-4 col-span-3 rounded shadow space-y-4">
                <h2 class="text-lg text-slate-700 font-semibold mb-2">{{ t('label.ongoing-calls') }}</h2>
                <p>{{ t('label.table-live-data') }}</p>
                <DataGrid :rows="calls" :columns="callColumns">
                    <template #cell.directionIcon="{ row }">
                        <Icon v-if="row.direction === CallDirection.Incoming" icon="mdi:phone-incoming" class="text-green-500" />
                        <Icon v-else-if="row.direction === CallDirection.Outgoing" icon="mdi:phone-outgoing" class="text-blue-500" />
                    </template>
                    <template #cell.direction="{ row }">
                        {{ row.direction === CallDirection.Incoming ? t('label.incoming') : t('label.outgoing') }}
                    </template>
                    <template #cell.duration="{ row }">
                        <span>{{ pickupDurations[row.uniqueId] }}</span>
                    </template>
                    <template #cell.externalNumber="{ row }">
                        <span>{{ row.externalNumber }}</span>
                        <span v-if="row.externalName" class="text-gray-500 ml-2">({{ row.externalName }})</span>
                    </template>
                    <template #cell.extensionNumber="{ row }">
                        <span>{{ row.extensionNumber }}</span>
                        <span v-if="row.extensionName" class="text-gray-500 ml-2">({{ row.extensionName }})</span>
                    </template>
                    <template #cell.state="{ row }">
                        <span v-if="row.state === CallState.Ringing && !row.queueId" class="text-yellow-600 font-semibold">{{ t('label.ringing') }}</span>
                        <span v-if="row.state === CallState.Ringing && row.queueId" class="text-yellow-600 font-semibold">{{ t('label.in-queue') }}</span>
                        <span v-else-if="row.state === CallState.Ongoing" class="text-green-600 font-semibold">{{ t('label.connected') }}</span>
                    </template>

                    <template #empty>
                        {{ t('label.no-ongoing-calls') }}
                    </template>
                </DataGrid>
            </div>
        </div>
    </AdminLayout>
</template>