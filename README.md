<div align="center">

# GAIL (India) Ltd. QAP & TPI Portal

### Quality Assurance Plan Management System

*A digital workflow for creating, reviewing, and approving Third-Party Inspection Quality Assurance Plans against Purchase Orders — built for GAIL.*

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-Oracle-6DB33F?logo=dotnet&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![Oracle](https://img.shields.io/badge/Database-Oracle-F80000?logo=oracle&logoColor=white)](https://www.oracle.com/database/)
[![SignalR](https://img.shields.io/badge/Realtime-SignalR-000000?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![License](https://img.shields.io/badge/License-MIT-informational)](#license)

</div>

---

## Overview

**QAP Portal** digitizes the Quality Assurance Plan (QAP) lifecycle for purchase orders — replacing scattered emails and manual sign-offs with a single, auditable web workflow. An **Initiator** raises a QAP against PO line items, attaches drawings and documents, and submits it. A designated **Admin/Reviewer** then approves, rejects with remarks, or reopens it — with every action timestamped and logged.

Built as a two-tier .NET solution: a stateless **Web API** backed by Oracle, and an **MVC front end** that consumes it.

---

## Features

- **PO Lookup** — Search SAP purchase orders by number and pull header + line-item details in one call
- **Grouped QAP Creation** — Bundle multiple PO line items into one or more QAP groups in a single request
- **Document Handling** — Upload QAP documents, drawings, technical specifications, and PO copies per group/PO
- **Approval Workflow** — Draft → Submitted → Approved / Rejected → Reopened, with a full action log (who, when, remarks)
- **Role-Based Access** — Separate **Admin** and **Initiator** logins, each backed by their own database-verified credentials
- **Live Notifications** — Admins can broadcast real-time updates to all connected users via SignalR
- **Dashboard** — At-a-glance counts of Draft / Submitted / Approved / Rejected QAPs, filtered per user role
- **Hashed Credentials** — Passwords stored and verified with BCrypt, never in plain text

---

## Architecture

```
┌─────────────────────────┐         ┌──────────────────────────┐
│   QAP_Portal.MVC         │  HTTP   │   QAP.Portal.API          │
│   (ASP.NET Core MVC)     │ ──────▶ │   (ASP.NET Core Web API) │
│   Razor Views + Sessions │         │   EF Core + Oracle        │
└─────────────────────────┘         └──────────────┬───────────┘
                                                     │
                                                     ▼
                                          ┌────────────────────┐
                                          │   Oracle Database   │
                                          │   (QAP_DEV schema)   │
                                          └────────────────────┘
```

The MVC app holds **no direct database connection** — every read/write goes through the API, keeping business logic and data access centralized.

---

## Database Schema

| Table | Purpose |
|---|---|
| `SAP_PO_MASTER` | Purchase order headers synced from SAP |
| `MBA_PO_DETAILS` | PO line-item details (qty, UOM, pricing, vendor) |
| `QAP_LINE_GROUPS` | One row per QAP group, tracks status (`D`/`S`/`A`/`R`) and stores QAP/drawing documents |
| `QAP_GROUP_ITEMS` | Maps PO line items to a QAP group |
| `PO_DOCUMENTS` | Technical specification & PO copy attachments per PO |
| `GROUP_ACTION_LOG` | Full audit trail of every workflow action per group |
| `ADMIN_USERS` | Admin login credentials |
| `QAP_USERS` | Initiator login credentials |

---

## Project Structure

```
QAP_Portal/
├── QAP.Portal.API/              # Web API — Oracle-backed
│   ├── Controllers/              # AdminController, QapUserController, QapCreationController,
│   │                              # QapLineGroupsController, QapGroupItemsController,
│   │                              # PurchaseOrdersController, PoDocumentsController, GroupActionLogController
│   ├── Data/                     # QapDbContext (EF Core)
│   ├── Models/                   # Entity models mapped 1:1 to Oracle tables
│   └── Models/Dtos/               # Request/response DTOs
│
└── QAP_Portal.MVC/               # Front end
    ├── Controllers/               # HomeController, QapController, QapApprovalController
    ├── Services/                  # IQapApiService / QapApiService (typed HttpClient)
    ├── Models/                    # View models + API DTO mirrors
    ├── Hubs/                      # NotificationHub (SignalR)
    └── Views/                     # Razor views (Home, Qap, QapApproval, Shared)
```

---

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Oracle Database (with the schema above provisioned)
- Oracle client / ODP.NET drivers configured for EF Core

### Setup

```bash
# 1. Clone the repo
git clone https://github.com/SRV-KILLER09/QAP-Portal-TPI.git
cd QAP-Portal-TPI/QAP_Portal

# 2. Configure your Oracle connection string
#    QAP.Portal.API/appsettings.json → ConnectionStrings:OracleConnection

# 3. Restore & run the API
cd QAP.Portal.API
dotnet restore
dotnet run

# 4. In a separate terminal, run the MVC app
cd ../QAP_Portal.MVC
dotnet restore
dotnet run
```

Then open the MVC app in your browser and log in as an **Admin** or **Initiator**.

---

## Authentication

| Role | Verified against | Notes |
|---|---|---|
| **Admin** | `ADMIN_USERS` | Full access — approve/reject/reopen QAPs, broadcast notifications |
| **Initiator** | `QAP_USERS` | Create and submit QAPs for their own POs, track status |

Passwords are hashed with **BCrypt** on creation and verified on login — never compared or stored in plain text.

---

## QAP Workflow

```
  Draft (D) ──submit──▶ Submitted (S) ──approve──▶ Approved (A)
                              │
                              └──reject──▶ Rejected (R) ──reopen──▶ Submitted (S)
```

Every transition writes a row to `GROUP_ACTION_LOG` with the acting user, timestamp, and (for rejections) remarks — giving a complete audit trail per QAP group.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend API | ASP.NET Core Web API, Entity Framework Core (Oracle provider) |
| Frontend | ASP.NET Core MVC, Razor Views |
| Database | Oracle |
| Real-time | SignalR |
| Auth | BCrypt.Net password hashing |

---

## Snippets

<img width="949" height="469" alt="image" src="https://github.com/user-attachments/assets/12cca33a-ba0a-4f9b-8916-79ea15f1cb7e" />

---

<img width="949" height="475" alt="image" src="https://github.com/user-attachments/assets/506af4f7-203f-40ba-9030-38f5a38114b5" />

---

<img width="945" height="468" alt="image" src="https://github.com/user-attachments/assets/06b47c3b-d2f2-49ce-99fa-9d3a569ac18f" />

---

<img width="949" height="497" alt="image" src="https://github.com/user-attachments/assets/876a7b6b-9ea5-4089-ac12-92449e00515e" />

---

<img width="951" height="478" alt="image" src="https://github.com/user-attachments/assets/3c46e34f-0744-4cc6-9788-0077b5dcebce" />

---

<img width="950" height="481" alt="image" src="https://github.com/user-attachments/assets/64ce87fc-d686-476e-a27b-dc1f6a4c644b" />

---

<img width="947" height="458" alt="image" src="https://github.com/user-attachments/assets/42f0a557-2fe4-4cdb-8813-5c61107820b5" />

---

<img width="954" height="453" alt="image" src="https://github.com/user-attachments/assets/19b383d3-8cf3-4bc0-8f96-6e9c9a3e7975" />

---

<img width="944" height="456" alt="image" src="https://github.com/user-attachments/assets/57ed2285-f71b-414b-bd02-64ebcd972c4f" />

---

## License

Distributed under the MIT License. See `LICENSE` for details.

---
