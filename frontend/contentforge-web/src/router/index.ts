import { createRouter, createWebHistory } from 'vue-router';
import AppLayout from '@/layouts/AppLayout.vue';
import AuthLayout from '@/layouts/AuthLayout.vue';
import { ROUTE_PERMISSIONS } from '@/config/navigation';
import { authGuard, permissionGuard } from '@/router/guards';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      component: AuthLayout,
      meta: { guestOnly: true, title: 'Sign in' },
      children: [
        {
          path: '',
          name: 'login',
          component: () => import('@/views/auth/LoginView.vue'),
        },
      ],
    },
    {
      path: '/preview/:token',
      name: 'content-preview',
      component: () => import('@/views/content-entries/ContentPreviewView.vue'),
      meta: { title: 'Preview', publicPreview: true },
    },
    {
      path: '/',
      component: AppLayout,
      meta: { requiresAuth: true },
      children: [
        {
          path: '',
          redirect: { name: 'dashboard' },
        },
        {
          path: 'dashboard',
          name: 'dashboard',
          component: () => import('@/views/DashboardView.vue'),
          meta: { title: 'Dashboard' },
        },
        {
          path: 'content',
          name: 'content',
          component: () => import('@/views/content-entries/ContentIndexView.vue'),
          meta: { title: 'Content', permissions: ['content.read'] },
        },
        {
          path: 'content/:contentTypeSlug',
          name: 'content-by-type',
          component: () => import('@/views/content-entries/ContentEntryListView.vue'),
          meta: { title: 'Content', permissions: ['content.read'] },
        },
        {
          path: 'content/:contentTypeSlug/new',
          name: 'content-entry-create',
          component: () => import('@/views/content-entries/ContentEntryCreateView.vue'),
          meta: { title: 'Create entry', permissions: ['content.create'] },
        },
        {
          path: 'content/:contentTypeSlug/:entryId/view',
          name: 'content-entry-view',
          component: () => import('@/views/content-entries/ContentEntryViewView.vue'),
          meta: { title: 'View entry', permissions: ['content.read'] },
        },
        {
          path: 'content/:contentTypeSlug/:entryId',
          name: 'content-entry-edit',
          component: () => import('@/views/content-entries/ContentEntryEditView.vue'),
          meta: { title: 'Edit entry', permissions: ['content.update'] },
        },
        {
          path: 'content-types',
          name: 'content-types',
          component: () => import('@/views/content-types/ContentTypeListView.vue'),
          meta: { title: 'Content Types', permissions: [...ROUTE_PERMISSIONS.contentTypes] },
        },
        {
          path: 'content-types/new',
          name: 'content-type-create',
          component: () => import('@/views/content-types/ContentTypeCreateView.vue'),
          meta: { title: 'Create content type', permissions: ['contentType.create'] },
        },
        {
          path: 'content-types/:id',
          name: 'content-type-edit',
          component: () => import('@/views/content-types/ContentTypeEditView.vue'),
          meta: { title: 'Edit content type', permissions: ['contentType.update'] },
        },
        {
          path: 'media',
          name: 'media',
          component: () => import('@/views/media/MediaLibraryView.vue'),
          meta: { title: 'Media', permissions: ['media.read'] },
        },
        {
          path: 'users',
          name: 'users',
          component: () => import('@/views/users/UserListView.vue'),
          meta: { title: 'Users', permissions: [...ROUTE_PERMISSIONS.users] },
        },
        {
          path: 'users/new',
          name: 'user-create',
          component: () => import('@/views/users/UserCreateView.vue'),
          meta: { title: 'Create user', permissions: ['user.create'] },
        },
        {
          path: 'users/:id',
          name: 'user-edit',
          component: () => import('@/views/users/UserEditView.vue'),
          meta: { title: 'Manage user', permissions: ['user.update'] },
        },
        {
          path: 'roles',
          name: 'roles',
          component: () => import('@/views/roles/RoleListView.vue'),
          meta: { title: 'Roles', permissions: [...ROUTE_PERMISSIONS.roles] },
        },
        {
          path: 'audit',
          name: 'audit',
          component: () => import('@/views/audit/AuditLogView.vue'),
          meta: { title: 'Audit Log', permissions: [...ROUTE_PERMISSIONS.audit] },
        },
        {
          path: 'settings',
          name: 'settings',
          component: () => import('@/views/placeholders/SectionPlaceholderView.vue'),
          meta: { title: 'Settings' },
        },
        {
          path: 'profile',
          name: 'profile',
          component: () => import('@/views/placeholders/SectionPlaceholderView.vue'),
          meta: { title: 'Profile' },
        },
        {
          path: 'access-denied',
          name: 'access-denied',
          component: () => import('@/views/errors/AccessDeniedView.vue'),
          meta: { title: 'Access denied' },
        },
        {
          path: 'unauthorized',
          name: 'unauthorized',
          component: () => import('@/views/errors/UnauthorizedView.vue'),
          meta: { title: 'Sign in required' },
        },
      ],
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: () => import('@/views/errors/NotFoundView.vue'),
      meta: { title: 'Not found' },
    },
  ],
});

router.beforeEach(authGuard);
router.beforeEach(permissionGuard);

router.afterEach((to) => {
  const pageTitle = typeof to.meta.title === 'string' ? to.meta.title : 'ContentForge';
  document.title = `${pageTitle} · ContentForge`;
});

export default router;
