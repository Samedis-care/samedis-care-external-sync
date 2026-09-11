#!/usr/bin/env python3
"""
Builds a small, self-consistent stand-in for the export the source system drops into
data/to_samedis, so the import can be exercised without the live files (109k inventories,
25k rooms) and without live names.

Everything here is invented. What is kept faithful is the *shape*: the column sets, the
German date and decimal formats, the quoting and BOM/CRLF conventions of each file, and the
fill rates that make the edge paths reachable -- roughly half the inventories carry no
location, `id` and `catalog_id` are always empty, and a few rows are deliberately awkward.

  3 buildings x 5 floors x 3 rooms = 45 rooms
  4 profit centres, 8 departments spread over them
  50 inventories, each on a real room and in a real department
  20 tasks, each belonging to one of the 50 inventories

Every reference resolves inside the set: an inventory names a room that exists in rooms.csv
and a department that exists in departments.csv, and a department names a profit centre that
exists. Nothing points outside, so a failed import means the import is wrong rather than the
data.
"""
import csv, io, pathlib, random

OUT = pathlib.Path(__file__).parent / "to_samedis"
OUT.mkdir(parents=True, exist_ok=True)

rnd = random.Random(20260828)  # fixed seed: the fixtures must not change between runs

# ---------------------------------------------------------------- invented vocabulary
BUILDINGS = [
    ("Haus Ahorn",  "Ahornweg 3",     "12345", "Musterstadt"),
    ("Haus Birke",  "Birkenallee 17", "12345", "Musterstadt"),
    ("Haus Ceder",  "Cederstrasse 8", "12347", "Nachbarort"),
]
FLOORS = ["Untergeschoss", "Erdgeschoss", "1. Obergeschoss", "2. Obergeschoss", "Dachgeschoss"]
ROOM_KINDS = ["Behandlungsraum", "Lagerraum", "Funktionsraum"]

MODELS = [
    ("Vitalmess 300",   "Patientenmonitor",     "Nordlicht Medizintechnik GmbH"),
    ("Infusomat X2",    "Infusionspumpe",       "Talwerk Medical AG"),
    ("Spirolux 12",     "Lungenfunktionsgeraet", "Nordlicht Medizintechnik GmbH"),
    ("Sonocheck 40",    "Ultraschallgeraet",    "Bergquell Diagnostik GmbH"),
    ("Defi Rescue 9",   "Defibrillator",        "Talwerk Medical AG"),
    ("Waegemat 120",    "Personenwaage",        "Muehlbach Geraetebau KG"),
    ("Thermofix 5",     "Waermetherapiegeraet", "Muehlbach Geraetebau KG"),
    ("Ventilo Pro",     "Beatmungsgeraet",      "Bergquell Diagnostik GmbH"),
]
OWNERSHIP = ["Eigen", "Eigen", "Eigen", "Leasing", "Miete", "Leihe", "NN", ""]
STATUS = ["Aktiv", "Aktiv", "Aktiv", "Aktiv", "Eingelagert", "Stillgelegt", "Ausgemustert"]
PROFIT_CENTERS = ["Zentrum Konservativ", "Zentrum Operativ",
                  "Zentrum Diagnostik", "Zentrum Querschnitt"]

# (cost centre, department, profit centre, notes) -- the notes column is optional in the
# import, so half of them carry one and half do not.
DEPARTMENTS = [
    ("41100", "Innere Medizin",   PROFIT_CENTERS[0], "Station 1 bis 3"),
    ("41110", "Kardiologie",      PROFIT_CENTERS[0], ""),
    ("41200", "Chirurgie",        PROFIT_CENTERS[1], "OP-Bereich West"),
    ("41210", "Unfallchirurgie",  PROFIT_CENTERS[1], ""),
    ("41300", "Radiologie",       PROFIT_CENTERS[2], "MRT und CT"),
    ("41310", "Nuklearmedizin",   PROFIT_CENTERS[2], ""),
    ("41400", "Anaesthesie",      PROFIT_CENTERS[3], "inkl. Aufwachraum"),
    ("41500", "Zentrallabor",     PROFIT_CENTERS[3], ""),
]

def de_datetime(d, m, y):  return f"{d:02d}.{m:02d}.{y} 00:00:00"
def de_date(d, m, y):      return f"{d:02d}.{m:02d}.{y}"

def write(name, header, rows, *, quote_all, bom):
    """Writes one file the way the source system writes it: ';' separated, CRLF, and with
    the same quoting and BOM convention as its live counterpart."""
    buf = io.StringIO()
    w = csv.writer(buf, delimiter=";", lineterminator="\r\n",
                   quoting=csv.QUOTE_ALL if quote_all else csv.QUOTE_MINIMAL)
    w.writerow(header)
    w.writerows(rows)
    text = ("﻿" if bom else "") + buf.getvalue()
    (OUT / name).write_text(text, encoding="utf-8", newline="")
    print(f"  {name:<18} {len(rows):>3} rows")

# ---------------------------------------------------------------- locations
LOC_HEADER_B = ["id", "parent_id", "number", "description", "location_type",
                "plis_code", "changed at", "created_at"]
buildings, floors, rooms = [], [], []
next_id = 30000000

def take_id():
    global next_id
    next_id += 137          # not consecutive, like the live export
    return str(next_id)

for b_index, (b_name, street, zip_code, city) in enumerate(BUILDINGS, start=1):
    b_id = take_id()
    buildings.append([b_id, "", f"{2000 + b_index}", b_name, "Gebäude", "",
                      street, zip_code, city,
                      de_datetime(4, 3, 2026), de_datetime(12, 1, 2019)])
    for f_index, f_name in enumerate(FLOORS):
        f_id = take_id()
        floors.append([f_id, b_id, f"{f_index:02d}", f"{b_name} - {f_name}", "Ebene",
                       f"{100 + f_index}", de_datetime(4, 3, 2026), de_datetime(12, 1, 2019)])
        for r_index, kind in enumerate(ROOM_KINDS, start=1):
            r_id = take_id()
            number = f"{b_index}{f_index}{r_index:02d}"
            rooms.append([r_id, f_id, number, f"{kind} {number}", "Raum", "",
                          de_datetime(4, 3, 2026), de_datetime(12, 1, 2019)])

write("buildings.csv",
      ["id", "parent_id", "number", "description", "location_type", "plis_code",
       "street", "postal_code", "city", "changed at", "created_at"],
      buildings, quote_all=False, bom=True)
write("floors.csv", LOC_HEADER_B, floors, quote_all=False, bom=True)
write("rooms.csv", LOC_HEADER_B, rooms, quote_all=False, bom=True)

# ---------------------------------------------------------------- departments
# Read from to_samedis/departments.csv by the preload pass. The title is taken from
# `department`, falling back to `cost_center_description` and then `abteilung`; the profit
# centre from `profit_center`, falling back to `wirtschaftende_einheit`. Both fallbacks are
# exercised below so the column-name handling stays covered.
DEPARTMENT_HEADER = ["id", "department_id", "cost_center_number", "department",
                     "cost_center_description", "abteilung", "notes", "profit_center",
                     "wirtschaftende_einheit"]

departments = []
for index, (cost_center, name, profit_center, notes) in enumerate(DEPARTMENTS):
    source_id = f"D{4000 + index}"
    if index % 4 == 1:
        # title only in cost_center_description
        row = [source_id, "", cost_center, "", name, "", notes, profit_center, ""]
    elif index % 4 == 2:
        # title only in the German column, profit centre in the German column too
        row = [source_id, "", cost_center, "", "", name, notes, "", profit_center]
    else:
        row = [source_id, "", cost_center, name, "", "", notes, profit_center, ""]
    departments.append(row)

write("departments.csv", DEPARTMENT_HEADER, departments, quote_all=False, bom=True)

# ---------------------------------------------------------------- inventories
INV_HEADER = ["id", "external_id", "inventory_number", "serial_number", "catalog_id",
              "device_type_id", "device_model_title", "device_type_title", "manufacturer",
              "responsible_manufacturer", "construction_year", "commissioning_at",
              "purchase_price", "currency_code", "retirement_date", "date_of_acquisition",
              "warranty_period", "ownership", "operation_status", "cost_center_number",
              "cost_center_description", "source_location_number", "source_location_type",
              "source_location_id", "department_station", "description", "software_version",
              "additional_location_info", "changed_at", "created_at",
              # Beyond the live export: the import reads these when they are present.
              "department", "profit_center", "notes", "location"]

inventories = []
# Running counters rather than i-derived indices: a stride into the room list cycles and
# leaves most rooms unreferenced, which is a location assignment nobody tests.
next_room = next_floor = next_building = 0

for i in range(50):
    model, device_type, maker = MODELS[i % len(MODELS)]
    external_id = str(1400000 + i * 7)
    inventory_number = str(320000 + i * 3)
    cost_center, cost_center_name, profit_center, _ = DEPARTMENTS[i % len(DEPARTMENTS)]
    status = STATUS[i % len(STATUS)]

    # Every row carries a location, and all three reference types are exercised: the import
    # branches on source_location_type and resolves a floor or a building to a placeholder
    # room, which only gets covered if those cases appear.
    if i % 10 == 8:
        floor = floors[next_floor % len(floors)]
        next_floor += 1
        source_number, source_type, source_id = floor[2], "Ebene", floor[0]
        room_title = ""
    elif i % 10 == 9:
        building = buildings[next_building % len(buildings)]
        next_building += 1
        source_number, source_type, source_id = building[2], "Gebäude", building[0]
        room_title = ""
    else:
        room = rooms[next_room % len(rooms)]
        next_room += 1
        source_number, source_type, source_id = room[2], "Raum", room[0]
        room_title = room[3]
    station = cost_center_name

    year = 2004 + (i % 18)
    inventories.append([
        "",                                        # id: always empty in the export
        external_id,
        inventory_number,
        f"SN-{9000 + i * 13}",
        "",                                        # catalog_id: always empty
        str(250000 + (i % len(MODELS))),
        model, device_type, maker, maker,
        str(year),
        de_datetime(1 + i % 27, 1 + i % 12, year),
        f"{1200 + i * 37},000000",                 # German decimal comma, six places
        "EUR",
        # In the past on purpose: the server rejects a device_retired issue whose finished
        # date lies ahead ("The finished date is invalid").
        de_datetime(14, 6, min(year + 15, 2025)) if status == "Ausgemustert" else "",
        de_datetime(1 + i % 27, 1 + i % 12, year),
        "24" if i % 3 else "",
        OWNERSHIP[i % len(OWNERSHIP)],
        status,
        cost_center, cost_center_name,
        source_number, source_type, source_id, station,
        f"{device_type} im Bestand",
        f"{1 + i % 4}.{i % 10}",
        "",                                        # additional_location_info: always empty
        de_datetime(2, 6, 2026), de_datetime(9, 11, year),
        cost_center_name,          # department: matches departments.csv
        profit_center,             # profit_center: matches departments.csv
        "" if i % 3 else f"Inventarnotiz {i + 1}",
        room_title,                # location: room title, used as a fallback
    ])

# A few deliberately awkward rows, so the edge paths stay covered:
# padded values, the literal NULL a SQL export writes, and a price in the other convention.
inventories[3][3]  = "  SN-PADDED  "
inventories[7][25] = "NULL"
inventories[11][12] = "1499.50"                    # invariant convention in a German file
# Derselbe Modellname bei einem anderen Hersteller. Zeile 15 traegt sonst Ventilo Pro,
# also muss die Geraeteart mitwandern -- sonst steht ein Patientenmonitor unter
# "Beatmungsgeraet" und der Fall prueft zwei Dinge auf einmal statt einem.
inventories[15][6] = "Vitalmess 300"
inventories[15][7] = "Patientenmonitor"
inventories[15][8] = inventories[15][9] = "Talwerk Medical AG"
inventories[19][11] = ""                           # no commissioning date

write("inventories.csv", INV_HEADER, inventories, quote_all=True, bom=True)

# ---------------------------------------------------------------- tasks
TASK_HEADER = ["issue_number", "inventory_device_number", "issue_type", "title", "date",
               "status", "done_at", "responsible_name", "maintenance_passed",
               "test_comment", "filename"]

TASK_TITLES = ["Regelwartung", "Sicherheitstechnische Kontrolle", "Messtechnische Kontrolle",
               "Wiederholungspruefung", "Herstellerwartung"]
PROVIDERS = ["Pruefdienst Nord GmbH", "Technikpartner Sued AG", "Hauswerkstatt"]

# The server refuses a maintenance task on a retired device: "The inventory is retired. Its
# operation status may only be changed via a recommission task." So the tasks are drawn from
# the inventories that are still in service.
serviceable = [row for row in inventories if row[18] != "Ausgemustert"]
assert len(serviceable) >= 20, len(serviceable)

tasks = []
for i in range(20):
    inv = serviceable[i * 2]                       # every task belongs to a real inventory
    done_year, due_year = 2025, 2026
    passed = "True" if i % 5 else "False"
    tasks.append([
        str(i + 1),
        inv[2],                                    # joins on inventory_number
        "Wartung / Prüfung",
        TASK_TITLES[i % len(TASK_TITLES)],
        de_date(1 + i % 28, 1 + i % 12, due_year),
        "Abgeschlossen",
        de_date(1 + i % 28, 1 + i % 12, done_year),
        PROVIDERS[i % len(PROVIDERS)],
        passed,
        "" if passed == "True" else f"Maengel festgestellt, Nachpruefung vereinbart ({i + 1})",
        # Every row names one: the upload exists to attach the protocol and skips a row
        # that has none, so a task without a file is not a test case, it is a no-op.
        f"protokoll_{i + 1:03d}.pdf",
    ])

write("tasks.csv", TASK_HEADER, tasks, quote_all=False, bom=False)

# The task upload attaches a protocol when `filename` names one. Without the files the rows
# are skipped, so the upload path would never be exercised -- these are placeholders, valid
# enough to be posted and recognised as PDFs.
DOCS = OUT / "task_documents"
DOCS.mkdir(exist_ok=True)
# At least 1 KB: the server rejects anything smaller with "The file size is smaller than
# the minimum size of 1 KB. Please check if the file is broken." The padding sits in a
# comment, so the file stays a structurally valid PDF.
_padding = b"%" + b" Platzhalter fuer den Testimport." * 40 + b"\n"
minimal_pdf = (
    b"%PDF-1.4\n"
    + _padding * 2 +
    b"1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n"
    b"2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n"
    b"3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 200]>>endobj\n"
    b"trailer<</Root 1 0 R>>\n%%EOF\n"
)
assert len(minimal_pdf) > 1024, len(minimal_pdf)

written = 0
for row in tasks:
    name = row[TASK_HEADER.index("filename")]
    if name:
        (DOCS / name).write_bytes(minimal_pdf)
        written += 1
print(f"  task_documents/    {written} PDFs")
# ---------------------------------------------------------------- requests (incidents)
# Unlike everything above, these two files do NOT create anything: requests.csv only PUTs a
# status and a responsible onto requests that already exist, and request-messages.csv POSTs
# messages onto them. A request is raised by a person in the app, never by this sync.
#
# So the incident numbers below are an assumption: raise five requests in the facility first
# and they will be numbered 1..5. Rows whose number does not resolve are skipped with a
# warning, which is the correct behaviour and harmless -- but then this path is not tested.
REQUEST_HEADER = ["id", "incident_number", "inventory_id", "inventory_number",
                  "inventory_device_number", "responsible_email", "status"]

STATUSES = ["new", "pending", "in_progress", "done", "pending"]

requests_rows = []
for i in range(5):
    inv = inventories[i * 4]
    requests_rows.append([
        "",                       # id: empty, so the row is matched by incident_number
        str(i + 1),
        "",                       # inventory_id: empty, matched by the number below
        inv[2],
        inv[2],
        "",                       # responsible_email: needs a supporter on that inventory
        STATUSES[i],
    ])

write("requests.csv", REQUEST_HEADER, requests_rows, quote_all=False, bom=False)

MESSAGE_HEADER = ["id", "incident_id", "incident_number", "content", "filename"]

messages = []
for i in range(5):
    messages.append([
        "",                       # id empty => create this message
        "",                       # incident_id empty => resolved via incident_number
        str(i + 1),
        f"Rueckmeldung aus dem Fremdsystem zu Anfrage {i + 1}.",
        f"protokoll_{i + 1:03d}.pdf" if i % 2 == 0 else "",
    ])

write("request-messages.csv", MESSAGE_HEADER, messages, quote_all=False, bom=False)

print(f"\n  -> {OUT}")
