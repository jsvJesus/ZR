# F8 camera inspection during Play

**Goal:** Toggle a detached camera with F8 while keeping the visible character at the
exact current position and animation pose, so blocked collision situations can be inspected.

**Approach:** Keep the existing Play session, third-person orbit and editor camera intact.
Use a separate FreeCamera initialized from the currently rendered Play view on entry. Freeze
character updates while inspecting; route mouse and flight keys to the detached camera.
Repeat F8 to resume the original third-person camera and character. Esc still stops Play.
Character Editor exits inspection before taking over the orbit. Reuse Play raw mouse capture.

**Controls:** Mouse look, WASD horizontal flight, Space up, Shift down, wheel speed.
Clear queued gameplay input on mode transitions; suppress F8 autorepeat and gameplay actions.

**Constraints:** No tests, test apps, builds or project launches, per AGENTS.md. Static only.

- [x] Add inspection state, camera switching and input/render/update routing.
- [x] Show active-mode controls and reset inspection on Stop/level change/Character Editor.
- [x] Statically inspect focus, key repeat, held input, pose freeze and camera restoration;
  check XAML and whitespace. Preserve existing unrelated character collision changes.

**Verification:** Separate static review reported no actionable findings across input,
focus/capture, render routing, frozen pose, camera preservation, slider feedback and resets.
MainWindow.axaml parses with 33 unique named controls and no duplicate names. Scoped
whitespace inspection completed. No tests, builds, applications or shader compilation ran;
native input/capture and rendered behavior remain unverified at runtime.
