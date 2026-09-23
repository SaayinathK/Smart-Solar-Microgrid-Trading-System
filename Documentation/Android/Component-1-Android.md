# Component 1 — Native Android Application Documentation

## Technology Stack
- Native Kotlin & XML Layouts
- Android Jetpack Architecture Components (MVVM)
- Room (SQLite) Local Client Storage & Offline Cache
- Retrofit 2 & Gson for REST API communication
- Kotlin Coroutines for async network & database operations

## Application Architecture
```text
Activities / XML Layouts  <──>  ViewModels  <──>  Repositories  <──┬──>  Retrofit (API)
                                                                 └──>  Room SQLite (Cache)
```

## Screen Overview
1. **MainActivity**: Navigation hub.
2. **MicrogridListActivity**: List of microgrid nodes with status badges and capacity info.
3. **MicrogridDetailsActivity**: Microgrid specifications, capacity breakdown, and battery levels.
4. **EnergyAvailabilityActivity**: Available energy slots browser.
5. **EnergySlotActivity**: Energy slots management list.
