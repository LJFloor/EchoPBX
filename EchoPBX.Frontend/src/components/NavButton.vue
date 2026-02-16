<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();

const props = defineProps<{
    label: string;
    href?: string;
}>();

const active = computed(() => {
    if (props.href) {
        return route.path.startsWith(props.href);
    }

    return false;
});

const emit = defineEmits<{
    (e: 'click', event: MouseEvent): void
}>();
</script>

<template>
    <div class="relative h-full nav-button text-white duration-100 cursor-pointer font-semibold"
        :class="active ? 'bg-sky-800' : 'hover:bg-sky-700'">
        <RouterLink v-if="props.href" :to="props.href" class="h-full flex items-center gap-2 px-4" @click="$emit('click', $event)">
            {{ props.label }}
        </RouterLink>
        <button v-else class="h-full flex items-center gap-2 px-4 cursor-pointer" @click="$emit('click', $event)">
            {{ props.label }}
        </button>
    </div>
</template>