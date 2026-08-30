<script setup lang="ts">
import { computed } from 'vue';
import { useAuthStore } from '@/stores/authStore';
import { getRoleDisplayName } from '@/types/auth';

const authStore = useAuthStore();

const welcomeName = computed(() => authStore.user?.displayName ?? 'Administrator');
const roleLabel = computed(() => (authStore.user ? getRoleDisplayName(authStore.user.role) : ''));
</script>

<template>
  <section class="page-card">
    <h2>Welcome, {{ welcomeName }}</h2>
    <p class="page-card__lead">
      The administrative foundation is ready. CMS workflows will be implemented in upcoming phases.
    </p>

    <dl class="summary-list">
      <div>
        <dt>Signed in as</dt>
        <dd>{{ authStore.user?.email }}</dd>
      </div>
      <div>
        <dt>Role</dt>
        <dd>{{ roleLabel }}</dd>
      </div>
    </dl>
  </section>
</template>
