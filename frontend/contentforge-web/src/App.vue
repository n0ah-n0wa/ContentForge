<script setup lang="ts">
import { RouterView } from 'vue-router';
</script>

<template>
  <div class="app-shell">
    <header class="app-header">
      <h1>ContentForge</h1>
      <p>Administrative CMS Dashboard</p>
    </header>
    <main class="app-main">
      <RouterView />
    </main>
  </div>
</template>
