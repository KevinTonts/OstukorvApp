# Ostukorvi Planeerija

Veebirakendus, mis aitab kasutajatel genereerida nädala toidumenüü ja leida toiduainete ostukorvile kõige soodsama poe, võttes arvesse eelarvet ja koostisosade ühikuhindu. Projekt kasutab ASP.NET Core backendi, Reacti frontendis ja PostgreSQL-i andmete salvestamiseks.

---

## 🛠️ Tehnoloogiad

- Backend: ASP.NET Core (.NET 6+)
- Frontend: React + Bootstrap
- Andmebaas: PostgreSQL
- Kraapimine: Selenium WebDriver (Coop ja Selver)
- Arendusriistad: Vite, VS Code

---

## ⚙️ Paigaldusjuhend

### Backend

**Eeldused:**
- .NET 6 või uuem
- PostgreSQL andmebaas (nt `ostukorv`)

**Seadista ühenduse string `appsettings.json` failis:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ostukorv;Username=postgres;Password=parool"
  }
}
```

### Käivita rakendus:

```bash
dotnet restore
dotnet ef database update
dotnet run
```

### Frontend

### Mine Frontend kausta ja paigalda sõltuvused:

```bash
cd Frontend
npm install
```

### Käivita arendusserver:
```bash
npm run dev
```