import { ref, type Ref } from "vue";

const refMemo = new Map<string, Ref<any>>();

export function useMemo<T>(key: string, factory: () => T): Ref<T> {
    if (refMemo.has(key)) {
        return refMemo.get(key) as Ref<T>;
    }

    const data: Ref<T> = refMemo.set(key, ref(factory()) as Ref<T>).get(key) as Ref<T>;

    return data;
}