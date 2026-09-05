<script setup lang="ts">
import { onMounted, ref } from 'vue';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import RolePermissionsPanel from '@/components/users/RolePermissionsPanel.vue';
import { listRoles } from '@/api/roles';
import { useUserPermissions } from '@/composables/useUserPermissions';
import type { RoleDefinition } from '@/types/users';

const { canRead } = useUserPermissions();

const roles = ref<RoleDefinition[]>([]);
const loading = ref(true);
const errorMessage = ref<string | null>(null);

async function loadRoles(): Promise<void> {
  if (!canRead.value) {
    loading.value = false;
    return;
  }

  loading.value = true;
  errorMessage.value = null;

  try {
    roles.value = await listRoles();
  } catch {
    errorMessage.value = 'Unable to load roles and permissions.';
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  void loadRoles();
});
</script>

<template>
  <section class="page-card">
    <header class="page-header">
      <div>
        <h2>Roles &amp; permissions</h2>
        <p class="page-card__lead">
          Inspect the permission sets assigned to each CMS role. Role assignment is managed on user
          accounts.
        </p>
      </div>
    </header>

    <AppAlert
      v-if="!canRead"
      kind="warning"
      title="Access restricted"
      message="You do not have permission to inspect roles."
    />

    <AppAlert v-if="errorMessage" kind="error" title="Unable to load" :message="errorMessage">
      <template #actions>
        <AppButton variant="secondary" type="button" @click="loadRoles">Retry</AppButton>
      </template>
    </AppAlert>

    <div v-if="loading" class="inline-loading">
      <AppSpinner label="Loading roles" />
      <span>Loading roles…</span>
    </div>

    <RolePermissionsPanel v-else-if="roles.length" :roles="roles" />

    <div v-else class="empty-state">No roles are configured.</div>
  </section>
</template>
