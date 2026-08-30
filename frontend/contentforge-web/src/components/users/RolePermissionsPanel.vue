<script setup lang="ts">
import type { RoleDefinition } from '@/types/users';
import { getUserRoleLabel } from '@/types/users';

defineProps<{
  roles: RoleDefinition[];
  selectedRole?: string | null;
}>();
</script>

<template>
  <div class="role-permissions">
    <article
      v-for="role in roles"
      :key="role.name"
      class="role-permissions__card"
      :class="{ 'role-permissions__card--selected': selectedRole === role.name }"
    >
      <header class="role-permissions__header">
        <h3>{{ getUserRoleLabel(role.name) }}</h3>
        <span>{{ role.permissions.length }} permissions</span>
      </header>
      <ul class="role-permissions__list">
        <li v-for="permission in role.permissions" :key="permission">
          <code>{{ permission }}</code>
        </li>
      </ul>
    </article>
  </div>
</template>
