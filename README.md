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
- If `sync.archive_to_samedis_csv_files` is `true` (default), CSV files in `<paths.to_samedis>` are moved after run to `<parent>/archive/<folder>` with timestamp suffix (`<name>_yyyyMMdd_HHmmss.csv`).
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
| `sync.inventories_upload_fallback_by_device_number` | bool | `false` | yes | If `id` and `external_id` lookup fail/missing, resolve target inventory by `inventory_number` (`device_number`). |
| `sync.create_local_device_models_on_inventory_lookup` | bool | `false` | yes | If `catalog_id` is missing and model lookup fails, resolves/creates device type + manufacturer contact and creates a tenant-local device model for inventory import. |
| `sync.inventories_upload_create_departments_on_the_fly` | bool | `false` | yes | Allows creating missing departments from CSV title. |
| `sync.inventories_upload_create_locations_on_the_fly` | bool | `false` | yes | Standard mode only: allows creating missing locations from the `location` column. Ignored for row-level assignment in property mode. |
| `sync.archive_to_samedis_csv_files` | bool | `true` | yes | Archives CSVs from `<paths.to_samedis>` to `<parent>/archive/<folder>` with timestamped filenames to avoid reprocessing. |
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
- `external_id` (fallback target reference for update when `id` is empty/not found)
- `inventory_number` (required; fallback lookup after `id` and `external_id`)
- `catalog_id` (optional direct model reference)
- `title`, `device_model_title`, `manufacturer`, `responsible_manufacturer` (used for catalog auto-resolution if `catalog_id` is empty)
- `device_type_title` (required when `sync.create_local_device_models_on_inventory_lookup: true` and catalog/model lookup fails)
- `device_model_is_placeholder` (optional bool; when `true`, importer skips catalog/device-model lookup and local type/manufacturer/model creation)
- `placeholder_device_model_manufacturer`, `placeholder_device_model_title`, `placeholder_device_type_title` (used for placeholder rows; if empty and `device_model_is_placeholder=true`, importer auto-fills from `manufacturer`/`responsible_manufacturer`, `device_model_title`/`title`, `device_type_title`)
- `department_id`, `department`, `cost_center_number`, `cost_center_description`, `Abteilung`
- `location_id`, `location`, `source_location_id`, `source_location_type`, `source_location_number`
- `take_authority` (optional JSON object for record/field protection with keys `drop`, `locked`, `protected_fields`)
- `take_authority_drop`, `take_authority_locked`, `take_authority_protected_fields` (optional column-based alternative to `take_authority`; overrides JSON when provided)
- `buildings.csv`, `floors.csv`, `rooms.csv` (or legacy `StandorteGeba*`, `StandorteEbe*`, `StandorteRau*`) in `<paths.to_samedis>` are used for property-mode hierarchy sync
- In property mode, `source_location_id` is resolved against API `external_id` for rooms/floors/buildings when CSV mapping is missing (using `/via/external_id/{id}` lookups).
- During hierarchy import, created buildings/floors/rooms are written with `external_id` from source CSV (`lid`, fallback `id`).
- During building creation in hierarchy import, `buildings.csv` columns `street`, `postal_code`, `city` are mapped to API fields `street`, `zip`, `town`.
- During hierarchy sync reruns, buildings/floors/rooms are matched by `external_id` first (fallback: title + parent scope) and existing records are updated via `PUT` (title/external_id and hierarchy fields).
- In tenant property mode, hierarchy sync runs before inventory import: first all buildings, then floors (with building parent), then rooms (with floor parent)
- For floor/building source references, importer resolves/creates a room placeholder under the hierarchy using `sync.locations_room_placeholder` (default: `Keine Raumzuordnung`)
- In tenant property mode, if `source_location_id` is missing/unresolvable, inventory rows continue without location reference

Location assignment modes:
- Standard mode (`location` column): importer resolves `location_id` / `location`; if `sync.inventories_upload_create_locations_on_the_fly=true`, missing locations are created on the fly.
- Property mode (`source_location_id` + hierarchy CSV): importer first resolves/creates hierarchy from `buildings.csv`, `floors.csv`, `rooms.csv`. During inventory row processing, locations are only resolved against this hierarchy; no on-the-fly creation is done per row. If not resolvable, inventory is uploaded without `device_location_id`.

`take_authority` mapping details:
- Supported keys are exactly `drop` (bool), `locked` (bool), `protected_fields` (string array).
- CSV `take_authority` should be valid JSON (example: `{"drop":false,"locked":true,"protected_fields":["serial_number","device_location_id"]}`).
- As an alternative, set `take_authority_drop` / `take_authority_locked` (`true|false|1|0|yes|no`) and `take_authority_protected_fields` (comma/pipe separated or JSON array).

Rows with `operation_status` resolved to `retired` are skipped only if the inventory already exists; otherwise they are created as retired devices.

Create defaults:
- For create operations, `do_maintenance` defaults to `true` when CSV value is empty.
- For create operations, `no_medical_device` defaults to `false` when CSV value is empty.

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

Inventory download note:
- In tenant property mode (`use_extended_device_locations=true`), `inventories.csv` includes `source_location_id`.
- Export value mapping is: room (`device_location`) `external_id`; if empty, fallback to parent floor `external_id`.

Additional files in project root:
- `lastrun.txt`
- `log/...` (if enabled by `logging.mode`)

## Security Notes

- `config.yml` contains secrets. Do not commit real credentials.
- Use `http.valid_certificate: false` only in controlled environments (staging/dev with self-signed certs).
