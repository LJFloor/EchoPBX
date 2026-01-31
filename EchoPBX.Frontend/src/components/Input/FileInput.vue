<script setup lang="ts">
import { ref } from 'vue';
import Btn from '~/components/Button/Btn.vue';
import type { UploadedFile } from './UploadedFile';
import { useTranslation } from '~/composables/useTranslation';

const { t } = useTranslation();
const props = defineProps<{

    /**
     * A comma-separated list of accepted file types (MIME types or file extensions).
     */
    accept?: string;

    /**
     * If true, allows multiple file selection.
     */
    multiple?: boolean;

    /**
     * Maximum size of all the files combined in bytes. Undefined means no limit.
     */
    maxSize?: number;
}>();

const files = ref<UploadedFile[]>([]);
const error = ref<string>();

function onUpload(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files) {
        return;
    }

    const newFiles: UploadedFile[] = [];

    for (let i = 0; i < input.files.length; i++) {
        const file = input.files[i];
        if (!file) {
            continue;
        }

        const uploadedFile: UploadedFile = {
            name: file.name,
            size: file.size,
            type: file.type,
            lastModified: file.lastModified,
            dataUrl: undefined!
        };


        const reader = new FileReader();
        reader.onload = (e) => {
            uploadedFile.dataUrl = e.target?.result as string;
            newFiles.push(uploadedFile);
            if (props.maxSize) {
                const totalSize = newFiles.reduce((sum, f) => sum + f.size, 0);
                if (totalSize > props.maxSize) {
                    error.value = `${input.files!.length > 1 ? t('label.files-too-big') : t('label.file-too-big')} (${(totalSize / 1024 / 1024).toFixed(2)} MB > ${(props.maxSize / 1024 / 1024).toFixed(2)} MB)`;
                    files.value = [];
                    emit('upload', files.value);
                    return;
                } else {
                    error.value = undefined;
                }
            }

            if (newFiles.length === input.files!.length) {
                files.value = newFiles;
                error.value = undefined;
                emit('upload', files.value);
            }
        };
        reader.readAsDataURL(file);

    }
}

const emit = defineEmits<{
    (e: 'upload', files: UploadedFile[]): void
}>();
</script>

<template>
    <div class="flex gap-2 items-center">
        <input class="hidden" type="file" :accept="accept" :multiple="multiple" @change="onUpload" />
        <div class="flex-none">
            <slot name="button">
                <Btn design="secondary" :label="multiple ? t('button.select-files') : t('button.select-file')"
                    @click="$el.querySelector('input[type=file]')!.click()" />
            </slot>
        </div>
        <div class="truncate text-gray-500">
            <div v-if="files.length === 0 && !error && maxSize">
                Max. {{ (maxSize / 1024 / 1024).toFixed(2) }} MB {{ multiple ? t('label.total').toLowerCase() : '' }}
            </div>
            <span v-if="error" class="text-red-500">{{ error }}</span>
            <span v-else-if="files.length > 1">
                {{ files.length }} {{ t('label.files-selected').toLowerCase() }} ({{(files.reduce((sum, file) => sum + file.size, 0) / 1024 /
                    1024).toFixed(2) }} MB)
            </span>
            <span v-else-if="files[0]" :title="files[0].name">
                {{ files[0].name }} ({{ (files[0].size / 1024 / 1024).toFixed(2) }} MB)
            </span>
        </div>
    </div>
</template>