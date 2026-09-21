/**
 * Keep in sync with EchoPBX.Data.Models.SoundKind
 */
export enum SoundKind {
    Upload = 1,
    Builtin = 2,
}

export type CallFlowNodeType = 'start' | 'playback' | 'hangup' | 'menu' | 'queue';

interface CallFlowNodeBase {
    id: string;
    type: CallFlowNodeType;
}

export interface StartNode extends CallFlowNodeBase {
    type: 'start';
}

export interface HangupNode extends CallFlowNodeBase {
    type: 'hangup';
}

export interface PlaybackNode extends CallFlowNodeBase {
    type: 'playback';
    kind: SoundKind;

    /**
     * A data URL for a freshly picked file, a /sounds/... URL for one that is already saved,
     * or the Asterisk sound name when kind is Builtin.
     */
    sound?: string | null;
}

/**
 * Plays a prompt and branches on the key the caller presses. Every key in options is an output
 * of its own, plus NO_CHOICE_HANDLE for a caller who never presses a valid key.
 */
export interface MenuNode extends CallFlowNodeBase {
    type: 'menu';
    kind: SoundKind;

    /** Same as PlaybackNode.sound. */
    sound?: string | null;

    options: string[];

    /** Seconds to wait for a key once the prompt has finished. */
    timeout: number;

    /** How often the prompt is played before the no choice branch is followed. */
    attempts: number;
}

/** Hands the call to a queue, which ends the flow. */
export interface QueueNode extends CallFlowNodeBase {
    type: 'queue';
    queueId: number | null;
}

export type CallFlowNode = StartNode | HangupNode | PlaybackNode | MenuNode | QueueNode;

/** The output of a step that has only one. */
export const NEXT_HANDLE = 'next';

/** Keep in sync with EchoPBX.Data.Models.MenuNode.NoChoiceHandle */
export const NO_CHOICE_HANDLE = 'none';

/** Every key a menu can offer, in the order they appear on a keypad. */
export const MENU_KEYS = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '*', '0', '#'];

export interface CallFlowEdge {
    id: string;
    source: string;
    target: string;

    /**
     * Which output of the source step this line leaves from: NEXT_HANDLE, or for a menu a key or
     * NO_CHOICE_HANDLE.
     */
    sourceHandle?: string | null;
}

export interface CallFlowDefinition {
    nodes: CallFlowNode[];
    edges: CallFlowEdge[];
}

export interface CallFlow {
    id: number;
    slug: string;
    name: string;
    internalNumber?: number | null;
    steps: number;

    /** The trunks that send their incoming calls to this flow. */
    trunks?: string[];

    definition: CallFlowDefinition;
}
