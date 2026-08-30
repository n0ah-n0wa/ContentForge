<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import AppButton from '@/components/common/AppButton.vue';
import { useAuthStore } from '@/stores/authStore';
import { getRoleDisplayName } from '@/types/auth';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const pageTitle = computed(() => {
  if (typeof route.meta.title === 'string') {
    return route.meta.title;
  }

  return 'ContentForge';
});

const userLabel = computed(() => {
  if (!authStore.user) {
    return '';
  }

  return `${authStore.user.displayName} (${getRoleDisplayName(authStore.user.role)})`;
});

async function onLogout(): Promise<void> {
  await authStore.logout();
  await router.push({ name: 'login' });
}
</script>

<template>
  <header class="app-header">
    <div>
      <p class="app-header__eyebrow">ContentForge Admin</p>
      <h1 class="app-header__title">{{ pageTitle }}</h1>
    </div>
    <div class="app-header__actions">
      <p class="app-header__user">{{ userLabel }}</p>
      <AppButton variant="secondary" @click="onLogout">Sign out</AppButton>
    </div>
  </header>
</template>
