<script setup lang="ts">
import { computed, nextTick, onMounted, provide, ref, watch } from 'vue';
import { VueFlow, useVueFlow, type Edge, type Node } from '@vue-flow/core';
import { Background } from '@vue-flow/background';
import { Icon } from '@iconify/vue';
import Action from './Action.vue';
import Branch from './Branch.vue';
import SoundPicker from './SoundPicker.vue';
import { newNodeId, nodeType, nodeTypes, type NodeTypeDefinition } from './nodeTypes';
import { layoutTree } from './treeLayout';
import { flowContextKey } from './flowContext';
import Modal from '~/components/Modal/Modal.vue';
import Btn from '~/components/Button/Btn.vue';
import Textbox from '~/components/Input/Textbox.vue';
import Select from '~/components/Select/Select.vue';
import { useTranslation } from '~/composables/useTranslation';
import {
    MENU_KEYS, NEXT_HANDLE, NO_CHOICE_HANDLE, SoundKind,
    type CallFlowDefinition, type CallFlowNode, type CallFlowNodeType, type MenuNode, type PlaybackNode, type QueueNode,
} from '~/types/CallFlow';
import type { Queue } from '~/types/Queue';

const props = defineProps<{
    /** Shown under the start card, describing how the flow is reached. */
    startSubtitle: string;
}>();

const definition = defineModel<CallFlowDefinition>({ required: true });

const { t } = useTranslation();
const { fitView } = useVueFlow();

/** The output of a step the picker will insert a new step after. */
const addingAfter = ref<{ source: string; handle: string }>();
const editingId = ref<string>();

const editing = computed(() => definition.value.nodes.find(x => x.id === editingId.value));
const editingPlayback = computed(() => editing.value?.type === 'playback' ? editing.value as PlaybackNode : undefined);
const editingMenu = computed(() => editing.value?.type === 'menu' ? editing.value as MenuNode : undefined);
const editingQueue = computed(() => editing.value?.type === 'queue' ? editing.value as QueueNode : undefined);

const queues = ref<Queue[]>();

onMounted(async () => {
    const response = await fetch('/api/queues');
    queues.value = response.ok ? await response.json() : [];
});

// Cards are laid out top-down as a tree on every change, so positions never have to be stored
// and the user never has to drag anything.
const NODE_WIDTH = 260;
const NODE_HEIGHT = 62;
const BRANCH_WIDTH = 110;
const BRANCH_HEIGHT = 30;

/** Branches are listed in reading order, unlike the keypad in the menu settings. */
const KEY_ORDER = '1234567890*#';

/** A step with outputs other than NEXT_HANDLE shows each of them as a branch. */
function branches(node: CallFlowNode): string[] {
    const outputs = nodeType(node.type).outputs(node);
    return outputs.some(x => x !== NEXT_HANDLE) ? outputs : [];
}

function branchId(source: string, handle: string) {
    return `${source}/${handle}`;
}

/**
 * What is drawn: every step as a card, plus a pill for each branch of a step. The pills only
 * exist here, the stored edge runs straight from the step to whatever follows the branch.
 */
const view = computed(() => {
    const nodes: { id: string; type: 'action' | 'branch'; width: number; height: number; data: object }[] = [];
    const edges: { id: string; source: string; target: string }[] = [];
    const branchIds = new Set<string>();

    for (const node of definition.value.nodes) {
        const outputs = branches(node);

        nodes.push({
            id: node.id,
            type: 'action',
            width: NODE_WIDTH,
            height: NODE_HEIGHT,
            data: {
                node,
                subtitle: subtitle(node),
                canAddAfter: outputs.length === 0 && nodeType(node.type).outputs(node).length > 0,
            },
        });

        for (const handle of outputs) {
            const id = branchId(node.id, handle);
            branchIds.add(id);
            nodes.push({
                id,
                type: 'branch',
                width: BRANCH_WIDTH,
                height: BRANCH_HEIGHT,
                data: {
                    source: node.id,
                    handle,
                    label: handle === NO_CHOICE_HANDLE ? t('label.no-valid-choice') : t('label.press-key', handle),
                },
            });
            edges.push({ id: `${id}/in`, source: node.id, target: id });
        }
    }

    for (const edge of definition.value.edges) {
        const branch = branchId(edge.source, edge.sourceHandle ?? NEXT_HANDLE);
        edges.push({ id: edge.id, source: branchIds.has(branch) ? branch : edge.source, target: edge.target });
    }

    return { nodes, edges };
});

const flowNodes = computed<Node[]>(() => {
    const positions = layoutTree(view.value.nodes, view.value.edges);

    return view.value.nodes.map(node => ({
        id: node.id,
        type: node.type,
        position: positions.get(node.id) ?? { x: 0, y: 0 },
        data: node.data,
    }));
});

const flowEdges = computed<Edge[]>(() => view.value.edges.map(edge => ({
    id: edge.id,
    source: edge.source,
    target: edge.target,
    type: 'smoothstep',
    style: { stroke: '#94a3b8', strokeWidth: 2 },
})));

function soundSubtitle(node: PlaybackNode | MenuNode): string {
    if (node.kind === SoundKind.Builtin) {
        return node.sound || t('label.no-sound-selected');
    }

    // Uploads are stored under the step id, so the file name means nothing to the user.
    if (!node.sound) return t('label.no-sound-selected');
    if (node.sound.startsWith('data:')) return t('label.sound-not-saved-yet');
    return t('label.uploaded-sound');
}

function queueSubtitle(node: QueueNode): string {
    if (node.queueId === null) return t('label.no-queue-selected');
    if (!queues.value) return '';
    return queues.value.find(x => x.id === node.queueId)?.name ?? t('label.unknown-queue');
}

function subtitle(node: CallFlowNode): string {
    if (node.type === 'start') return props.startSubtitle;
    if (node.type === 'hangup') return t('label.hang-up-description');
    if (node.type === 'queue') return queueSubtitle(node);
    return soundSubtitle(node);
}

/** The edge leaving a step through the given output, if there is one. */
function outgoing(id: string, handle: string) {
    return definition.value.edges.find(x => x.source === id && (x.sourceHandle ?? NEXT_HANDLE) === handle);
}

/** Whether the line continues after a step of this type. */
function canContinue(type: NodeTypeDefinition) {
    return type.outputs(type.create('')).length > 0;
}

/** The steps the picker offers. Inserting a step that ends the call would cut off the steps below. */
const addableTypes = computed(() => {
    const at = addingAfter.value;
    const continues = at && outgoing(at.source, at.handle);
    return nodeTypes.filter(x => x.addable && (!continues || canContinue(x)));
});

function addAfter(id: string, handle = NEXT_HANDLE) {
    addingAfter.value = { source: id, handle };
}

function addNode(type: CallFlowNodeType) {
    const at = addingAfter.value;
    addingAfter.value = undefined;
    if (!at) return;

    const definitionType = nodeType(type);
    const node = definitionType.create(newNodeId());
    const existing = outgoing(at.source, at.handle);
    const edges = definition.value.edges.filter(x => x !== existing);

    // Splice the new step into the line that was already there. A menu carries the rest of the
    // line on its first key.
    edges.push({ id: `${at.source}-${node.id}`, source: at.source, target: node.id, sourceHandle: at.handle });
    const continueFrom = definitionType.outputs(node)[0];
    if (existing && continueFrom) {
        edges.push({ id: `${node.id}-${existing.target}`, source: node.id, target: existing.target, sourceHandle: continueFrom });
    }

    definition.value = { nodes: [...definition.value.nodes, node], edges };

    if (definitionType.configurable) {
        editingId.value = node.id;
    }
}

function deleteNode(id: string) {
    const node = definition.value.nodes.find(x => x.id === id);
    if (!node) return;

    // There is no single line to reconnect, so the branches go with the step.
    if (branches(node).length > 0) {
        removeSubtree(id);
        return;
    }

    const before = definition.value.edges.find(x => x.target === id);
    const after = outgoing(id, NEXT_HANDLE);
    const edges = definition.value.edges.filter(x => x.source !== id && x.target !== id);

    // Close the gap so the steps around the deleted one stay connected.
    if (before && after) {
        edges.push({ id: `${before.source}-${after.target}`, source: before.source, target: after.target, sourceHandle: before.sourceHandle });
    }

    definition.value = {
        nodes: definition.value.nodes.filter(x => x.id !== id),
        edges,
    };
}

/** Removes a step and everything below it. The editor only builds trees, so nothing else leads there. */
function removeSubtree(id: string) {
    const removed = new Set([id]);
    const queue = [id];

    while (queue.length > 0) {
        const current = queue.pop()!;
        for (const edge of definition.value.edges) {
            if (edge.source === current && !removed.has(edge.target)) {
                removed.add(edge.target);
                queue.push(edge.target);
            }
        }
    }

    definition.value = {
        nodes: definition.value.nodes.filter(x => !removed.has(x.id)),
        edges: definition.value.edges.filter(x => !removed.has(x.source) && !removed.has(x.target)),
    };
}

function editNode(id: string) {
    editingId.value = id;
}

function updateEditing(changes: Partial<Omit<MenuNode, 'id' | 'type'> & Omit<QueueNode, 'id' | 'type'>>) {
    definition.value = {
        ...definition.value,
        nodes: definition.value.nodes.map(x => x.id === editingId.value ? { ...x, ...changes } as CallFlowNode : x),
    };
}

/** Turning a key off takes the steps on its branch with it. */
function toggleKey(key: string) {
    const menu = editingMenu.value;
    if (!menu) return;

    if (!menu.options.includes(key)) {
        const options = [...menu.options, key].sort((a, b) => KEY_ORDER.indexOf(a) - KEY_ORDER.indexOf(b));
        updateEditing({ options });
        return;
    }

    const branch = outgoing(menu.id, key);
    if (branch) {
        removeSubtree(branch.target);
    }

    updateEditing({ options: menu.options.filter(x => x !== key) });
}

function closeEditor() {
    // The number boxes hold whatever was typed; settle them once the user is done.
    const menu = editingMenu.value;
    if (menu) {
        const clamp = (value: number, min: number, max: number, fallback: number) =>
            Number.isFinite(value) ? Math.min(Math.max(Math.round(value), min), max) : fallback;

        updateEditing({
            timeout: clamp(menu.timeout, 1, 60, 5),
            attempts: clamp(menu.attempts, 1, 10, 3),
        });
    }

    editingId.value = undefined;
}

provide(flowContextKey, { editNode, deleteNode, addAfter });

watch(flowNodes, async () => {
    await nextTick();
    fitView({ padding: 0.2, maxZoom: 1 });
});
</script>

<template>
    <div class="h-[560px] bg-gray-50 border border-gray-200 rounded overflow-hidden">
        <VueFlow :nodes="flowNodes" :edges="flowEdges" :nodes-draggable="false" :nodes-connectable="false"
            :elements-selectable="false" :zoom-on-double-click="false" :min-zoom="0.3" :max-zoom="1.5"
            :default-viewport="{ x: 0, y: 0, zoom: 1 }" fit-view-on-init>
            <template #node-action="nodeProps">
                <Action :data="nodeProps.data" />
            </template>
            <template #node-branch="nodeProps">
                <Branch :data="nodeProps.data" />
            </template>
            <Background pattern-color="#cbd5e1" :gap="16" />
        </VueFlow>
    </div>

    <!-- Pick which step to insert -->
    <Modal v-if="addingAfter" :title="t('button.add-step')" @close="addingAfter = undefined">
        <div class="space-y-2">
            <button v-for="type in addableTypes" :key="type.type" type="button"
                class="w-full flex items-center gap-3 p-3 border border-gray-200 rounded text-left hover:border-sky-500 hover:bg-sky-50 cursor-pointer"
                @click="addNode(type.type)">
                <div class="size-9 flex-none rounded bg-gray-100 text-slate-600 flex items-center justify-center">
                    <Icon :icon="type.icon" class="size-5" />
                </div>
                <span class="font-semibold text-sm">{{ t(type.labelKey) }}</span>
            </button>
        </div>
    </Modal>

    <!-- Play sound settings -->
    <Modal v-if="editingPlayback" :title="t('label.play-sound')" @close="closeEditor">
        <SoundPicker :kind="editingPlayback.kind" :sound="editingPlayback.sound" @change="updateEditing" />
        <template #footer>
            <Btn design="primary" :label="t('button.confirm')" @click="closeEditor" />
        </template>
    </Modal>

    <!-- Go to queue settings -->
    <Modal v-if="editingQueue" :title="t('label.go-to-queue')" @close="closeEditor">
        <Select :items="(queues ?? []).map(x => x.id)" :model-value="editingQueue.queueId ?? undefined"
            @update:model-value="updateEditing({ queueId: $event ?? null })">
            <template #item="{ value }">
                {{ queues?.find(x => x.id === value)?.name ?? t('label.unknown-queue') }}
            </template>
        </Select>
        <p v-if="queues && queues.length === 0" class="text-xs text-slate-500">
            {{ t('message.no-queues-created') }}
        </p>
        <template #footer>
            <Btn design="primary" :label="t('button.confirm')" @click="closeEditor" />
        </template>
    </Modal>

    <!-- Phone menu settings -->
    <Modal v-if="editingMenu" :title="t('label.menu')" @close="closeEditor">
        <div>
            <div class="font-semibold mb-2">{{ t('label.menu-prompt') }}</div>
            <SoundPicker :kind="editingMenu.kind" :sound="editingMenu.sound" @change="updateEditing" />
        </div>

        <div>
            <div class="font-semibold">{{ t('label.menu-keys') }}</div>
            <p class="text-xs text-slate-500 mb-2">{{ t('label.menu-keys-description') }}</p>
            <div class="grid grid-cols-3 gap-2 w-48">
                <button v-for="key in MENU_KEYS" :key="key" type="button"
                    class="h-10 rounded border font-semibold text-base cursor-pointer disabled:cursor-not-allowed"
                    :class="editingMenu.options.includes(key)
                        ? 'bg-sky-600 border-sky-600 text-white'
                        : 'bg-white border-gray-300 text-slate-600 hover:border-sky-500'"
                    :disabled="editingMenu.options.length === 1 && editingMenu.options.includes(key)"
                    @click="toggleKey(key)">
                    {{ key }}
                </button>
            </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
            <div>
                <div class="font-semibold mb-1">{{ t('label.menu-timeout') }}</div>
                <Textbox type="number" :model-value="editingMenu.timeout" @update:model-value="updateEditing({ timeout: $event })" />
            </div>
            <div>
                <div class="font-semibold mb-1">{{ t('label.menu-attempts') }}</div>
                <Textbox type="number" :model-value="editingMenu.attempts" @update:model-value="updateEditing({ attempts: $event })" />
                <p class="text-xs text-slate-500 mt-1">{{ t('label.menu-attempts-description') }}</p>
            </div>
        </div>

        <template #footer>
            <Btn design="primary" :label="t('button.confirm')" @click="closeEditor" />
        </template>
    </Modal>
</template>
