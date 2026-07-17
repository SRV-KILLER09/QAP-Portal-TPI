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

<img width="948" height="477" alt="image" src="https://github.com/user-attachments/assets/ead0c6f5-44e4-447d-829f-05a677976de6" />

---

<img width="949" height="483" alt="image" src="https://github.com/user-attachments/assets/bb343b15-7672-4e10-9c93-cd3a9a8d84c8" />

---

<img width="950" height="491" alt="image" src="https://github.com/user-attachments/assets/a102cd19-44bc-4ccd-9d82-cd3c94fa20e3" />

---

<img width="1600" height="828" alt="image" src="https://github.com/user-attachments/assets/277ae13b-55da-47c0-af2c-8fd5d66d0d22" />

---

<img width="1600" height="832" alt="image" src="https://github.com/user-attachments/assets/dcd6a57e-5d10-41fc-a171-313297bd137f" />

---

<img width="949" height="488" alt="image" src="https://github.com/user-attachments/assets/ef98f825-aab7-4301-ab52-c8ae0d8af805" />

---

<img width="948" height="453" alt="image" src="https://github.com/user-attachments/assets/0df23877-a2c5-4bf1-b19f-1c38a6a54bd5" />

---

<img width="802" height="238" alt="image" src="https://github.com/user-attachments/assets/8e084f9a-aace-4e01-a297-381df3c437e8" />

---

<img width="951" height="494" alt="image" src="https://github.com/user-attachments/assets/56e550f6-db0f-4d58-a9fa-91bebec377ef" />

---

<img width="955" height="497" alt="image" src="https://github.com/user-attachments/assets/d999f878-d54b-4157-9d99-e131321193dd" />

---

<img width="949" height="481" alt="image" src="https://github.com/user-attachments/assets/de25042b-5a95-481f-a344-1487c7b22cf7" />

---

<img width="954" height="489" alt="image" src="https://github.com/user-attachments/assets/29aa0db6-04c8-40c4-9d42-194bd88bd03a" />

---

<img width="950" height="431" alt="image" src="https://github.com/user-attachments/assets/3b6e0833-a5b0-4565-8454-6019c26fe76b" />

---

<img width="948" height="359" alt="image" src="https://github.com/user-attachments/assets/0a32fbfd-46d5-4ded-8d46-6746c60b22da" />

---

<img width="950" height="485" alt="image" src="https://github.com/user-attachments/assets/8fa20be9-a003-418f-8934-fae5ea4097c2" />

---

<img width="950" height="482" alt="image" src="https://github.com/user-attachments/assets/a6758a01-af37-4187-82b8-c41c66111b28" />

---

<img width="950" height="485" alt="image" src="https://github.com/user-attachments/assets/f0672325-bb80-45cd-9cd2-8183a6886a32" />

---

<img width="952" height="383" alt="image" src="https://github.com/user-attachments/assets/bd76f054-c4d3-46c6-9b0f-e8c8125fb22a" />

---

<img width="959" height="323" alt="image" src="https://github.com/user-attachments/assets/ca4413fe-88f2-4f1a-a0e0-6d790887951c" />

---

<img width="948" height="487" alt="image" src="https://github.com/user-attachments/assets/5ee2c971-4eaa-4d34-a4d5-276b63d904d4" />

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

<img width="948" height="455" alt="image" src="https://github.com/user-attachments/assets/5ef04acc-0924-4e25-9121-4fce8e105c20" />

---

<img width="949" height="476" alt="image" src="https://github.com/user-attachments/assets/e2bacb5d-19e3-4e5b-ae7e-872304ee4319" />

---

## License

Distributed under the MIT License. See `LICENSE` for details.

---
