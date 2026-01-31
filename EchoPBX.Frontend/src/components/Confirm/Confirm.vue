<script setup lang="ts">
import { ref } from 'vue';
import Btn from '~/components/Button/Btn.vue';
import { useTranslation } from '~/composables/useTranslation';

const { t } = useTranslation();

withDefaults(defineProps<{
    message?: string;
    design?: 'primary' | 'danger';
}>(), {
    design: 'primary'
});

const isOpen = ref(false);
const answer = ref<boolean | null>(null);

async function execute(): Promise<boolean> {
    answer.value = null;
    isOpen.value = true;

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && isOpen.value) {
            answer.value = false;
        }
    }, { once: true });

    while (answer.value === null) {
        await new Promise(resolve => setTimeout(resolve, 50));
    }

    isOpen.value = false;
    return answer.value;
}

defineExpose({
    execute
});
</script>

<template>
    <Teleport v-if="isOpen" to="body">
        <div class="fixed inset-0 bg-black/50 z-40"></div>
        <div class="fixed inset-0 flex items-center justify-center z-50 text-sm">
            <div class="bg-white rounded shadow-lg w-120">
                <div class="p-6">

                    <h2 class="text-lg font-semibold mb-4">{{ t('button.confirm') }}</h2>
                    <p class="mb-6">{{ message || "Weet u zeker dat u deze actie wilt uitvoeren?" }}</p>
                </div>
                <div class="flex justify-end space-x-2 px-4 py-2 border-t border-gray-200">
                    <Btn :label="t('button.cancel')" design="secondary" @click="answer = false" />
                    <Btn :design="design" :label="t('button.confirm')" @click="answer = true" />
                </div>
            </div>
        </div>
    </Teleport>
</template>