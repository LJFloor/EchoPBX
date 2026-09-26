import { onMounted, onUnmounted } from 'vue';

/**
 * Escape handlers, newest last. Only the newest one runs, so Escape closes an open modal before it
 * leaves the page underneath.
 */
const handlers: (() => void)[] = [];

document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        handlers[handlers.length - 1]?.();
    }
});

/**
 * Run a handler when Escape is pressed, until the returned function is called.
 */
export function pushEscapeHandler(handler: () => void): () => void {
    handlers.push(handler);
    return () => {
        const index = handlers.lastIndexOf(handler);
        if (index !== -1) {
            handlers.splice(index, 1);
        }
    };
}

/**
 * Run a handler when Escape is pressed while the calling component is mounted.
 */
export function useEscape(handler: () => void) {
    let remove: (() => void) | undefined;
    onMounted(() => remove = pushEscapeHandler(handler));
    onUnmounted(() => remove?.());
}
