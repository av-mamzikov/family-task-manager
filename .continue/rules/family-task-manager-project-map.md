---
globs: "**/*"
description: >-
  This rule provides a comprehensive map of the Family Task Manager project
  based on Clean Architecture. Key components
  include:

  1. **Core Layer (Domain)**: Contains aggregates (Family, Task, Spot, User),
  value objects, specifications, interfaces, and domain services

  2. **UseCases Layer (Application)**: Contains command/query handlers organized
  by domain (Families, Tasks, Spots, Users, Statistics)

  3. **Infrastructure Layer**: Contains EF Core data access, repositories, jobs
  (Quartz), notifications, and adapters

  4. **Host Layer (Modular Monolith)**: Main entry point with Bot and Worker
  modules

  5. **AspireHost Layer**: .NET Aspire orchestration for local development

  6. **Tests**: Comprehensive test suite including unit, functional,
  integration, E2E, and Aspire tests


  Key architectural patterns: Clean Architecture, CQRS with Mediator, Repository
  pattern with custom IAppRepository/IAppReadRepository, Domain Events, Modular
  Monolith
alwaysApply: true
---

Maintain awareness of the Family Task Manager project structure, architecture, and key components for all future interactions