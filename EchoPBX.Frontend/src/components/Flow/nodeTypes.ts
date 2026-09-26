import { NEXT_HANDLE, NO_CHOICE_HANDLE, SoundKind, type CallFlowNode, type CallFlowNodeType } from '~/types/CallFlow';

export interface NodeTypeDefinition {
    type: CallFlowNodeType;

    /** Icon shown on the card. */
    icon: string;

    /** Translation key for the card title. */
    labelKey: string;

    /** Whether the user can add this step from the picker. */
    addable: boolean;

    /** Whether clicking the card opens its settings. */
    configurable: boolean;

    /**
     * The outputs a line can leave from. A step with just NEXT_HANDLE gets a "+" under its card,
     * any other outputs are drawn as labelled branches.
     */
    outputs(node: CallFlowNode): string[];

    /** Builds a new node of this type. */
    create(id: string): CallFlowNode;
}

/**
 * Every step the editor knows about. Adding an action means adding an entry here and a case in
 * CallFlowDialplanBuilder on the server - nothing else in the editor needs to change.
 */
const startNodeType: NodeTypeDefinition = {
    type: 'start',
    icon: 'mdi:phone-incoming',
    labelKey: 'label.call-flow-start',
    addable: false,
    configurable: false,
    outputs: () => [NEXT_HANDLE],
    create: id => ({ id, type: 'start' }),
};

export const nodeTypes: NodeTypeDefinition[] = [
    startNodeType,
    {
        type: 'playback',
        icon: 'mdi:volume-high',
        labelKey: 'label.play-sound',
        addable: true,
        configurable: true,
        outputs: () => [NEXT_HANDLE],
        create: id => ({ id, type: 'playback', kind: SoundKind.Upload, sound: null }),
    },
    {
        type: 'menu',
        icon: 'mdi:dialpad',
        labelKey: 'label.menu',
        addable: true,
        configurable: true,
        outputs: node => node.type === 'menu' ? [...node.options, NO_CHOICE_HANDLE] : [],
        create: id => ({ id, type: 'menu', kind: SoundKind.Upload, sound: null, options: ['1', '2'], timeout: 5, attempts: 3 }),
    },
    {
        type: 'queue',
        icon: 'mdi:account-group',
        labelKey: 'label.go-to-queue',
        addable: true,
        configurable: true,
        outputs: () => [],
        create: id => ({ id, type: 'queue', queueId: null }),
    },
    {
        type: 'hangup',
        icon: 'mdi:phone-hangup',
        labelKey: 'label.hang-up',
        addable: true,
        configurable: false,
        outputs: () => [],
        create: id => ({ id, type: 'hangup' }),
    },
];

export function nodeType(type: CallFlowNodeType): NodeTypeDefinition {
    return nodeTypes.find(x => x.type === type) ?? startNodeType;
}

/** Node ids only have to be unique within one flow. */
export function newNodeId(): string {
    return `n${Math.random().toString(36).slice(2, 10)}`;
}
