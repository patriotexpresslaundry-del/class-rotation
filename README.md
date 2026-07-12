# Class Rotation Scheduler

A Windows desktop application (VB.NET / WinForms, .NET 8) for planning training classes
across the year. It schedules multiple classes with start/end dates, imports class rosters,
flags each student **Green** (prerequisites met) or **Red** (prerequisites not met) based on a
configurable prerequisite list, checks whether instructors are free to host a class given their
leave, and shows a visual timeline that separates **classroom Academics** from **On-the-Job
Training (OJT)**.

## Features

- **Yearly timeline** – Gantt-style view of every class across the year, colour-coded by type,
  with Academics and OJT shown in separate lanes for easy comparison. A dashed marker shows today.
- **Class scheduling** – create classes with a name, type (Academics / OJT), start and end dates,
  assigned instructor, and the prerequisites required to attend.
- **Roster import** – import a class roster from CSV. Students are matched to existing records by
  Employee ID (then name) or created if new, and enrolled in the selected class.
- **Green / Red prerequisite status** – for the selected class, every enrolled student is shown
  Green or Red with the list of any missing prerequisites. A student's completed prerequisites can
  be edited directly.
- **Instructor availability** – define instructor leave periods; the app reports whether an
  instructor can host a given class, flagging conflicts with leave or with another class they are
  already teaching.
- **Local storage** – all data is saved as JSON under `%AppData%\ClassRotation\`. No database or
  server required. A small sample data set is created on first launch.

## Project layout

```
src/
  ClassRotation.Core/     Cross-platform (net8.0) domain + data logic (no UI dependency)
    Domain/               Models, prerequisite checker, instructor availability
    Data/                 JSON store, CSV roster importer, sample seed data
  ClassRotation/          WinForms app (net8.0-windows): timeline, tabs, dialogs
tests/
  ClassRotation.Tests/    xUnit tests for the core logic
samples/
  sample-roster.csv       Example roster for the import feature
```

The non-UI logic lives in `ClassRotation.Core` so it can be unit-tested on any platform, while the
WinForms front-end depends on it.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Windows** to run the app (WinForms requires the Windows Desktop runtime). The core library and
  tests build and run on any platform.

## Build & run (Windows)

```powershell
dotnet build
dotnet run --project src/ClassRotation
```

To produce a self-contained executable:

```powershell
dotnet publish src/ClassRotation -c Release -r win-x64 --self-contained
```

## Run the tests (any platform)

```bash
dotnet test tests/ClassRotation.Tests
```

## CSV roster format

A header row is required (columns are case-insensitive):

| Column                   | Required | Notes                                                        |
|--------------------------|----------|--------------------------------------------------------------|
| `Name`                   | yes      | Student's full name                                          |
| `EmployeeId`             | no       | Used to match/deduplicate students                           |
| `CompletedPrerequisites` | no       | `;`-separated prerequisite **names** (must match defined ones)|

See [`samples/sample-roster.csv`](samples/sample-roster.csv).

## Note on "VB6"

This project was originally scoped as a VB6 app. VB6's IDE is Windows-only and long discontinued,
so it is implemented in **VB.NET / WinForms on .NET 8** — the modern successor that keeps the
Visual Basic language and Windows desktop experience while remaining buildable and testable today.
