# Shared kiosk and accessibility

Start the API and frontend normally. Open http://127.0.0.1:3000/kiosk for a shared
screen, or http://127.0.0.1:3000 for the existing personal-device experience.

## Kiosk behaviour

- Exhibition selections remain in React memory only. The kiosk never calls the
  itinerary save/restore endpoints or stores visitor tokens.
- Entering/resetting the kiosk removes this application's saved itinerary key
  from localStorage and sessionStorage where storage is accessible. Other browser
  data is not cleared. Use a separate browser profile for a shared kiosk.
- Finish visit clears selections, filters, expanded details and messages. Focus
  returns to the main content and the page scrolls to the top.
- After four minutes without keyboard, touch, wheel or pointer-down activity, a
  modal warns that the visit will reset in one minute. Mere pointer movement is
  not counted, avoiding accidental activity from a noisy mouse.
- Continue my visit adds another five minutes, with unlimited extensions. Escape
  also continues. Modal focus is contained by the native HTML dialog and returns
  to the previous control when continuing.
- At five idle minutes the visit resets. Checks use an absolute deadline and run
  on visibility, focus and pageshow as well as a timer, so a suspended tab resets
  when it resumes. Browser/OS suspension cannot execute JavaScript while suspended.
- Reloading the kiosk also starts an empty plan. The normal / page keeps its
  existing 30-day save-and-restore behaviour.

This is application behaviour, not OS kiosk lockdown. An operator must separately
configure the browser/Windows kiosk account to prevent navigation away, browser
history access or other desktop use. The app does not configure those settings.

## Accessibility changes

- Focusable main/visit targets and a skip link support keyboard navigation.
- Removing an itinerary stop moves focus to the visit heading instead of losing it.
- Filters form a labelled group with pressed states and a polite result count.
- Buttons have at least 44px height; remove controls are at least 44px square.
- Muted text is darker, focus outlines remain visible, and forced-colour button
  states have explicit borders.
- Narrow layouts stack cards at 400px and below. Reduced-motion preferences retain
  the existing non-smooth scrolling rule.
- The timeout dialog has an accessible name/description. Its countdown is not
  announced every second; the warning offers a full minute to extend the session.

## Browser checks

From frontend:

```powershell
npm ci
npx playwright install chromium
npm run test:e2e
npm run typecheck
npm run build
```

Playwright starts a separate frontend on port 3100 and uses fictional API fixtures,
so these tests do not need SQL Server or Sanity and do not create real itineraries.
The tests cover kiosk storage isolation, finish/reset, timeout extension, focus,
late resumption, personal saving/restoring, narrow reflow and axe accessibility scans.
GitHub Actions runs the browser checks as well.

## Remaining manual verification

Automated scans are not a WCAG conformance certification. On the actual kiosk:

1. Use Tab/Shift+Tab, Enter and Space to browse, filter, expand, add/remove and finish.
2. Test the warning with a screen reader (NVDA or Narrator): verify its announcement,
   focus order, Continue and Finish actions, and focus restoration.
3. Test repeated extensions, the final idle reset, and sleep/wake behaviour.
4. Check browser zoom at 200% and 400%, Windows high contrast, reduced motion and
   touchscreen use at the intended display size.
5. Confirm saving on the personal / page still works with your real SQL Server and
   exhibition content works with your Sanity project.

References: [W3C timing adjustable](https://www.w3.org/WAI/WCAG22/Understanding/timing-adjustable)
and [modal dialog keyboard/focus guidance](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/).

## Verification record (31 October 2026)

Eight Chromium browser scenarios passed. Axe reported no WCAG A/AA violations
in the tested desktop, 320px layout and timeout-dialog states. Keyboard focus,
skip navigation, reduced-motion scrolling, storage isolation, reset/resume and
personal saving/restoring were exercised using fixture responses. Desktop/mobile
screenshots were visually reviewed. TypeScript and the Next.js production build
passed, including the /kiosk route. This does not certify untested content, other
browsers, real assistive technology or the physical kiosk.

In this sandbox, the Playwright runner needed interruption during web-server teardown after all eight scenarios reported success; no test was interrupted mid-scenario. Normal CI execution is configured but has not been pushed or run yet.
