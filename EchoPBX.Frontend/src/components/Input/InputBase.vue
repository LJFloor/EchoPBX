<script setup lang="ts">
defineProps<{
    disabled?: boolean,
}>();
</script>

<template>
    <div class="w-full text-sm bg-gray-50 border duration-100 border-gray-300 hover:border-gray-400 focus-within:border-gray-600 focus-within:hover:border-gray-600 h-8 rounded overflow-hidden"
        :class="{
            'opacity-50 pointer-events-none bg-gray-300': disabled
        }">
        <slot></slot>
    </div>
</template>