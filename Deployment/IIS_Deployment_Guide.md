# IIS Deployment Guide for Smart Microgrid Web API

## Prerequisites
1. **IIS (Internet Information Services)** installed on Server Laptop with ASP.NET Core Hosting Bundle (.NET 8 runtime).
2. **MongoDB Community Server** running locally on port 27017.

## Steps to Deploy

### 1. Publish Backend Project
Open PowerShell in `Backend/SmartMicrogrid.API` and run:
```powershell
dotnet publish -c Release -o C:\inetpub\SmartMicrogridAPI
```

### 2. Configure IIS Web Site
1. Open **IIS Manager** (`inetmgr`).
2. Add a new Web Site named `SmartMicrogridAPI`.
3. Set Physical Path to `C:\inetpub\SmartMicrogridAPI`.
4. Set Binding to Port `5000` (or host name / IP).
5. Ensure Application Pool is set to `.NET CLR Version: No Managed Code`.

### 3. Environment Variables Override
Configure system environment variables or `web.config` for LAN deployment:
```xml
<configuration>
  <system.webServer>
    <aspNetCore processPath="dotnet" arguments=".\SmartMicrogrid.API.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        <environmentVariable name="MongoDB__ConnectionString" value="mongodb://localhost:27017" />
        <environmentVariable name="MongoDB__DatabaseName" value="SmartMicrogridDB" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

### 4. Firewall Rule
Allow inbound connections on TCP Port 5000 in Windows Defender Firewall:
```powershell
New-NetFirewallRule -DisplayName "SmartMicrogrid API Port 5000" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow
```
