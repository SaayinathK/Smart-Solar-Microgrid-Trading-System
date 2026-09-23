# IIS Deployment Guide — C# ASP.NET Core API

## Requirements
1. Windows Server / Windows 10/11 with IIS enabled.
2. **ASP.NET Core Hosting Bundle 8.0** installed.
3. Local MongoDB running on `mongodb://localhost:27017`.

---

## Step-by-Step Deployment

1. **Publish API**:
   ```bash
   dotnet publish Backend/SmartMicrogrid.API/SmartMicrogrid.API.csproj -c Release -o C:\inetpub\SmartMicrogridAPI
   ```

2. **Configure IIS Application Pool**:
   - Open IIS Manager (`inetmgr`).
   - Create a new Application Pool: `SmartMicrogridAppPool`.
   - Set **.NET CLR Version** to **No Managed Code**.
   - Managed Pipeline Mode: **Integrated**.

3. **Create IIS Website**:
   - Right-click Sites -> Add Website.
   - Site name: `SmartMicrogridAPI`.
   - Physical path: `C:\inetpub\SmartMicrogridAPI`.
   - Binding: `http`, Port: `5000`, IP: `All Unassigned`.
   - Select Application Pool: `SmartMicrogridAppPool`.

4. **Verify Deployment**:
   Open browser: `http://localhost:5000/swagger`
