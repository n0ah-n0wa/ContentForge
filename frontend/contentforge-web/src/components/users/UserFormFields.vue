<script setup lang="ts">
import { USER_ROLE_OPTIONS } from '@/types/users';

const email = defineModel<string>('email', { default: '' });
const displayName = defineModel<string>('displayName', { default: '' });
const password = defineModel<string>('password', { default: '' });
const role = defineModel<string>('role', { default: 'Viewer' });

defineProps<{
  mode: 'create' | 'edit';
  disabled?: boolean;
}>();
</script>

<template>
  <div class="stack-form">
    <label v-if="mode === 'create'" class="form-field">
      <span>Email</span>
      <input
        v-model="email"
        type="email"
        autocomplete="off"
        required
        :disabled="disabled"
      />
    </label>

    <label class="form-field">
      <span>Display name</span>
      <input v-model="displayName" type="text" required :disabled="disabled" />
    </label>

    <label v-if="mode === 'create'" class="form-field">
      <span>Password</span>
      <input
        v-model="password"
        type="password"
        autocomplete="new-password"
        required
        minlength="12"
        :disabled="disabled"
      />
      <span class="entry-field__hint">Minimum 12 characters with upper, lower, digit, and symbol.</span>
    </label>

    <label class="form-field">
      <span>Role</span>
      <select v-model="role" required :disabled="disabled">
        <option v-for="option in USER_ROLE_OPTIONS" :key="option.value" :value="option.value">
          {{ option.label }}
        </option>
      </select>
    </label>
  </div>
</template>
