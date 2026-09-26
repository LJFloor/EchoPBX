<script setup lang="ts">
import { Icon } from '@iconify/vue';
import { onMounted, onUnmounted, ref } from 'vue';
import { CallDirection,CallState, type  OngoingCall } from '~/types/OngoingCall';
import { CdrDisposition, type CdrEntry } from '~/types/Cdr';
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

const historyColumns = [
    new Column({ field: 'directionIcon', label: ' ', width: 35 }),
    new Column({ field: 'start', label: t('label.time'), width: 200 }),
    new Column({ field: 'source', label: t('label.from'), width: 250 }),
    new Column({ field: 'destination', label: t('label.to'), width: 250 }),
    new Column({ field: 'disposition', label: t('label.status'), width: 150 }),
    new Column({ field: 'billSeconds', label: t('label.duration') }),
];

const history = ref<CdrEntry[]>([]);

function formatSeconds(seconds: number): string {
    return new Date(seconds * 1000).toISOString().substring(11, 19);
}

// Uniqueid => duration string
const pickupDurations = ref<Record<string, string>>({});

const calls = ref<OngoingCall[]>([]);

document.title = t('label.dashboard');

function formatDuration(timestamp: number): string {
    const durationMs = Date.now() - timestamp;
    const durationDate = new Date(durationMs);
    return durationDate.toISOString().substr(11, 8);
}

let unmounted = false;
let websocket: WebSocket | undefined;
let durationInterval: ReturnType<typeof setInterval> | undefined;

onMounted(async () => {
    fetch('/api/cdr?n=25')
        .then(res => res.ok ? res.json() : [])
        .then(data => {
            history.value = data;
        });

    const callsResponse = await fetch('/api/asterisk/ongoing-calls');
    calls.value = await callsResponse.json();

    // The user may have left while the calls were loading
    if (unmounted) return;

    websocket = new WebSocket(`${window.location.protocol.replace('http', 'ws')}//${window.location.host}/api/asterisk/ongoing-calls/live`);
    websocket.onmessage = (event) => {
        const data = JSON.parse(event.data) as OngoingCall[];
        calls.value = data;
    };

    durationInterval = setInterval(() => {
        const newDurations: Record<string, string> = {};
        for (const call of calls.value.filter(c => c.state === CallState.Ongoing)) {
            const timestamp = call.pickupTime ?? call.startTime;
            newDurations[call.uniqueId] = formatDuration(timestamp);
        }
        pickupDurations.value = newDurations;
    }, 100);
});

onUnmounted(() => {
    unmounted = true;
    websocket?.close();
    clearInterval(durationInterval);
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
                        <Icon v-else icon="mdi:phone" class="text-gray-500" />
                    </template>
                    <template #cell.direction="{ row }">
                        <template v-if="row.direction === CallDirection.Incoming">{{ t('label.incoming') }}</template>
                        <template v-else-if="row.direction === CallDirection.Outgoing">{{ t('label.outgoing') }}</template>
                        <template v-else>{{ t('label.internal') }}</template>
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
                        <span v-else-if="row.state === CallState.Ringing && row.queueId" class="text-yellow-600 font-semibold">{{ t('label.in-queue') }}</span>
                        <span v-else-if="row.state === CallState.Ongoing" class="text-green-600 font-semibold">{{ t('label.connected') }}</span>
                    </template>

                    <template #empty>
                        {{ t('label.no-ongoing-calls') }}
                    </template>
                </DataGrid>
            </div>

            <div class="bg-white p-4 col-span-3 rounded shadow space-y-4">
                <h2 class="text-lg text-slate-700 font-semibold mb-2">{{ t('label.recent-calls') }}</h2>
                <DataGrid :rows="history" :columns="historyColumns">
                    <template #cell.directionIcon="{ row }">
                        <Icon v-if="row.direction === CallDirection.Incoming" icon="mdi:phone-incoming" class="text-green-500" />
                        <Icon v-else-if="row.direction === CallDirection.Outgoing" icon="mdi:phone-outgoing" class="text-blue-500" />
                        <Icon v-else icon="mdi:phone" class="text-gray-500" />
                    </template>
                    <template #cell.start="{ row }">
                        {{ new Date(row.start).toLocaleString() }}
                    </template>
                    <template #cell.disposition="{ row }">
                        <span v-if="row.disposition === CdrDisposition.Answered" class="text-green-600">{{ t('label.answered') }}</span>
                        <span v-else-if="row.disposition === CdrDisposition.Busy" class="text-yellow-600">{{ t('label.busy') }}</span>
                        <span v-else class="text-red-600">{{ t('label.not-answered') }}</span>
                    </template>
                    <template #cell.billSeconds="{ row }">
                        <span v-if="row.disposition === CdrDisposition.Answered">{{ formatSeconds(row.billSeconds) }}</span>
                    </template>

                    <template #empty>
                        {{ t('label.no-recent-calls') }}
                    </template>
                </DataGrid>
            </div>
        </div>
    </AdminLayout>
</template>