<script setup lang="ts">
withDefaults(
  defineProps<{
    variant?: 'primary' | 'secondary' | 'ghost';
    type?: 'button' | 'submit' | 'reset';
    disabled?: boolean;
    loading?: boolean;
  }>(),
  {
    variant: 'primary',
    type: 'button',
    disabled: false,
    loading: false,
  },
);
</script>

<template>
  <button
    class="app-button"
    :class="[`app-button--${variant}`, { 'app-button--loading': loading }]"
    :type="type"
    :disabled="disabled || loading"
    :aria-busy="loading"
  >
    <span v-if="loading" class="app-button__spinner" aria-hidden="true" />
    <span class="app-button__label">
      <slot />
    </span>
  </button>
</template>
