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

/** Keep parent nav items highlighted on nested create/edit/view routes. */
function isNavItemActive(itemName: string): boolean {
  const name = activeRouteName.value;
  if (!name) {
    return false;
  }

  if (name === itemName) {
    return true;
  }

  switch (itemName) {
    case 'content':
      return name === 'content-by-type' || name.startsWith('content-entry-');
    case 'content-types':
      return name.startsWith('content-type-');
    case 'users':
      return name.startsWith('user-');
    default:
      return false;
  }
}
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
          :class="{ 'app-sidebar__link--active': isNavItemActive(item.name) }"
          :aria-current="isNavItemActive(item.name) ? 'page' : undefined"
        >
          {{ item.label }}
        </RouterLink>
      </li>
    </ul>
  </nav>
</template>
