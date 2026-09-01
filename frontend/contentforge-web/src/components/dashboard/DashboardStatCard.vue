<script setup lang="ts">
defineProps<{
  label: string;
  value: number | string;
  loading?: boolean;
}>();
</script>

<template>
  <article class="dashboard-stat-card">
    <p class="dashboard-stat-card__label">{{ label }}</p>
    <p v-if="loading" class="dashboard-stat-card__value dashboard-stat-card__value--loading">—</p>
    <p v-else class="dashboard-stat-card__value">{{ value }}</p>
  </article>
</template>
