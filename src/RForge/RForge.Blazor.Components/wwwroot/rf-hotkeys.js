// The browser side of <RfHotKey>: one keydown listener and one stack shared by every hot key on the page.

const isMac = /mac|iphone|ipad|ipod/i.test(navigator.userAgentData?.platform ?? navigator.platform);
const notTextInputs = new Set(['button', 'checkbox', 'color', 'file', 'image', 'radio', 'range', 'reset', 'submit']);

const entries = [];
let nextId = 1;

function chord(ctrl, alt, shift, meta, key) {
    // A symbol already includes Shift: "?" is Shift+/ on a US keyboard and something else elsewhere.
    const shiftMatters = shift && (key.length > 1 || /[\p{L}\p{N}]/u.test(key));
    return [ctrl && 'ctrl', alt && 'alt', shiftMatters && 'shift', meta && 'meta', key].filter(Boolean).join('+');
}

function comboToChord(combo) {
    const parts = combo.split('+');
    const key = parts.pop();
    const mod = parts.includes('mod');
    return chord(parts.includes('ctrl') || (mod && !isMac), parts.includes('alt'), parts.includes('shift'), mod && isMac, key);
}

function keyName(event) {
    if (event.key === ' ') return 'space';
    if (event.key === '+') return 'plus';
    return event.key.toLowerCase();
}

function eventChords(event, key) {
    const chords = [chord(event.ctrlKey, event.altKey, event.shiftKey, event.metaKey, key)];

    // Shift and Alt change what a key types (Shift+1 is "!", Alt+S is "ß" on a Mac), so also try the physical key.
    const physical = /^(?:Key|Digit)(.)$/.exec(event.code);
    if (physical && (event.shiftKey || event.altKey)) {
        chords.push(chord(event.ctrlKey, event.altKey, event.shiftKey, event.metaKey, physical[1].toLowerCase()));
    }

    return chords;
}

function isTyping(event) {
    const target = event.composedPath()[0];
    if (!(target instanceof Element)) return false;
    if (target.isContentEditable || target.tagName === 'TEXTAREA' || target.tagName === 'SELECT') return true;
    return target.tagName === 'INPUT' && !notTextInputs.has(target.type);
}

function typesText(event, key) {
    return !event.ctrlKey && !event.altKey && !event.metaKey && key !== 'escape' && !/^f\d+$/.test(key);
}

function onKeyDown(event) {
    // Browsers fire keydown without a key for autofill. AltGr only ever types a character.
    if (!event.key || event.repeat || event.isComposing || event.getModifierState('AltGraph')) return;

    const key = keyName(event);
    const chords = eventChords(event, key);
    const blockedByTyping = typesText(event, key) && isTyping(event);

    for (let i = entries.length - 1; i >= 0; i--) {
        const entry = entries[i];
        if (!chords.includes(entry.chord) || (blockedByTyping && !entry.allowWhileTyping)) continue;

        event.preventDefault();
        // Handler errors are dealt with in .NET, so a rejection only means the component or circuit is gone.
        entry.reference.invokeMethodAsync('OnKeyPressed').catch(() => { });
        return;
    }
}

export function register(reference, combo, allowWhileTyping) {
    if (entries.length === 0) {
        document.addEventListener('keydown', onKeyDown);
    }

    const id = nextId++;
    entries.push({ id, reference, chord: comboToChord(combo), allowWhileTyping });
    return id;
}

export function update(id, combo, allowWhileTyping) {
    const entry = entries.find(e => e.id === id);
    if (!entry) return;

    entry.chord = comboToChord(combo);
    entry.allowWhileTyping = allowWhileTyping;
}

export function unregister(id) {
    const index = entries.findIndex(e => e.id === id);
    if (index === -1) return;

    entries.splice(index, 1);

    if (entries.length === 0) {
        document.removeEventListener('keydown', onKeyDown);
    }
}
