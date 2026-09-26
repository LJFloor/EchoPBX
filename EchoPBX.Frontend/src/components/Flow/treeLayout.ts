export interface LayoutNode {
    id: string;
    width: number;
    height: number;
}

const RANK_GAP = 80;
const SIBLING_GAP = 40;

/**
 * Positions the flow top-down as a tree, keeping the children of a step in the order their
 * edges are listed, so the branches of a menu read 1, 2, 3 from left to right. A general graph
 * layout would reorder siblings freely, since any order is as good as another to it.
 *
 * The editor only builds trees. Anything else still gets a place: a step reached twice is drawn
 * under the first step that leads to it, and a step nothing leads to becomes a tree of its own.
 *
 * @returns The top-left corner of every node.
 */
export function layoutTree(nodes: LayoutNode[], edges: { source: string; target: string }[]) {
    const byId = new Map(nodes.map(x => [x.id, x]));
    const outgoing = new Map<string, string[]>();
    for (const edge of edges) {
        outgoing.set(edge.source, [...(outgoing.get(edge.source) ?? []), edge.target]);
    }

    // Decide which node each node hangs under, so the rest can treat the graph as a tree.
    const children = new Map<string, string[]>();
    const claimed = new Set<string>();
    const roots: string[] = [];

    function claim(id: string) {
        const own = (outgoing.get(id) ?? []).filter(x => byId.has(x) && !claimed.has(x));
        own.forEach(x => claimed.add(x));
        children.set(id, own);
        own.forEach(claim);
    }

    function addRoot(id: string) {
        claimed.add(id);
        roots.push(id);
        claim(id);
    }

    const targets = new Set(edges.map(x => x.target));
    nodes.filter(x => !targets.has(x.id)).forEach(x => addRoot(x.id));

    // Whatever is left can only be reached through a loop.
    nodes.filter(x => !claimed.has(x.id)).forEach(x => addRoot(x.id));

    // Every subtree is as wide as its widest level, so neighbouring subtrees never overlap.
    const widths = new Map<string, number>();
    const childrenWidth = (id: string) => {
        const kids = children.get(id)!;
        return kids.reduce((sum, x) => sum + widths.get(x)!, 0) + SIBLING_GAP * Math.max(kids.length - 1, 0);
    };

    function measure(id: string) {
        children.get(id)!.forEach(measure);
        widths.set(id, Math.max(byId.get(id)!.width, childrenWidth(id)));
    }

    const positions = new Map<string, { x: number; y: number }>();

    function place(id: string, left: number, top: number) {
        const node = byId.get(id)!;
        const width = widths.get(id)!;
        positions.set(id, { x: left + (width - node.width) / 2, y: top });

        let x = left + (width - childrenWidth(id)) / 2;
        for (const kid of children.get(id)!) {
            place(kid, x, top + node.height + RANK_GAP);
            x += widths.get(kid)! + SIBLING_GAP;
        }
    }

    let left = 0;
    for (const root of roots) {
        measure(root);
        place(root, left, 0);
        left += widths.get(root)! + SIBLING_GAP;
    }

    return positions;
}
