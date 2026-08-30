<script setup lang="ts">
withDefaults(
  defineProps<{
    label?: string;
    size?: 'sm' | 'md' | 'lg';
  }>(),
  {
    label: 'Loading',
    size: 'md',
  },
);
</script>

<template>
  <span class="app-spinner" :class="`app-spinner--${size}`" role="status" :aria-label="label" />
</template>
