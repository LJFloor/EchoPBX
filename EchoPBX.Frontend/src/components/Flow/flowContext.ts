import type { InjectionKey } from 'vue';

/**
 * Provided by FlowCanvas so the individual cards can act on the flow without every callback
 * having to be threaded through Vue Flow's node data.
 */
export interface FlowContext {
    editNode(id: string): void;
    deleteNode(id: string): void;
    /** Opens the step picker for the line leaving this output of a step. */
    addAfter(id: string, handle?: string): void;
}

export const flowContextKey = Symbol('callFlowContext') as InjectionKey<FlowContext>;
