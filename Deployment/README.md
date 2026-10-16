# Deployment & Hosting Guide

## Overview
The `Deployment/` folder contains hosting scripts, environment setup guidelines, and IIS configuration templates for deploying the **Smart Microgrid Energy Management & Trading System**.

---

## Index of Deployment Guides

- **[IIS_Deployment_Guide.md](IIS_Deployment_Guide.md)**: Detailed step-by-step guide for hosting the C# Web API on IIS over Windows LAN / Wi-Fi network.

---

## Deployment Quick Commands

### Insert demonstration data for all system models

From the repository root, run:

```powershell
dotnet run --project Backend/SmartMicrogrid.API --no-launch-profile -- --environment Development --seed
```

This one-shot command uses the API's configured MongoDB database and inserts missing users, microgrids, energy slots, reservations, transactions, system configuration, and activity records. Prices are in LKR and new delivery windows are relative to the execution time. It preserves existing records, passwords, configuration, and workflow changes. Repeating it does not duplicate the same demo dataset or refresh its delivery dates. It prints insertion counts and verifies every demo record before exiting.

The separate `--reseed` option drops the configured MongoDB database, including any legacy or unregistered collections, before rebuilding the complete demonstration dataset; use it only when an intentional database reset is required.

### 1. Local Publish Command
```powershell
cd Backend\SmartMicrogrid.API
dotnet publish -c Release -o ./publish
```

### 2. Windows Firewall Rule for LAN / Wi-Fi Access (Run PowerShell as Admin)
```powershell
New-NetFirewallRule -DisplayName "SmartMicrogrid API Port 5050" -Direction Inbound -LocalPort 5050 -Protocol TCP -Action Allow
```

### 3. Restart IIS Services
```powershell
net stop was /y
net start w3svc
```
