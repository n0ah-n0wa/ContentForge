<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue';
import { useNotifications } from '@/composables/useNotifications';

const KIND_LABELS = {
  success: 'Success',
  error: 'Error',
  warning: 'Warning',
  info: 'Info',
} as const;

const { items, dismiss } = useNotifications();
const timers = new Map<string, number>();

function scheduleDismissal(id: string, timeoutMs: number): void {
  const timerId = window.setTimeout(() => dismiss(id), timeoutMs);
  timers.set(id, timerId);
}

watch(
  items,
  (notifications) => {
    for (const notification of notifications) {
      if (!timers.has(notification.id)) {
        scheduleDismissal(notification.id, notification.timeoutMs);
      }
    }
  },
  { deep: true, immediate: true },
);

onMounted(() => {
  for (const notification of items.value) {
    scheduleDismissal(notification.id, notification.timeoutMs);
  }
});

onUnmounted(() => {
  for (const timerId of timers.values()) {
    window.clearTimeout(timerId);
  }

  timers.clear();
});
</script>

<template>
  <div class="notification-container" aria-live="polite" aria-relevant="additions">
    <article
      v-for="notification in items"
      :key="notification.id"
      class="notification"
      :class="`notification--${notification.kind}`"
      :role="notification.kind === 'error' ? 'alert' : 'status'"
      :aria-live="notification.kind === 'error' ? 'assertive' : 'polite'"
    >
      <div class="notification__content">
        <p class="notification__kind">{{ KIND_LABELS[notification.kind] }}</p>
        <strong>{{ notification.title }}</strong>
        <p v-if="notification.message">{{ notification.message }}</p>
      </div>
      <button
        type="button"
        class="notification__close"
        :aria-label="`Dismiss ${notification.title}`"
        @click="dismiss(notification.id)"
      >
        ×
      </button>
    </article>
  </div>
</template>
