# Fit View Scale Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement custom Grasshopper component `FitViewScaleComponent` in `custom/QD_GrasshopperTeklaDrawingLink` to automatically adjust the Scale of a Tekla Drawing View by increments divisible by 5 so that the view frame geometry fits within user-defined maximum width and height, keeping the view as large as possible.

**Architecture:**
- Create `FitViewScaleComponent` inheriting `TeklaComponentBaseNew<FitViewScaleCommand>` in `custom/QD_GrasshopperTeklaDrawingLink/FitViewScaleComponent.cs`.
- Category: `"QD"`, SubCategory: `QD_PanelHeadings.Drawing` (`"Drawing"`).
- ComponentGuid: `93cc47b0-3a3b-43de-ab91-4ba697b3b87a`.
- Zero core modifications: no changes to `src/GrasshopperTeklaDrawingLink/`.
- Hybrid optimization algorithm:
  1. If current frame <= limits, keep current scale.
  2. Compute theoretical lower bound scale: $S_{\text{theo}} = S_{\text{current}} \times \max(W/W_{\max}, H/H_{\max})$, round up to nearest multiple of `Step` (default 5).
  3. Iteratively test in Tekla via `view.Attributes.Scale = targetScale; view.Modify(); view.Select();` up to max 20 iterations.
  4. If `apply == true`, commit drawing changes (`DrawingInteractor.CommitChanges()`); if `false`, restore original scale (preview mode).
- Target multi-version build: Tekla 2021 & Tekla 2024.

---

## Global Constraints

- Never modify files in `src/GrasshopperTeklaDrawingLink/`.
- All custom component code must stay in `custom/QD_GrasshopperTeklaDrawingLink/`.
- Category on canvas: `QD`, SubCategory: `Drawing`.
- ComponentGuid must be `93cc47b0-3a3b-43de-ab91-4ba697b3b87a`.
- Build configurations: `2021` and `2024`.

---

## Task Breakdown

### Task 1: Create `FitViewScaleComponent.cs`

**Files:**
- Create: `custom/QD_GrasshopperTeklaDrawingLink/FitViewScaleComponent.cs`

- [x] **Step 1:** Define `FitViewScaleComponent` inheriting `TeklaComponentBaseNew<FitViewScaleCommand>`, setting `Category = "QD"`, `SubCategory = QD_PanelHeadings.Drawing`, ComponentGuid, and embedded 24x24 Icon.
- [x] **Step 2:** Define `FitViewScaleCommand` with inputs:
  - `_inView`: `InputParam<View>`
  - `_inMaxWidth`: `InputStructParam<double>`
  - `_inMaxHeight`: `InputStructParam<double>`
  - `_inStep`: `InputOptionalStructParam<int>` (default 5)
  - `_inApply`: `InputOptionalStructParam<bool>` (default true)
  and outputs:
  - `_outView`: `OutputParam<View>`
  - `_outScale`: `OutputParam<double>`
  - `_outWidth`: `OutputParam<double>`
  - `_outHeight`: `OutputParam<double>`
  - `_outFits`: `OutputParam<bool>`
- [x] **Step 3:** Implement execution logic in `InvokeCommand`:
  - Validate inputs (View != null, MaxWidth > 0, MaxHeight > 0, Step > 0).
  - Check current size against limits; keep scale if already within bounds.
  - Calculate theoretical lower bound scale rounded up to multiple of Step.
  - Verification loop with `view.Modify()` and `view.Select()`.
  - Handle `apply` flag and `DrawingInteractor.CommitChanges()`.
  - Set outputs.

### Task 2: Build and verify against Tekla 2021 & Tekla 2024

**Files:**
- Build output: `custom/QD_GrasshopperTeklaDrawingLink/bin/2021/QD_G-TDrawingLink_2021.dll`
- Build output: `custom/QD_GrasshopperTeklaDrawingLink/bin/2024/QD_G-TDrawingLink_2024.dll`

- [x] **Step 1:** Build project for Configuration `2021`:
  `dotnet build custom/QD_GrasshopperTeklaDrawingLink/QD_GrasshopperTeklaDrawingLink.csproj -c 2021`
- [x] **Step 2:** Verify 0 build errors and DLL output generated.
- [x] **Step 3:** Build project for Configuration `2024`:
  `dotnet build custom/QD_GrasshopperTeklaDrawingLink/QD_GrasshopperTeklaDrawingLink.csproj -c 2024`
- [x] **Step 4:** Verify 0 build errors and DLL output generated.

### Task 3: Update documentation

**Files:**
- Modify: `docs/CUSTOM_COMPONENTS.md`

- [x] **Step 1:** Document `FitViewScaleComponent` in `docs/CUSTOM_COMPONENTS.md` (Name, NickName, Guid, Category/SubCategory, Inputs, Outputs, Purpose).
- [x] **Step 2:** Verify git status and ensure zero core changes.
