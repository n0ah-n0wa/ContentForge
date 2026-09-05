<script setup lang="ts">
withDefaults(
  defineProps<{
    kind?: 'info' | 'success' | 'warning' | 'error';
    title: string;
    message?: string;
  }>(),
  {
    kind: 'info',
  },
);
</script>

<template>
  <div class="app-alert" :class="`app-alert--${kind}`" role="alert">
    <strong>{{ title }}</strong>
    <p v-if="message">{{ message }}</p>
    <div v-if="$slots.actions" class="app-alert__actions">
      <slot name="actions" />
    </div>
  </div>
</template>
