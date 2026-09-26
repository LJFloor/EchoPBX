<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import Card from '~/components/Card/Card.vue';
import Textbox from '~/components/Input/Textbox.vue';
import Btn from '~/components/Button/Btn.vue';
import type { Queue } from '~/types/Queue';
import RadioButton from '~/components/Radio/RadioButton.vue';
import type { Extension } from '~/types/Extension';
import FileInput from '~/components/Input/FileInput.vue';
import ExtensionList from '~/components/ExtensionList.vue';
import AdminLayout from '~/layouts/AdminLayout.vue';
import { useTranslation } from '~/composables/useTranslation';
import { useEscape } from '~/composables/useEscape';

const route = useRoute();
const router = useRouter();

useEscape(() => router.push('/admin/queues'));
const { t } = useTranslation();

const queue = ref<Queue>();
const extensions = ref<Extension[]>();
const isSaving = ref(false);
const error = ref<string>();

onMounted(async () => {
    fetch('/api/extensions')
        .then(res => res.json())
        .then(data => {
            extensions.value = data;
        });

    if (route.params.queueId === 'new') {
        document.title = t('label.new-queue');
        queue.value = {
            name: '',
            extensions: [],
            strategy: 'ringall',
            timeout: 30,
            wrapUpTime: 0,
            id: undefined!,
            maxLength: 0,
            retryInterval: 0,
            musicOnHold: [],
        };
        return;
    } else {
        await fetch(`/api/queues/${route.params.queueId}`)
            .then(res => res.json())
            .then(data => {
                queue.value = data;
            });

        if (queue.value?.name) {
            document.title = queue.value?.name;
        }
    }
});

async function save() {
    if (!queue.value) return;

    isSaving.value = true;
    error.value = undefined;
    const response = await fetch(`/api/queues`, {
        method: route.params.queueId === 'new' ? 'POST' : 'PUT',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(queue.value)
    });

    isSaving.value = false;

    if (!response.ok) {
        // A 4xx carries a message meant for the user, a 5xx only a stack trace at best
        const message = response.status < 500 ? await response.text() : '';
        error.value = message || t('message.something-went-wrong');
        return;
    }

    router.push('/admin/queues');
}
</script>

<template>
    <AdminLayout>
        <form @submit.prevent="save" v-if="queue && extensions" class="space-y-2">
            <div class="flex justify-end gap-2">
                <Btn @click="router.push('/admin/queues')" design="secondary" :label="t('button.cancel')" />
                <Btn type="submit" :loading="isSaving" design="primary" :label="t('button.save')" />
            </div>

            <div v-if="error" class="bg-red-50 border border-red-300 text-red-700 rounded px-4 py-2">
                {{ error }}
            </div>

            <Card>
                <div class="p-4">
                    <div class="grid grid-cols-2 gap-4">
                        <div class="font-semibold h-8 flex items-center">{{ t('label.name') }}:</div>
                        <Textbox :minlength="3" :required="true" v-model="queue.name" />

                        <div class="font-semibold h-8 flex items-center">{{ t('label.extensions') }}:</div>
                        <ExtensionList v-model="queue.extensions" :available-extensions="extensions" />

                        <div class="h-8 flex items-center">{{ t('label.timeout-for-extension-seconds') }}:</div>
                        <Textbox type="number" v-model.number="queue.timeout" />

                        <div class="h-8 flex items-center">{{ t('label.wrap-up-time-for-extension-seconds') }}:</div>
                        <Textbox type="number" v-model.number="queue.wrapUpTime" />

                        <div class="h-8 flex items-center">{{ t('label.maximum-size') }}:</div>
                        <Textbox type="number" v-model.number="queue.maxLength" />

                        <div class="h-8 flex items-center">{{ t('label.announcement') }}:</div>
                        <div class="space-y-2">
                            <FileInput accept="audio/*"
                                @upload="queue.announcement = $event.map(file => file.dataUrl)[0]" />
                            <div v-if="queue.announcement" class="flex items-center gap-1">
                                <audio :src="queue.announcement" controls class="w-full"></audio>
                                <Btn design="icon-danger-secondary" icon="mdi:delete"
                                    @click="queue.announcement = undefined" />
                            </div>
                        </div>

                        <div class="h-8 flex items-center">{{ t('label.custom-music-on-hold') }}:</div>
                        <div class="space-y-2">
                            <div class="space-y-2">
                                <FileInput accept="audio/*" multiple
                                    @upload="queue.musicOnHold = [...queue.musicOnHold, ...$event.map(file => file.dataUrl)]" />
                                <div v-for="musicUrl in queue.musicOnHold" class="flex items-center gap-1">
                                    <audio :src="musicUrl" controls class="w-full"></audio>
                                    <Btn v-if="queue.musicOnHold.length" design="icon-danger-secondary"
                                        icon="mdi:delete"
                                        @click="queue.musicOnHold = queue.musicOnHold.filter(x => x !== musicUrl)" />
                                </div>
                            </div>
                        </div>

                        <div class="h-8 flex items-center">{{ t('label.strategy') }}:</div>
                        <div>
                            <RadioButton v-model="queue.strategy" value="ringall"
                                :description="t('label.ringall-description')"
                                :label="`ringall - ${t('label.ringall')}`" />
                            <RadioButton v-model="queue.strategy" value="rrmemory"
                                :description="t('label.rrmemory-description')"
                                :label="`rrmemory - ${t('label.rrmemory')}`" />
                            <RadioButton v-model="queue.strategy" value="rrordered"
                                :description="t('label.rrordered-description')"
                                :label="`rrordered - ${t('label.rrordered')}`" />
                            <RadioButton v-model="queue.strategy" value="fewestcalls"
                                :description="t('label.fewestcalls-description')"
                                :label="`fewestcalls - ${t('label.fewestcalls')}`" />
                            <RadioButton v-model="queue.strategy" value="random"
                                :description="t('label.random-description')"
                                :label="`random - ${t('label.random')}`" />
                        </div>
                    </div>
                </div>
            </Card>
        </form>
    </AdminLayout>
</template>