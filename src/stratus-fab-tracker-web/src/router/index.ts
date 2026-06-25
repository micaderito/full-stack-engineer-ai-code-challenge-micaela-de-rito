import { createRouter, createWebHistory } from 'vue-router';
import DashboardView from '../views/DashboardView.vue';

export const routes = [
  { path: '/', name: 'Dashboard', component: DashboardView },
];

export default createRouter({
  history: createWebHistory(),
  routes,
});
