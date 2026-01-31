import { ref } from "vue";

const translations: Record<string, any> = {};
const language = ref(navigator.language.split('-')[0] || 'en');

function getNestedValue(obj: Record<string, any>, path: string): string | undefined {
    return path.split('.').reduce((o, p) => (o ? o[p] : undefined), obj)?.toString();
}

export function useTranslation() {
    /**
     * Translate a key to the current language, with optional arguments for placeholders.
     * @param key The key to translate
     * @param args Optional arguments to replace placeholders in the translation
     * @returns The translated string
     * @example
     * ```
     * t('label.save') // "Save" or "Opslaan" depending on the current language
     * t('greeting', 'John') // "Hello, John" if the translation is "Hello, {0}"
     * ```
     */
    function t(key: string, ...args: any[]): string {
        let translation = getNestedValue(translations[language.value] || {}, key);
        if (translation === undefined) {
            translation = getNestedValue(translations['en'] || {}, key) || key;
        }

        return args.length ? translation.replace(/{(\d+)}/g, (match, index) => 
            typeof args[index] !== 'undefined' ? args[index] : match
        ) : translation;
    }

    return { t, language };
}

function load() {
    // glob eager from the `src/lang` folder
    // e.g. `~/lang/en.json`
    const modules = import.meta.glob('~/lang/*.json', { eager: true });
    for (const path in modules) {
        const lang = path.match(/\/lang\/(.*)\.json$/)?.[1];
        if (lang && modules[path] && typeof modules[path] === 'object') {
            translations[lang] = {
                ...translations[lang],
                ...(modules[path] as Record<string, any>)
            };
        }
    }
}

load();