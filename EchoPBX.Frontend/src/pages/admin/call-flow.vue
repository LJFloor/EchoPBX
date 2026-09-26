<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import Card from '~/components/Card/Card.vue';
import Textbox from '~/components/Input/Textbox.vue';
import Btn from '~/components/Button/Btn.vue';
import Select from '~/components/Select/Select.vue';
import FlowCanvas from '~/components/Flow/FlowCanvas.vue';
import AdminLayout from '~/layouts/AdminLayout.vue';
import type { CallFlow } from '~/types/CallFlow';
import type { Extension } from '~/types/Extension';
import { useTranslation } from '~/composables/useTranslation';

const route = useRoute();
const router = useRouter();
const { t } = useTranslation();

const callFlow = ref<CallFlow>();
const extensions = ref<Extension[]>();
const isSaving = ref(false);
const error = ref<string>();
const testExtension = ref<Extension>();
const isNew = computed(() => route.params.slug === 'new');

const startSubtitle = computed(() => {
    const number = callFlow.value?.internalNumber;
    const trunks = callFlow.value?.trunks?.join(', ');

    if (trunks && number) return t('label.call-flow-trigger-trunks-and-number', trunks, number);
    if (trunks) return t('label.call-flow-trigger-trunks', trunks);
    if (number) return t('label.call-flow-trigger-number', number);
    return t('label.call-flow-trigger-test-only');
});

onMounted(async () => {
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            router.push('/admin/call-flows');
        }
    }, { once: true });

    fetch('/api/extensions')
        .then(res => res.json())
        .then(data => {
            extensions.value = data;
        });

    if (isNew.value) {
        document.title = t('label.new-call-flow');
        callFlow.value = {
            id: undefined!,
            name: '',
            slug: '',
            internalNumber: null,
            steps: 0,
            definition: {
                nodes: [{ id: 'start', type: 'start' }],
                edges: [],
            },
        };
        return;
    }

    await fetch(`/api/call-flows/${route.params.slug}`)
        .then(res => res.json())
        .then(data => {
            callFlow.value = data;
        });

    if (callFlow.value?.name) {
        document.title = callFlow.value.name;
    }
});

async function save() {
    if (!callFlow.value) return;

    isSaving.value = true;
    error.value = undefined;

    // Clearing the number box leaves an empty string behind, which the API cannot bind to int?.
    const internalNumber = Number(callFlow.value.internalNumber);
    callFlow.value.internalNumber = Number.isFinite(internalNumber) && internalNumber > 0 ? internalNumber : null;

    const response = await fetch('/api/call-flows', {
        method: isNew.value ? 'POST' : 'PUT',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(callFlow.value)
    });

    isSaving.value = false;

    if (!response.ok) {
        error.value = await response.text() || t('message.something-went-wrong');
        return;
    }

    router.push('/admin/call-flows');
}

async function testCall() {
    if (!callFlow.value || !testExtension.value) return;

    await fetch(`/api/call-flows/${callFlow.value.id}/test?to=${testExtension.value.extensionNumber}`, {
        method: 'POST'
    });
}
</script>

<template>
    <AdminLayout>
        <form @submit.prevent="save" v-if="callFlow" class="space-y-2">
            <div class="flex justify-end gap-2">
                <Btn @click="router.push('/admin/call-flows')" design="secondary" :label="t('button.cancel')" />
                <Btn type="submit" :loading="isSaving" design="primary" :label="t('button.save')" />
            </div>

            <div v-if="error" class="bg-red-50 border border-red-300 text-red-700 rounded px-4 py-2">
                {{ error }}
            </div>

            <Card>
                <div class="p-4">
                    <div class="grid grid-cols-2 gap-4">
                        <div class="font-semibold h-8 flex items-center">{{ t('label.name') }}:</div>
                        <Textbox :minlength="1" :maxlength="128" :required="true" v-model="callFlow.name" />

                        <template v-if="!isNew">
                            <div class="h-8 flex items-center">{{ t('label.slug') }}:</div>
                            <div>
                                <div class="h-8 flex items-center font-mono text-sm">{{ callFlow.slug }}</div>
                                <p class="text-xs text-slate-500 mt-1">{{ t('label.slug-description') }}</p>
                            </div>
                        </template>

                        <div class="h-8 flex items-center">{{ t('label.internal-number') }}:</div>
                        <div>
                            <Textbox type="number" v-model.number="callFlow.internalNumber" />
                            <p class="text-xs text-slate-500 mt-1">{{ t('label.internal-number-description') }}</p>
                        </div>

                        <template v-if="!isNew">
                            <div class="h-8 flex items-center">{{ t('button.test-call') }}:</div>
                            <div class="flex gap-2">
                                <div class="flex-1">
                                    <Select :items="extensions ?? []" v-model="testExtension">
                                        <template #item="{ value }">
                                            {{ value.extensionNumber }} {{ value.displayName }}
                                        </template>
                                    </Select>
                                </div>
                                <Btn design="secondary" icon="mdi:phone" :disabled="!testExtension"
                                    :label="t('button.test-call')" @click="testCall" />
                            </div>
                        </template>
                    </div>
                </div>
            </Card>

            <FlowCanvas v-model="callFlow.definition" :start-subtitle="startSubtitle" />
        </form>
    </AdminLayout>
</template>
