# Deployment & Hosting Guide

## Overview
The `Deployment/` folder contains hosting scripts, environment setup guidelines, and IIS configuration templates for deploying the **Smart Microgrid Energy Management & Trading System**.

---

## Index of Deployment Guides

- **[IIS_Deployment_Guide.md](IIS_Deployment_Guide.md)**: Detailed step-by-step guide for hosting the C# Web API on IIS over Windows LAN / Wi-Fi network.

---

## Deployment Quick Commands

### 1. Local Publish Command
```powershell
cd Backend\SmartMicrogrid.API
dotnet publish -c Release -o ./publish
```

### 2. Windows Firewall Rule for LAN / Wi-Fi Access (Run PowerShell as Admin)
```powershell
New-NetFirewallRule -DisplayName "SmartMicrogrid API Port 5000" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow
```

### 3. Restart IIS Services
```powershell
net stop was /y
net start w3svc
```
