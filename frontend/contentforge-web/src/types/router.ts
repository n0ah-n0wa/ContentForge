declare module 'vue-router' {
  interface RouteMeta {
    title?: string;
    requiresAuth?: boolean;
    guestOnly?: boolean;
    permissions?: string[];
  }
}
