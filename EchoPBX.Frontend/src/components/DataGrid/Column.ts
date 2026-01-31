export class Column {
    field: string;
    label: string;
    width: number;
    type: 'string' | 'number' | 'boolean' | 'date' | 'image';

    constructor(init: Partial<Column>) {
        if (!init.field) {
            throw new Error('Field is required for Column');
        }

        this.field = init.field;
        this.label = init.label || init.field;
        this.width = init.width || 150;
        this.type = init.type || 'string';
    }
}