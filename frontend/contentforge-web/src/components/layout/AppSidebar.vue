<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { primaryNavItems } from '@/config/navigation';
import { useAuthStore } from '@/stores/authStore';

const route = useRoute();
const authStore = useAuthStore();

const activeRouteName = computed(() => route.name?.toString() ?? '');

const visibleNavItems = computed(() =>
  primaryNavItems.filter(
    (item) => !item.permissions?.length || authStore.hasPermissions(item.permissions),
  ),
);
</script>

<template>
  <nav class="app-sidebar" aria-label="Primary">
    <div class="app-sidebar__brand">
      <span class="app-sidebar__logo">CF</span>
      <div>
        <strong>ContentForge</strong>
        <p>Administration</p>
      </div>
    </div>

    <ul class="app-sidebar__nav">
      <li v-for="item in visibleNavItems" :key="item.name">
        <RouterLink
          :to="{ name: item.name }"
          class="app-sidebar__link"
          :class="{ 'app-sidebar__link--active': activeRouteName === item.name }"
          :aria-current="activeRouteName === item.name ? 'page' : undefined"
        >
          {{ item.label }}
        </RouterLink>
      </li>
    </ul>
  </nav>
</template>
