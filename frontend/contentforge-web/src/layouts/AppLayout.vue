<script setup lang="ts">
import AppHeader from '@/components/layout/AppHeader.vue';
import AppSidebar from '@/components/layout/AppSidebar.vue';
import AppLoadingOverlay from '@/components/common/AppLoadingOverlay.vue';
</script>

<template>
  <div class="app-layout">
    <a href="#main-content" class="skip-link">Skip to main content</a>
    <AppSidebar />
    <div class="app-layout__main">
      <AppHeader />
      <main id="main-content" class="app-layout__content">
        <RouterView />
      </main>
    </div>
    <AppLoadingOverlay />
  </div>
</template>
