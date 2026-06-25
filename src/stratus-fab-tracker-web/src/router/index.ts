import { createRouter, createWebHistory } from 'vue-router';
import DashboardView from '../views/DashboardView.vue';
import SpoolDetailView from '../views/SpoolDetailView.vue';

export const routes = [
  { path: '/', name: 'Dashboard', component: DashboardView },
];

const allRoutes = [
  ...routes,
  { path: '/spools/:id', name: 'SpoolDetail', component: SpoolDetailView },
];

export default createRouter({
  history: createWebHistory(),
  routes: allRoutes,
});
