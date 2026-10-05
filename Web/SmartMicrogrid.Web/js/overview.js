// Infrastructure metrics and charts for the unified Overview page.
(() => {
    let statusChartInstance = null, capacityChartInstance = null;
    const value = (data, key) => Number(data[key]) || 0;
    const percent = (part, total) => total > 0 ? Math.max(0, Math.min(100, part / total * 100)) : 0;
    const energy = number => new Intl.NumberFormat(undefined, { maximumFractionDigits: 1 }).format(number);
    async function loadDashboard() {
      const button = document.getElementById('refresh-dashboard'); button.disabled = true; button.textContent = 'Refreshing...';
      try {
        const response = await MicrogridApi.getDashboardStats(), d = response.data || {};
        const availablePct = percent(value(d, 'totalAvailableCapacity'), value(d, 'totalCapacity'));
        const batteryPct = value(d, 'averageBatteryPercentage') || percent(value(d, 'currentBatteryLevel'), value(d, 'totalBatteryCapacity'));
        document.getElementById('dash-total-cap').textContent = `${energy(value(d, 'totalCapacity'))} kWh`;
        document.getElementById('dash-avail-cap').innerHTML = `<span class="status-dot"></span> Available: ${energy(value(d, 'totalAvailableCapacity'))} kWh`;
        document.getElementById('dash-battery-lvl').textContent = `${energy(value(d, 'currentBatteryLevel'))} kWh`;
        document.getElementById('dash-battery-pct').textContent = `Capacity: ${energy(value(d, 'totalBatteryCapacity'))} kWh (${batteryPct.toFixed(1)}%)`;
        document.getElementById('dash-total-mg').textContent = value(d, 'totalMicrogrids'); document.getElementById('dash-active-mg').textContent = `Active: ${value(d, 'activeMicrogrids')} | Inactive: ${value(d, 'inactiveMicrogrids')}`;
        document.getElementById('dash-slots-count').textContent = value(d, 'activeEnergySlots'); document.getElementById('dash-avail-energy').textContent = `Available Energy: ${energy(value(d, 'totalAvailableEnergy'))} kWh`;
        renderInsights(d, availablePct, batteryPct); renderCharts(d); document.getElementById('last-updated').textContent = `Updated ${new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
      } catch (err) {
        document.getElementById('last-updated').textContent = 'Could not refresh infrastructure data. Please try again.';
        ApiClient.showToast('Failed to load dashboard statistics', 'error');
      }
      finally { button.disabled = false; button.textContent = 'Refresh data'; }
    }
    function renderInsights(d, availablePct, batteryPct) {
      const activePct = percent(value(d, 'activeMicrogrids'), value(d, 'totalMicrogrids'));
      document.getElementById('readiness-score').textContent = `${Math.round(availablePct * .4 + batteryPct * .35 + activePct * .25)}%`;
      document.getElementById('capacity-headroom').textContent = `${availablePct.toFixed(0)}%`; document.getElementById('capacity-meter').style.width = `${availablePct}%`;
      document.getElementById('capacity-guidance').textContent = availablePct < 20 ? 'Low headroom - review reservations or bring additional generation online.' : `${energy(value(d, 'totalAvailableCapacity'))} kWh is ready to allocate across the network.`;
      document.getElementById('battery-readiness').textContent = `${batteryPct.toFixed(0)}%`; document.getElementById('battery-meter').style.width = `${batteryPct}%`;
      document.getElementById('battery-guidance').textContent = batteryPct < 30 ? 'Storage is below the resilience threshold. Schedule charging before the next demand peak.' : 'Storage is within the preferred operating range for reliable delivery.';
      document.getElementById('slot-energy').textContent = `${energy(value(d, 'totalAvailableEnergy'))} kWh`; document.getElementById('slot-guidance').textContent = `${value(d, 'activeEnergySlots')} active slots are currently ready for market discovery.`;
      const alerts = [];
      if (value(d, 'offlineMicrogrids')) alerts.push(['critical', `${value(d, 'offlineMicrogrids')} microgrid${value(d, 'offlineMicrogrids') === 1 ? '' : 's'} offline`, 'Inspect connectivity and restore service.']);
      if (value(d, 'maintenanceMicrogrids')) alerts.push(['warning', `${value(d, 'maintenanceMicrogrids')} node${value(d, 'maintenanceMicrogrids') === 1 ? '' : 's'} in maintenance`, 'Confirm expected completion before new allocations.']);
      if (batteryPct < 30) alerts.push(['warning', 'Battery resilience is low', `Average battery level is ${batteryPct.toFixed(0)}%.`]); if (availablePct < 20) alerts.push(['warning', 'Limited capacity headroom', `Only ${energy(value(d, 'totalAvailableCapacity'))} kWh is unallocated.`]);
      if (!alerts.length) alerts.push(['healthy', 'All core signals are healthy', 'Capacity, storage, and node availability are within target ranges.']);
      document.getElementById('alert-count').textContent = alerts[0][0] === 'healthy' ? 'Clear' : `${alerts.length} open`;
      document.getElementById('operations-alerts').innerHTML = alerts.map(([level, title, message]) => `<div class="alert-item ${level}"><span class="alert-icon">${level === 'critical' || level === 'warning' ? '!' : '&check;'}</span><div><strong>${title}</strong><p>${message}</p></div></div>`).join('');
    }
    function renderCharts(d) {
      const chartsAvailable = typeof Chart !== 'undefined';
      document.querySelectorAll('.chart-unavailable').forEach(message => { message.hidden = chartsAvailable; });
      document.querySelectorAll('.chart-canvas canvas').forEach(canvas => { canvas.hidden = !chartsAvailable; });
      if (!chartsAvailable) return;
      const common = { animation: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? false : { duration: 300 }, responsive: true, maintainAspectRatio: false, plugins: { legend: { position: 'bottom', labels: { boxWidth: 10, usePointStyle: true } } } };
      if (statusChartInstance) statusChartInstance.destroy(); statusChartInstance = new Chart(document.getElementById('statusChart'), { type: 'doughnut', data: { labels: ['Active', 'Inactive', 'Maintenance', 'Offline'], datasets: [{ data: [value(d, 'activeMicrogrids'), value(d, 'inactiveMicrogrids'), value(d, 'maintenanceMicrogrids'), value(d, 'offlineMicrogrids')], backgroundColor: ['#10b981', '#f59e0b', '#3b82f6', '#ef4444'], borderWidth: 0 }] }, options: common });
      if (capacityChartInstance) capacityChartInstance.destroy(); capacityChartInstance = new Chart(document.getElementById('capacityChart'), { type: 'bar', data: { labels: ['Available', 'Reserved', 'Used', 'Total'], datasets: [{ label: 'Capacity (kWh)', data: [value(d, 'totalAvailableCapacity'), value(d, 'totalReservedCapacity'), value(d, 'totalUsedCapacity'), value(d, 'totalCapacity')], backgroundColor: ['#10b981', '#f59e0b', '#6366f1', '#06b6d4'], borderRadius: 7 }] }, options: { ...common, scales: { y: { beginAtZero: true, grid: { color: 'rgba(148,163,184,.15)' } }, x: { grid: { display: false } } } } });
    }
    document.addEventListener('DOMContentLoaded', () => { loadDashboard(); document.getElementById('refresh-dashboard').addEventListener('click', loadDashboard); });
})();
