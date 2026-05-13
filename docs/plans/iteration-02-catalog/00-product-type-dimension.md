---
title: ProductType + Dimension aggregates
iteration: 02
status: done
labels: [catalog, domain]
depends-on: []
agent: backend
---

# ProductType + Dimension aggregates

`ProductType` is an Aggregate Root in the Catalog context. It owns `Dimension`s as Child Entities. A Dimension declares `Code` (unique within ProductType), display name, unit, and `Aggregation` (one of five values).

## Acceptance criteria

- Strongly-Typed `ProductTypeId`, `DimensionId`.
- `ProductType.AddDimension(code, name, unit, aggregation)` enforces unique code.
- Aggregation is a closed enum/discriminated value: `Sum | Last | Max | UniqueCount | TimeWeightedLast`.
- Dimensions cannot be removed once referenced by any PlanVersion (defer enforcement until Iteration 03 binding exists; for now, deletion is forbidden once any PricingComponent references the Dimension).
- EF Core mapping with `catalog` schema.
