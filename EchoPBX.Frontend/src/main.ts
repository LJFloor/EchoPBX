import { createApp } from 'vue'
import './style.css'
import App from './App.vue'
import { createRouter, createWebHistory } from 'vue-router';
import { addCollection } from '@iconify/vue';
import mdi from '@iconify-json/mdi/icons.json';

addCollection(mdi);

const app = createApp(App);
const router = createRouter({
    history: createWebHistory(),
    routes: [
        { path: '/', component: () => import('./pages/index.vue') },

        { path: '/setup', component: () => import('./pages/setup/index.vue') },
        { path: '/setup/license', component: () => import('./pages/setup/license.vue') },
        { path: '/setup/create-admin', component: () => import('./pages/setup/create-admin.vue') },
        { path: '/setup/finish', component: () => import('./pages/setup/finish.vue') },

        { path: '/admin/dashboard', component: () => import('./pages/admin/dashboard.vue') },
        { path: '/admin/extensions', component: () => import('./pages/admin/extensions.vue') },
        { path: '/admin/extensions/:extensionNumber', component: () => import('./pages/admin/extension.vue') },
        { path: '/admin/trunks', component: () => import('./pages/admin/trunks.vue') },
        { path: '/admin/trunks/:trunkId', component: () => import('./pages/admin/trunk.vue') },
        { path: '/admin/queues', component: () => import('./pages/admin/queues.vue') },
        { path: '/admin/queues/:queueId', component: () => import('./pages/admin/queue.vue') },
    ],
});

app.use(router);
app.mount('#app')