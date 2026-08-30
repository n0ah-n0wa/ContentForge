<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router';
import AppButton from '@/components/common/AppButton.vue';

const route = useRoute();
const router = useRouter();

function goToLogin(): void {
  void router.push({
    name: 'login',
    query: route.query,
  });
}
</script>

<template>
  <section class="page-card page-card--center">
    <h2>Sign in required</h2>
    <p class="page-card__lead">
      {{
        route.query.reason === 'expired'
          ? 'Your session expired. Sign in again to continue working.'
          : 'Authentication is required to access this page.'
      }}
    </p>
    <AppButton @click="goToLogin">Go to sign in</AppButton>
  </section>
</template>
