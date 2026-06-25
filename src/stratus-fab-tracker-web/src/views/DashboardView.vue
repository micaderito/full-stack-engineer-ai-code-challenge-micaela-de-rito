<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import { Doughnut, Bar } from 'vue-chartjs';
import {
  Chart as ChartJS, ArcElement, Tooltip, Legend,
  CategoryScale, LinearScale, BarElement,
} from 'chart.js';

ChartJS.register(ArcElement, Tooltip, Legend, CategoryScale, LinearScale, BarElement);

type DashboardDto = {
  wipByStation: Record<string, number>;
  pastDueCount: number;
};

type ThroughputDayDto = { day: string; completed: number };
type ThroughputDto = {
  daily: ThroughputDayDto[];
  completedPerDay: number;
  duePerDay: number;
  keepingUp: boolean;
};

const dashboard = ref<DashboardDto | null>(null);
const throughput = ref<ThroughputDto | null>(null);
const error = ref('');

const STATIONS = ['Detailing', 'Cut', 'Weld', 'QC', 'Shipped', 'Installed'];
const STATION_COLORS = ['#534AB7', '#185FA5', '#D85A30', '#D4537E', '#1D9E75', '#3B6D11'];

const totalWip = computed(() => {
  if (!dashboard.value) return 0;
  return Object.values(dashboard.value.wipByStation).reduce((a, b) => a + b, 0);
});

const donutData = computed(() => ({
  labels: STATIONS,
  datasets: [{
    data: STATIONS.map(s => dashboard.value?.wipByStation[s] ?? 0),
    backgroundColor: STATION_COLORS,
    borderWidth: 2,
    borderColor: 'transparent',
    hoverOffset: 6,
  }]
}));

const donutOptions = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: '65%',
  plugins: {
    legend: {
      position: 'bottom' as const,
      labels: { boxWidth: 10, padding: 12, font: { size: 12 } }
    },
    tooltip: {
      callbacks: {
        label: (ctx: any) => ` ${ctx.label}: ${ctx.raw} spools`
      }
    }
  }
};

const barData = computed(() => ({
  labels: throughput.value?.daily.map(d => d.day) ?? [],
  datasets: [{
    label: 'Completed',
    data: throughput.value?.daily.map(d => d.completed) ?? [],
    backgroundColor: '#1D9E75',
    borderRadius: 4,
    borderSkipped: false,
  }]
}));

const barOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
  },
  scales: {
    x: { grid: { display: false }, ticks: { font: { size: 11 } } },
    y: { beginAtZero: true, ticks: { stepSize: 1, font: { size: 11 } }, grid: { color: 'rgba(0,0,0,0.06)' } }
  }
};

async function load() {
  try {
    const [d, t] = await Promise.all([
      fetch('/api/dashboard').then(r => r.json()),
      fetch('/api/throughput').then(r => r.json()),
    ]);
    dashboard.value = d;
    throughput.value = t;
  } catch {
    error.value = 'Unable to load dashboard data. Make sure the API is running.';
  }
}

onMounted(load);
</script>

<template>
  <div>
    <div v-if="error" class="alert" role="alert">
      <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true" style="flex-shrink:0">
        <circle cx="8" cy="8" r="7" stroke="currentColor" stroke-width="1.4"/>
        <line x1="8" y1="5" x2="8" y2="8.5" stroke="currentColor" stroke-width="1.4" stroke-linecap="round"/>
        <circle cx="8" cy="11" r=".75" fill="currentColor"/>
      </svg>
      {{ error }}
    </div>

    <template v-if="dashboard && throughput">
      <!-- Metric strip -->
      <div class="metrics-grid" role="region" aria-label="Summary metrics">
        <div class="metric">
          <span class="metric__label">TOTAL WIP</span>
          <span class="metric__value">{{ totalWip }}</span>
        </div>
        <div class="metric" :class="{ 'metric--danger': dashboard.pastDueCount > 0 }">
          <span class="metric__label">PAST DUE</span>
          <span class="metric__value">{{ dashboard.pastDueCount }}</span>
        </div>
        <div class="metric">
          <span class="metric__label">COMPLETED / DAY</span>
          <span class="metric__value">{{ throughput.completedPerDay.toFixed(1) }}</span>
        </div>
        <div class="metric" :class="throughput.keepingUp ? 'metric--success' : 'metric--danger'">
          <span class="metric__label">PACE</span>
          <span class="metric__value">{{ throughput.keepingUp ? '✓ On track' : '⚠ Behind' }}</span>
        </div>
      </div>

      <!-- Chart row -->
      <div class="charts-grid">
        <section class="card chart-card" aria-labelledby="wip-heading">
          <h2 id="wip-heading" class="card-title">WIP by Station</h2>
          <div class="donut-wrap">
            <Doughnut :data="donutData" :options="donutOptions" />
            <div class="donut-center" aria-hidden="true">
              <span class="donut-total">{{ totalWip }}</span>
              <span class="donut-sub">active</span>
            </div>
          </div>
        </section>

        <section class="card chart-card" aria-labelledby="throughput-heading">
          <div class="card-header">
            <h2 id="throughput-heading" class="card-title">Daily Throughput</h2>
            <span class="badge" :class="throughput.keepingUp ? 'badge--green' : 'badge--red'">
              Due rate: {{ throughput.duePerDay.toFixed(1) }}/day
            </span>
          </div>
          <div class="bar-wrap">
            <Bar :data="barData" :options="barOptions" />
          </div>
        </section>
      </div>
    </template>

    <p v-else-if="!error" class="loading" aria-live="polite">Loading…</p>
  </div>
</template>

<style scoped>
.alert {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-4);
  border-radius: var(--radius-md);
  font-size: var(--text-sm);
  background: var(--color-danger-bg);
  color: var(--color-danger);
  border: 1px solid var(--color-danger);
  margin-bottom: var(--space-5);
}

.metrics-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--space-3);
  margin-bottom: var(--space-5);
}

.metric {
  background: var(--color-bg-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--space-4) var(--space-5);
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}
.metric__label {
  font-size: var(--text-xs);
  color: var(--color-text-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}
.metric__value {
  font-size: var(--text-2xl);
  font-weight: var(--weight-medium);
  line-height: 1.2;
}
.metric--success .metric__value { color: var(--color-success); }
.metric--danger .metric__value  { color: var(--color-danger); }

.charts-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-5);
}

.chart-card {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.card-header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: var(--space-3);
}

.card-title {
  font-size: var(--text-base);
  font-weight: var(--weight-medium);
  margin: 0;
  color: var(--color-text-primary);
}

.donut-wrap {
  position: relative;
  height: 280px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.donut-center {
  position: absolute;
  display: flex;
  flex-direction: column;
  align-items: center;
  pointer-events: none;
}
.donut-total { font-size: var(--text-2xl); font-weight: var(--weight-medium); }
.donut-sub   { font-size: var(--text-xs); color: var(--color-text-tertiary); }

.bar-wrap { height: 280px; }

.badge {
  font-size: var(--text-xs);
  font-weight: var(--weight-medium);
  padding: 3px 8px;
  border-radius: var(--radius-pill);
}
.badge--green { background: var(--color-success-bg); color: var(--color-success); }
.badge--red   { background: var(--color-danger-bg);  color: var(--color-danger); }

.loading {
  text-align: center;
  padding: var(--space-10) 0;
  font-size: var(--text-sm);
  color: var(--color-text-tertiary);
}
</style>
