# samedis-care-external-sync

SamedisExternalSync is a .NET 8 console application that syncs data from the Samedis API and exports it to CSV files. It supports downloading device types, departments, locations, device models, inventories, tasks, and requests, and can download task documents and protocols when enabled. Inventories upload is implemented from `data/to_samedis/inventories.csv`; other upload flows are still not implemented.

> **TODO** Extend this project to also handle read csv files to update data in Samedis.care using proper API calls.

## Requirements

- .NET SDK 8.0
- Samedis API credentials (client id/secret and tenant id)
- Network access to the Samedis API and identity endpoints

## Installation and setup

1. Copy the example config to a working config file:

```bash
cp config.yml.example config.yml
```

2. Edit `config.yml` and fill in your auth and tenant settings.
3. Run the application from the repo root so it can find `config.yml`.

## Configuration (config.yml)

`config.yml` is required and must be located in the working directory when the app starts. The config is loaded with underscored (snake_case) keys.

### auth

- `uri`: Identity service base URL (for example `https://ident.services`).
- `client_id`: Service account email or client id.
- `client_secret`: Service account client secret.

### samedis

- `uri`: Samedis API base URL.
- `api_version`: API version string (for example `v4`).
- `tenant_id`: Tenant identifier.

### sync

Feature flags to enable or disable each sync flow.

- `device_types`: Download device types.
- `device_models`: Download device models (also downloads manufacturers).
- `contacts`: Reserved (not used in the current flow).
- `departments_download`: Download departments.
- `departments_upload`: Upload departments (not implemented).
- `locations_download`: Download locations.
- `locations_upload`: Upload locations (not implemented).
- `inventories_download`: Download inventories.
- `inventories_upload`: Upload inventories from `data/to_samedis/inventories.csv`.
- `inventories_upload_fallback_by_device_number`: If `true`, update matching inventory by `inventory_number` when CSV `id` is missing or not found.
- `inventories_upload_create_departments_on_the_fly`: If `true`, create missing departments by exact title match from CSV column `department`.
- `inventories_upload_create_locations_on_the_fly`: If `true`, create missing locations by exact title match from CSV column `location`.
- `tasks_download`: Download tasks and task documents.
- `tasks_upload`: Upload tasks (not implemented).
- `task_download_types`: Comma-separated list of task types (for example `maintenance`).
- `task_archive_filter`: Boolean filter for archived tasks.
- `task_download_status`: Comma-separated list of task status values (for example `done`).
- `requests_download`: Download requests.
- `requests_upload`: Upload requests (not implemented).
- `trainings`: Reserved (not used in the current flow).

### logging

- `level`: `0` off, `1` on, `2` debug.
- `mode`: `0` none, `1` console, `2` logfile, `3` console and logfile.

### http

- `valid_certificate`: Validate TLS certificates (set `false` for self-signed only if needed).
- `proxy`: Proxy URL (empty for no proxy).
- `proxy_username`: Proxy username (optional).
- `proxy_password`: Proxy password (optional).

### import_mode and import_sql (currently unused)

The example file includes `import_mode`, `import_file`, and `import_sql` blocks. These keys are not read by the current codebase and are safe to ignore or remove unless you extend the project to use them.

## Build

Build a release binary:

```bash
dotnet build -c Release
```

Create a self-contained single-file build (choose your runtime identifier):

```bash
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
```

### Windows (single EXE)

Build a single self-contained EXE for Windows:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

The publish output folder will contain one executable. For xcopy-style deployment, copy only:

- the generated `.exe`
- `config.yml`

## Runtime output

- CSV exports are written to `data/from_samedis/`.
- Task documents and protocols are written to `data/from_samedis/task_documents/`.
- Last run date is tracked in `lastrun.txt`.
- Logs are written based on the `logging` settings.
