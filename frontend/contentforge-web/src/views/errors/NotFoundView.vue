<script setup lang="ts">
import AppButton from '@/components/common/AppButton.vue';
import { useRouter } from 'vue-router';

const router = useRouter();
</script>

<template>
  <section class="page-card page-card--center">
    <h2>Page not found</h2>
    <p class="page-card__lead">The requested route does not exist in the admin application.</p>
    <AppButton @click="router.push({ name: 'dashboard' })">Go to dashboard</AppButton>
  </section>
</template>
