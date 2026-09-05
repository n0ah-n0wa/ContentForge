import { nextTick, onMounted, onUnmounted, watch, type Ref } from 'vue';

const FOCUSABLE_SELECTOR = [
  'a[href]',
  'button:not([disabled])',
  'textarea:not([disabled])',
  'input:not([disabled]):not([type="hidden"])',
  'select:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
  '[contenteditable="true"]',
].join(', ');

function listFocusable(container: HTMLElement | null): HTMLElement[] {
  if (!container) {
    return [];
  }

  return [...container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR)].filter(
    (element) =>
      !element.hasAttribute('disabled') && element.getAttribute('aria-hidden') !== 'true',
  );
}

function focusInitial(container: HTMLElement | null): void {
  if (!container) {
    return;
  }

  const preferred =
    container.querySelector<HTMLElement>('[data-autofocus]') ??
    container.querySelector<HTMLElement>(
      'input:not([disabled]), textarea:not([disabled]), select:not([disabled])',
    ) ??
    listFocusable(container)[0] ??
    container;

  preferred.focus();
}

function trapTab(event: KeyboardEvent, container: HTMLElement | null): void {
  if (event.key !== 'Tab' || !container) {
    return;
  }

  const focusable = listFocusable(container);
  if (focusable.length === 0) {
    event.preventDefault();
    container.focus();
    return;
  }

  const first = focusable[0]!;
  const last = focusable[focusable.length - 1]!;
  const active = document.activeElement as HTMLElement | null;

  if (event.shiftKey && (active === first || !container.contains(active))) {
    event.preventDefault();
    last.focus();
    return;
  }

  if (!event.shiftKey && (active === last || !container.contains(active))) {
    event.preventDefault();
    first.focus();
  }
}

/**
 * Focus management for modal dialogs: move focus in on open, trap Tab, Escape to dismiss, restore focus on close.
 */
export function useModalDialog(options: {
  rootRef: Ref<HTMLElement | null>;
  isOpen?: Ref<boolean>;
  onEscape: () => void;
}): void {
  let previouslyFocused: HTMLElement | null = null;

  function onKeydown(event: KeyboardEvent): void {
    if (options.isOpen && !options.isOpen.value) {
      return;
    }

    if (event.key === 'Escape') {
      event.preventDefault();
      event.stopPropagation();
      options.onEscape();
      return;
    }

    trapTab(event, options.rootRef.value);
  }

  async function activate(): Promise<void> {
    previouslyFocused = document.activeElement as HTMLElement | null;
    await nextTick();
    const root = options.rootRef.value;
    if (root && !root.hasAttribute('tabindex')) {
      root.setAttribute('tabindex', '-1');
    }
    focusInitial(root);
    document.addEventListener('keydown', onKeydown, true);
  }

  function deactivate(): void {
    document.removeEventListener('keydown', onKeydown, true);
    if (previouslyFocused && typeof previouslyFocused.focus === 'function') {
      previouslyFocused.focus();
    }
    previouslyFocused = null;
  }

  if (options.isOpen) {
    watch(
      options.isOpen,
      (open) => {
        if (open) {
          void activate();
        } else {
          deactivate();
        }
      },
      { immediate: true },
    );
  } else {
    onMounted(() => {
      void activate();
    });
  }

  onUnmounted(() => {
    deactivate();
  });
}
