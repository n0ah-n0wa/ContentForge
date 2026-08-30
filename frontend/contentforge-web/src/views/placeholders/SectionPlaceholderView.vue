<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();

const title = computed(() => (typeof route.meta.title === 'string' ? route.meta.title : 'Section'));
</script>

<template>
  <section class="page-card">
    <h2>{{ title }}</h2>
    <p class="page-card__lead">
      This section is part of the ContentForge admin shell. Detailed CMS screens will be added in a
      later phase.
    </p>
    <p class="page-card__hint">Route: {{ route.fullPath }}</p>
  </section>
</template>
