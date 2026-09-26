<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import type { Extension } from '~/types/Extension';
import Card from '~/components/Card/Card.vue';
import Textbox from '~/components/Input/Textbox.vue';
import Btn from '~/components/Button/Btn.vue';
import type { Trunk } from '~/types/Trunk';
import RadioButton from '~/components/Radio/RadioButton.vue';
import { Icon } from '@iconify/vue';
import { useTranslation } from '~/composables/useTranslation';
import { useEscape } from '~/composables/useEscape';
import { generateStr } from '~/helper/stringHelper';
import AdminLayout from '~/layouts/AdminLayout.vue';
import { useMemo } from '~/composables/useMemo';

const route = useRoute();
const router = useRouter();

useEscape(() => router.push('/admin/extensions'));
const { t } = useTranslation();

const extension = ref<Extension>();
const trunks = useMemo<Trunk[]>('trunks', () => []);
const isSaving = ref(false);
const error = ref<string>();

onMounted(async () => {
    fetch(`/api/trunks`)
        .then(res => res.json())
        .then(data => {
            trunks.value = data;
        });

    if (route.params.extensionNumber === 'new') {
        document.title = t('label.new-extension');
        extension.value = {
            extensionNumber: null!,
            password: generateStr(),
            displayName: '',
            maxDevices: 5,
            connected: false,
            outgoingTrunkId: null,
        };
        return;
    } else {
        document.title = `${t('label.extension')} ${route.params.extensionNumber}`;
        extension.value = await fetch(`/api/extensions/${route.params.extensionNumber}`).then(res => res.json());
    }

})

async function save() {
    if (!extension.value) return;

    isSaving.value = true;
    error.value = undefined;
    const response = await fetch(`/api/extensions`, {
        method: route.params.extensionNumber === 'new' ? 'POST' : 'PUT',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(extension.value)
    });

    isSaving.value = false;

    if (!response.ok) {
        // A 4xx carries a message meant for the user, a 5xx only a stack trace at best
        const message = response.status < 500 ? await response.text() : '';
        error.value = message || t('message.something-went-wrong');
        return;
    }

    router.push('/admin/extensions');
}
</script>

<template>
    <AdminLayout>
        <form @submit.prevent="save" v-if="extension" class="space-y-2">
            <div class="flex justify-end gap-2">
                <Btn @click="router.push('/admin/extensions')" design="secondary" :label="t('button.cancel')" />
                <Btn type="submit" :loading="isSaving" design="primary" :label="t('button.save')" />
            </div>

            <div v-if="error" class="bg-red-50 border border-red-300 text-red-700 rounded px-4 py-2">
                {{ error }}
            </div>

            <Card>
                <h2 class="text-lg font-medium">{{ t('label.general') }}</h2>
                <p>{{ t('label.extension-general-description') }}</p>
                <div class="p-4 grid grid-cols-2 gap-4">
                    <div class="font-semibold h-8 flex items-center">{{ t('label.extension-number') }}:</div>
                    <Textbox :disabled="route.params.extensionNumber !== 'new'" :minlength="3" :required="true"
                        v-model="extension.extensionNumber" type="number" />

                    <div class="font-semibold h-8 flex items-center">{{ t('label.password') }}:</div>
                    <Textbox :required="true" v-model="extension.password" type="secret" />

                    <div class="h-8 flex items-center">{{ t('label.name') }}:</div>
                    <Textbox :maxlength="15" v-model="extension.displayName" type="text" />

                    <div class="font-semibold h-8 flex items-center">{{ t('label.max-devices') }}:</div>
                    <Textbox :required="true" v-model="extension.maxDevices" type="number" />

                    <div class="h-8 flex items-center">{{ t('label.outgoing-trunk') }}:</div>
                    <div>
                        <template v-if="trunks.length > 0">
                            <RadioButton v-model="extension.outgoingTrunkId" :value="null" :label="t('label.no-trunk')"
                                :description="t('label.no-trunk-description')" />
                            <RadioButton v-for="trunk in trunks" :key="trunk.id" v-model="extension.outgoingTrunkId"
                                :value="trunk.id" :label="trunk.name" :description="trunk.cid || trunk.host" />
                        </template>
                        <template v-else>
                            <div class="text-base text-gray-500 inline-flex gap-2">
                                <Icon icon="mdi:information-outline" class="size-6" />
                                {{ t('label.no-trunks-available-description') }}
                            </div>
                        </template>
                    </div>
                </div>
            </Card>
        </form>
    </AdminLayout>
</template>