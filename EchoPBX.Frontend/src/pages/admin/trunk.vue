<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import Card from '~/components/Card/Card.vue';
import Btn from '~/components/Button/Btn.vue';
import { IncomingCallBehaviour, type Trunk } from '~/types/Trunk';
import Textbox from '~/components/Input/Textbox.vue';
import Checkbox from '~/components/Checkbox/Checkbox.vue';
import RadioButton from '~/components/Radio/RadioButton.vue';
import type { Extension } from '~/types/Extension';
import ExtensionList from '~/components/ExtensionList.vue';
import Select from '~/components/Select/Select.vue';
import type { Queue } from '~/types/Queue';
import type { CallFlow } from '~/types/CallFlow';
import AdminLayout from '~/layouts/AdminLayout.vue';
import { useTranslation } from '~/composables/useTranslation';
import { useMemo } from '~/composables/useMemo';

const route = useRoute();
const router = useRouter();
const { t } = useTranslation();

const trunk = ref<Trunk>();
const extensions = useMemo<Extension[]>('extensions', () => []);
const queues = useMemo<Queue[]>('queues', () => []);
const callFlows = useMemo<CallFlow[]>('call-flows', () => []);
const isSaving = ref(false);

onMounted(async () => {
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            router.push('/admin/trunks');
        }
    }, { once: true });

    await fetch(`/api/extensions`)
        .then(res => res.json())
        .then(data => {
            extensions.value = data;
        });

    await fetch(`/api/queues`)
        .then(res => res.json())
        .then(data => {
            queues.value = data;
        });

    await fetch(`/api/call-flows`)
        .then(res => res.json())
        .then(data => {
            callFlows.value = data;
        });

    if (route.params.trunkId === 'new') {
        document.title = t('label.new-trunk');
        trunk.value = {
            codecs: ['alaw', 'g729'],
            id: undefined!,
            name: '',
            host: '',
            cid: '',
            username: '',
            password: '',
            connected: false,
            incomingCallBehaviour: IncomingCallBehaviour.Ignore,
            extensions: [],
        };
        return;
    } else {
        await fetch(`/api/trunks/${route.params.trunkId}`)
            .then(res => res.json())
            .then(data => {
                trunk.value = data;
            });

        if (trunk.value?.name) {
            document.title = trunk.value?.name;
        }
    }
})

async function save() {
    if (!trunk.value) return;

    isSaving.value = true;
    await fetch(`/api/trunks`, {
        method: route.params.trunkId === 'new' ? 'POST' : 'PUT',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(trunk.value)
    });

    isSaving.value = false;
    router.push('/admin/trunks');
}
</script>

<template>
    <AdminLayout>
        <form @submit.prevent="save" v-if="trunk" class="space-y-2">
            <div class="flex justify-end gap-2">
                <Btn @click="router.push('/admin/trunks')" design="secondary" label="Annuleren" />
                <Btn type="submit" :loading="isSaving" design="primary" label="Opslaan" />
            </div>
            <Card>
                <div class="p-4">
                    <div class="grid grid-cols-2 gap-4">
                        <div class="font-semibold h-8 flex items-center">{{ t('label.name') }}:</div>
                        <Textbox :minlength="3" :required="true" v-model="trunk.name" type="text" />

                        <div class="font-semibold h-8 flex items-center">{{ t('label.host') }}:</div>
                        <Textbox :required="true" v-model="trunk.host" type="text" />

                        <div class="font-semibold h-8 flex items-center">{{ t('label.username') }}:</div>
                        <Textbox :required="true" v-model="trunk.username" type="text" />

                        <div class="font-semibold h-8 flex items-center">{{ t('label.password') }}:</div>
                        <Textbox :required="true" v-model="trunk.password" type="secret" />

                        <div class="h-8 flex items-center">{{ t('label.cid') }}:</div>
                        <div>
                            <Textbox v-model="trunk.cid" type="text" />
                            <div class="text-gray-500 mt-0.5">{{ t('label.cid-explanation') }}</div>
                        </div>

                        <div class="h-8 flex items-center">{{ t('label.codecs') }}:</div>
                        <div class="space-y-2">
                            <Checkbox v-model="trunk.codecs" value="ulaw" label="ulaw" />
                            <Checkbox v-model="trunk.codecs" value="alaw" label="alaw" />
                            <Checkbox v-model="trunk.codecs" value="g729" label="g729" />
                        </div>

                        <div class="h-8 flex items-center">{{ t('label.when-a-call-comes-in') }}:</div>
                        <div class="space-y-2">
                            <RadioButton v-model="trunk.incomingCallBehaviour" :value="IncomingCallBehaviour.Ignore"
                                :label="t('label.ignore-call')" :description="t('label.ignore-call-description')" />

                            <RadioButton v-model="trunk.incomingCallBehaviour"
                                :value="IncomingCallBehaviour.RingAllExtensions" :label="t('label.ring-all-extensions')"
                                :description="t('label.ring-all-extensions-description')" />
                            <div>
                                <RadioButton v-model="trunk.incomingCallBehaviour"
                                    :value="IncomingCallBehaviour.RingSpecificExtensions"
                                    :label="t('label.ring-specific-extensions')" />
                                <div class="pl-8">
                                    <ExtensionList v-model="trunk.extensions" :available-extensions="extensions"
                                        :disabled="trunk.incomingCallBehaviour !== IncomingCallBehaviour.RingSpecificExtensions" />
                                </div>
                            </div>

                            <div>
                                <RadioButton v-model="trunk.incomingCallBehaviour"
                                    :value="IncomingCallBehaviour.RingQueue" :label="t('label.transfer-to-queue')" />
                                <div class="pl-8">

                                    <Select v-model="trunk.queueId" :items="queues.map(q => q.id)"
                                        :disabled="trunk.incomingCallBehaviour !== IncomingCallBehaviour.RingQueue">
                                        <template #item="{ value }">
                                            {{(queues.find(q => q.id === value)?.name || t('label.unknown-queue'))}}
                                        </template>
                                    </Select>
                                </div>
                            </div>

                            <div>
                                <RadioButton v-model="trunk.incomingCallBehaviour"
                                    :value="IncomingCallBehaviour.SendToCallFlow" :label="t('label.send-to-call-flow')"
                                    :description="t('label.send-to-call-flow-description')" />
                                <div class="pl-8">
                                    <Select v-model="trunk.callFlowId" :items="callFlows.map(x => x.id)"
                                        :disabled="trunk.incomingCallBehaviour !== IncomingCallBehaviour.SendToCallFlow">
                                        <template #item="{ value }">
                                            {{ callFlows.find(x => x.id === value)?.name || t('label.unknown-call-flow') }}
                                        </template>
                                    </Select>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </Card>
        </form>
    </AdminLayout>
</template>