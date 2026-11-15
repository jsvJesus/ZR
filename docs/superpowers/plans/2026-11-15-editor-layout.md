# Editor layout implementation plan

> **For agentic workers:** Use superpowers:executing-plans. Follow project restrictions instead of test/build steps.

**Goal:** Implement the approved fixed editor layout with contextual browsing on the left and properties on the right.
**Architecture:** Keep existing panel instances and event handlers. Expose separate browser controls from panels, host them in the shell, synchronize visibility and enabled state. Keep the native viewport mounted.
**Tech Stack:** C#, Avalonia, existing Fluent theme, DX11.
**Spec:** `docs/superpowers/specs/2026-11-15-editor-layout-design.md`

## Global constraints

- No tests, test applications or builds. Static inspection only.
- No docking or new dependencies.
- Preserve unrelated working-tree changes and work in the current feature checkout.

## Review focus

- Browser controls must have one parent and retain selection callbacks.
- Busy / Play / drag locks must apply to both panel halves and toolbar commands.
- Collapsing sidebars must not deactivate editing modes or recreate the native viewport.
- Navigation from menus and side buttons must agree about the selected mode.
- At minimum window size, sidebars must leave usable scene space and permit restoring hidden panels.

## Tasks

- [x] Add shared compact styles and rebuild the main shell with collapsible sidebars, toolbar, camera and help sections.
- [x] Separate browsing from properties for objects, terrain, vegetation, roads and character equipment. Keep environment section selection on the left.
- [x] Connect navigation, sidebar resizing and context-sensitive undo/redo to existing state and actions.
- [x] Inspect XML, names, event handlers, control ownership and lock propagation; review the final diff. Record limitations.

## Execution notes

The user approved the concrete layout and requested implementation. Continue without another approval round. Existing change in `bin/packs/levels/Escape/terrain/updates/.../paint-overview.bin` is unrelated and must remain untouched.

## Verification record

- XML parsing of App, MainWindow and EditorTheme completed without errors.
- No duplicate XAML names, unresolved element bindings, missing click handlers, or C# references to removed controls found.
- Checked new Avalonia APIs against installed 12.1.4 XML documentation.
- Independent source review completed; Play-to-character and object subsection navigation retain existing behavior.
- `git diff --check` passed. Control ownership, sidebar state, and busy/drag guards inspected.
- Attempted standalone Roslyn syntax parsing could not complete because the PowerShell host could not resolve parser dependencies; no C# syntax-parser or compilation success is claimed.
- No tests, test applications, builds or editor launches performed. Runtime appearance and XAML compilation remain unverified.
