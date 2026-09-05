<script setup lang="ts">
import AppButton from '@/components/common/AppButton.vue';

const props = defineProps<{
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  disabled?: boolean;
}>();

const emit = defineEmits<{
  'update:page': [page: number];
  'update:pageSize': [pageSize: number];
}>();

function goToPage(nextPage: number): void {
  if (nextPage < 1 || nextPage > props.totalPages || nextPage === props.page) {
    return;
  }

  emit('update:page', nextPage);
}

function onPageSizeChange(event: Event): void {
  const value = Number((event.target as HTMLSelectElement).value);
  emit('update:pageSize', value);
  emit('update:page', 1);
}
</script>

<template>
  <nav class="pagination-bar" aria-label="Pagination">
    <p class="pagination-bar__summary" aria-live="polite">
      Showing page {{ page }} of {{ totalPages }} ({{ totalItems }} items)
    </p>
    <div class="pagination-bar__controls">
      <label class="form-field pagination-bar__size">
        <span>Page size</span>
        <select :value="pageSize" :disabled="disabled" @change="onPageSizeChange">
          <option :value="10">10</option>
          <option :value="25">25</option>
          <option :value="50">50</option>
        </select>
      </label>
      <AppButton
        variant="secondary"
        type="button"
        :disabled="disabled || page <= 1"
        @click="goToPage(page - 1)"
      >
        Previous
      </AppButton>
      <AppButton
        variant="secondary"
        type="button"
        :disabled="disabled || page >= totalPages"
        @click="goToPage(page + 1)"
      >
        Next
      </AppButton>
    </div>
  </nav>
</template>
