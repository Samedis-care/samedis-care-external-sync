# samedis-care-external-sync

SamedisExternalSync is a .NET 8 console application that synchronizes data with the Samedis API.

Current implementation focus:
- Download sync: `tasks`, `requests`, `device_types`, `departments`, `locations`, `device_models`, `inventories`
- Upload sync: `inventories` from CSV (`<to_samedis>/inventories.csv`)
- Task file download: task documents and test protocols

Not implemented yet:
- `tasks_upload`
- `requests_upload`
- `departments_upload`
- `locations_upload`
- dedicated `contacts` / `trainings` sync flows

## Requirements

- .NET SDK 8.0
- Samedis credentials (`auth.client_id`, `auth.client_secret`)
- Samedis tenant (`samedis.tenant_id`)
- Network access to Identity and Samedis API endpoints

## Setup

1. Create config file:

```bash
cp config.yml.example config.yml
```

2. Fill in credentials and tenant values in `config.yml`.
3. Run from repository root (the app expects `config.yml` in current working directory).

## Run

```bash
dotnet run
```

## Build

```bash
dotnet build -c Release
```

```bash
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
```

Windows single EXE:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## Runtime Behavior

- `config.yml` is mandatory.
- `lastrun.txt` is used as incremental sync cursor (`updated_at > lastRun`).
- If `lastrun.txt` is missing or invalid, fallback is `2022-01-01T00:00:00.000<local-offset>`.
- On every run, `<paths.from_samedis>` is deleted and recreated.
- `<paths.to_samedis>` is created if missing and kept (not cleaned).
- Tenant settings are loaded via `/api/{version}/user/tenants/{tenant_id}`.
- If tenant settings cannot be loaded, fallback is `standard` location mode.

## config.yml Reference

Configuration keys are deserialized in snake_case (YAML) to C# classes.

### `auth`

| Key | Type | Required | Default | Description |
|---|---|---|---|---|
| `auth.uri` | string | yes | none | Identity base URL used for login (`/api/v1/samedis.care/oauth/token`). |
| `auth.client_id` | string | yes | none | Login identity (currently sent as `email` form field). |
| `auth.client_secret` | string | yes | none | Login secret (currently sent as `password` form field). |

### `samedis`

| Key | Type | Required | Default | Description |
|---|---|---|---|---|
| `samedis.uri` | string | yes | none | Base URL for all Samedis API requests. |
| `samedis.api_version` | string | yes | none | API version path segment, e.g. `v4`. |
| `samedis.tenant_id` | string | yes | none | Tenant ID used in tenant-scoped resources. |

### `paths`

| Key | Type | Required | Default | Description |
|---|---|---|---|---|
| `paths.from_samedis` | string | no | `data/from_samedis` | Export target folder. Important: folder is wiped at start of each run. |
| `paths.to_samedis` | string | no | `data/to_samedis` | Input folder for uploads. Currently used for `inventories.csv`. |

### `sync`

| Key | Type | Default | Implemented | Description |
|---|---|---|---|---|
| `sync.device_types` | bool | `false` | yes | Downloads device types to `<from_samedis>/devicetypes.csv`. |
| `sync.device_models` | bool | `true` | yes | Downloads device models and manufacturers to `devicemodels.csv` and `devicemanufacturers.csv`. |
| `sync.contacts` | bool | `false` | no | Currently not used in control flow. |
| `sync.departments_download` | bool | `false` | yes | Downloads departments to `departments.csv`. |
| `sync.departments_upload` | bool | `false` | no | Flag exists, but upload flow is not implemented. |
| `sync.locations_download` | bool | `false` | yes | Downloads locations to `locations.csv`. |
| `sync.locations_upload` | bool | `false` | no | Flag exists, but upload flow is not implemented. |
| `sync.inventories_download` | bool | `false` | yes | Downloads inventories to `inventories.csv`. |
| `sync.inventories_upload` | bool | `false` | yes | Uploads inventories from `<to_samedis>/inventories.csv`. |
| `sync.inventories_upload_fallback_by_device_number` | bool | `false` | yes | If ID lookup fails/missing, resolve target inventory by `inventory_number` (`device_number`). |
| `sync.inventories_upload_create_departments_on_the_fly` | bool | `false` | yes | Allows creating missing departments from CSV title. |
| `sync.inventories_upload_create_locations_on_the_fly` | bool | `false` | yes | Allows creating missing locations; in property mode also property/building/floor. |
| `sync.tasks_download` | bool | `false` | yes | Downloads tasks to `tasks.csv`, plus task documents/protocol files into `task_documents/`. |
| `sync.tasks_upload` | bool | `false` | no | Flag exists, but upload flow is not implemented. |
| `sync.task_download_types` | string | `maintenance` | yes | Passed as `filter[issue_type]`. Comma-separated values are supported by API. |
| `sync.task_archive_filter` | bool | `true` | yes | Passed as `filter[archive]=true/false`. |
| `sync.task_download_status` | string | `done` | yes | Passed as `filter[status]`. |
| `sync.requests_download` | bool | `false` | yes | Downloads requests to `requests.csv`. |
| `sync.requests_upload` | bool | `false` | no | Flag exists, but upload flow is not implemented. |
| `sync.trainings` | bool | `false` | no | Currently not used in control flow. |

### `logging`

| Key | Type | Default | Description |
|---|---|---|---|
| `logging.level` | int | `0` | `0=off`, `1=info`, `2=debug`. |
| `logging.mode` | int | `0` | `0=none`, `1=console`, `2=file`, `3=console+file`. |

When logfile mode is active, logs are written to `log/Logfile_<date>.log`.

### `http`

| Key | Type | Default | Description |
|---|---|---|---|
| `http.valid_certificate` | bool | `false` | If `false`, TLS certificate validation is disabled for API/auth and file downloads. |
| `http.proxy` | string | empty | Proxy address. Use a full URI (recommended: `http://host:port`). |
| `http.proxy_username` | string | empty | Optional proxy username. |
| `http.proxy_password` | string | empty | Optional proxy password. |

### Keys present in `config.yml.example` but currently unused

The following keys are currently not read anywhere in code:
- `import_mode`
- `import_file`
- `import_sql.*`

## Inventories Upload CSV

Source file:
- `<paths.to_samedis>/inventories.csv`

Format:
- delimiter is `;`
- header row required
- minimum required column: `inventory_number`

Important lookup columns:
- `id` (preferred target ID for update)
- `inventory_number` (required; also used for fallback lookup)
- `catalog_id` (optional direct model reference)
- `title`, `device_model_title`, `manufacturer`, `responsible_manufacturer` (used for catalog auto-resolution if `catalog_id` is empty)
- `department_id`, `department`
- `location_id`, `location`
- `source_gebaeude`, `source_ebene`, `source_raum` (used in tenant property mode)

Rows with `operation_status` resolved to `retired` are skipped.

## Output Files

Depending on enabled sync flags, these files are generated in `<paths.from_samedis>`:
- `tasks.csv`
- `requests.csv`
- `devicetypes.csv`
- `departments.csv`
- `locations.csv`
- `devicemodels.csv`
- `devicemanufacturers.csv`
- `inventories.csv`
- `task_documents/*` (task documents and protocol files)

Additional files in project root:
- `lastrun.txt`
- `log/...` (if enabled by `logging.mode`)

## Security Notes

- `config.yml` contains secrets. Do not commit real credentials.
- Use `http.valid_certificate: false` only in controlled environments (staging/dev with self-signed certs).
