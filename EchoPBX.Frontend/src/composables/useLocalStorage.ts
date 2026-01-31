import { ref, watch, type Ref } from "vue";

const refMemo = new Map<string, Ref<any>>();

/**
 * A composable to use localStorage with reactivity. It syncs the value across tabs/windows. Inspired by https://vueuse.org/useLocalStorage/
 * @param key The localStorage key
 * @param defaultValue The default value if the key does not exist
 * @returns A reactive reference to the value in localStorage
 * @example
 * ```
 * const username = useLocalStorage('username', 'Guest');
 * console.log(username.value); // 'Guest' if not set in localStorage
 * username.value = 'JohnDoe'; // Updates localStorage automatically
 * ```
 */
export function useLocalStorage<T>(key: string, defaultValue: T) {
    if (refMemo.has(key)) {
        return refMemo.get(key) as Ref<T>;
    }

    const storedValue = localStorage.getItem(key);
    const data: Ref<T> = refMemo.set(key, ref(storedValue ? JSON.parse(storedValue) : defaultValue) as Ref<T>).get(key) as Ref<T>;

    watch(data, (newValue) => {
        localStorage.setItem(key, JSON.stringify(newValue));
    }, { deep: true });

    document.addEventListener('storage', (event) => {
        if (!(event instanceof StorageEvent)) return;

        if (event.key === key && event.newValue && event.newValue !== JSON.stringify(data.value) && event.storageArea === localStorage) {
            data.value = JSON.parse(event.newValue);
        }
    });

    return data;
}