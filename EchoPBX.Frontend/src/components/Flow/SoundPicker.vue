<script lang="ts">
// Shared by every picker on the page, so the list is only fetched once.
let builtinSoundsRequest: Promise<string[]> | undefined;
</script>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import Btn from '~/components/Button/Btn.vue';
import RadioButton from '~/components/Radio/RadioButton.vue';
import FileInput from '~/components/Input/FileInput.vue';
import Select from '~/components/Select/Select.vue';
import { useTranslation } from '~/composables/useTranslation';
import { SoundKind } from '~/types/CallFlow';

const props = defineProps<{
    kind: SoundKind;
    sound?: string | null;
}>();

const emit = defineEmits<{
    change: [changes: { kind?: SoundKind; sound: string | null }];
}>();

const { t } = useTranslation();
const builtinSounds = ref<string[]>();

onMounted(async () => {
    builtinSoundsRequest ??= fetch('/api/system/builtin-sounds')
        .then(res => res.ok ? res.json() : [])
        .catch(() => []);

    builtinSounds.value = await builtinSoundsRequest;
});
</script>

<template>
    <div class="space-y-3">
        <RadioButton :model-value="props.kind" :value="SoundKind.Upload" :label="t('label.upload-sound')"
            @update:model-value="emit('change', { kind: SoundKind.Upload, sound: null })" />
        <RadioButton :model-value="props.kind" :value="SoundKind.Builtin" :label="t('label.built-in-sound')"
            @update:model-value="emit('change', { kind: SoundKind.Builtin, sound: null })" />

        <div v-if="props.kind === SoundKind.Upload" class="space-y-2">
            <FileInput accept="audio/*" @upload="emit('change', { sound: $event.map(f => f.dataUrl)[0] ?? null })" />
            <div v-if="props.sound" class="flex items-center gap-1">
                <audio :src="props.sound" controls class="w-full"></audio>
                <Btn design="icon-danger-secondary" icon="mdi:delete" @click="emit('change', { sound: null })" />
            </div>
        </div>

        <div v-else>
            <Select :items="builtinSounds ?? []" :model-value="props.sound ?? undefined"
                @update:model-value="emit('change', { sound: $event ?? null })">
                <template #item="{ value }">{{ value }}</template>
            </Select>
            <p v-if="builtinSounds && builtinSounds.length === 0" class="text-xs text-slate-500 mt-1">
                {{ t('label.no-built-in-sounds') }}
            </p>
        </div>
    </div>
</template>
