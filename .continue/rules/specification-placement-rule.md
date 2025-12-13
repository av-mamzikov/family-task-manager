---
globs: "**/*.cs"
description: This rule ensures consistent organization of Specification classes
  according to Clean Architecture principles. Specifications are domain concepts
  that belong to the Core layer, not the Application/UseCases layer. Keeping
  them in the Aggregate's Specifications folder maintains architectural purity
  and makes specifications reusable across different use cases.
alwaysApply: true
---

Always place Specification classes in the "Specifications" folder within the corresponding Aggregate folder in the Core layer (e.g., src/FamilyTaskManager.Core/TaskAggregate/Specifications/). Never define Specifications inline in UseCase files.