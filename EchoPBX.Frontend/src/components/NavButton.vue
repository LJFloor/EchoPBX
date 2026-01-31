<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();

const props = defineProps<{
    label: string;
    href: string;
}>();
const active = computed(() => route.path.startsWith(props.href));
</script>

<template>
    <div class="relative h-full nav-button">
        <RouterLink v-if="href" :to="href"
            class="h-full flex items-center gap-2 px-4 text-white duration-100 cursor-pointer font-semibold "
            :class="active ? 'bg-sky-800' : 'hover:bg-sky-700'">
            {{ label }}
        </RouterLink>
    </div>
</template>