<script setup lang="ts">
defineProps<{
    title?: string;
}>();
</script>

<template>
    <div class="bg-white p-4 col-span-3 rounded shadow">
        <h2 v-if="title" class="text-lg text-slate-700 font-semibold mb-2">{{ title }}</h2>
        <div class="space-y-2">
            <slot></slot>
        </div>
    </div>
</template>