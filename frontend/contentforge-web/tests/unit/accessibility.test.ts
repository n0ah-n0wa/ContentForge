import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { createMemoryHistory, createRouter } from 'vue-router';
import AppAlert from '@/components/common/AppAlert.vue';
import ConfirmActionPanel from '@/components/common/ConfirmActionPanel.vue';
import PaginationBar from '@/components/common/PaginationBar.vue';
import LoginView from '@/views/auth/LoginView.vue';
import { expectNoA11yViolations } from '../support/a11y';

vi.mock('@/stores/authStore', async () => {
  const actual = await vi.importActual<typeof import('@/stores/authStore')>('@/stores/authStore');
  return {
    ...actual,
    useAuthStore: () => ({
      isAuthenticating: false,
      login: vi.fn(),
    }),
  };
});

describe('accessibility primitives', () => {
  it('AppAlert exposes an alert role and passes axe', async () => {
    const wrapper = mount(AppAlert, {
      props: {
        kind: 'error',
        title: 'Sign in failed',
        message: 'Invalid email or password.',
      },
      attachTo: document.body,
    });

    expect(wrapper.attributes('role')).toBe('alert');
    await expectNoA11yViolations(wrapper.element);
    wrapper.unmount();
  });

  it('ConfirmActionPanel is a labelled modal dialog', async () => {
    const wrapper = mount(ConfirmActionPanel, {
      props: {
        title: 'Publish',
        message: 'Publish this entry?',
        requireChangeSummary: true,
      },
      attachTo: document.body,
      global: {
        stubs: {
          AppButton: {
            template: '<button type="button"><slot /></button>',
          },
        },
      },
    });

    expect(wrapper.attributes('role')).toBe('dialog');
    expect(wrapper.attributes('aria-modal')).toBe('true');
    expect(wrapper.get('h3').text()).toBe('Publish');
    await expectNoA11yViolations(wrapper.element);
    wrapper.unmount();
  });

  it('PaginationBar is a named navigation landmark', async () => {
    const wrapper = mount(PaginationBar, {
      props: {
        page: 1,
        pageSize: 10,
        totalItems: 25,
        totalPages: 3,
      },
      attachTo: document.body,
      global: {
        stubs: {
          AppButton: {
            template: '<button type="button"><slot /></button>',
          },
        },
      },
    });

    expect(wrapper.element.tagName).toBe('NAV');
    expect(wrapper.attributes('aria-label')).toBe('Pagination');
    await expectNoA11yViolations(wrapper.element);
    wrapper.unmount();
  });

  it('LoginView form is labelled and axe-clean', async () => {
    setActivePinia(createPinia());
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [{ path: '/', component: { template: '<div />' } }],
    });
    await router.push('/');
    await router.isReady();

    const wrapper = mount(LoginView, {
      attachTo: document.body,
      global: {
        plugins: [router],
        stubs: {
          AppButton: {
            template: '<button type="submit"><slot /></button>',
          },
        },
      },
    });

    expect(wrapper.get('label').text()).toContain('Email');
    await expectNoA11yViolations(wrapper.element);
    wrapper.unmount();
  });
});
