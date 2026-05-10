# Changelog

---

## Version 2.2.3

- **Auto-Scale now resizes the game window for you.** No need to manually set the game to 1280x720 first. The bot switches the game to windowed mode, adds the title bar, and resizes it automatically when you click Auto-Scale.
- **Fixed XP bar being skipped on rare fish.** The bot was sometimes clicking dismiss before the XP bar ever appeared during long rare-fish catch animations. It now always waits up to 15 seconds for the XP bar before doing anything.
- **Daily Reward now clicks 3 times, 3 seconds apart.** A single click was sometimes not enough to clear the popup.
- **Fixed Daily Reward auto-scale coordinates.** The preset point was slightly off at 1440p and has been corrected.
- **Menu icons updated.** Settings and Dark Mode now use clean monochrome symbols instead of colored images. Stay on Top and Background Input icons turn green when active so you can tell at a glance, even with the side menu collapsed.
- **Welcome Screen button added to FAQ.** You can reopen the first-run welcome page anytime from the FAQ header.

---

## Version 2.2.2

- **Fixed rare fish being dismissed too early.** The bot now tracks whether the XP bar has appeared this catch cycle and always waits up to 15 seconds before acting in the post-catch phase. The brief appearance of the cast-ready button during a rare-fish animation no longer triggers an early recast.
- **Simplified the post-catch flow.** Both normal and rare catches now go through the same path when the XP bar is configured. The old split between captured and escaped only remains as a fallback for users who have not set up the XP bar point.

---

## Version 2.2.1

- **Improved green zone detection.** Replaced the old line detector with contour-based HSV tracking. More reliable and no longer snaps to zero randomly when detection briefly loses the zone.
- **Improved cursor detection.** Same contour tracking applied to the fishing cursor. Returns a more accurate center position.
- **Better color matching.** Detection now uses HSV color space instead of raw RGB. More forgiving of compression artifacts and slight lighting differences.

---

## Version 2.2

- **Background Inputs reworked.** Now sends keyboard and mouse inputs properly without stealing window focus. Should work reliably while you are using another app.
- **Fixed dismiss click location.** Click now targets the bottom-left corner of the game window where the caught fish UI actually is, instead of the wrong corner.
- **Fixed clicks not registering on child windows.** The click now finds the correct child window inside the game so it always lands in the right place.
- **AFK Mode removed.** F-spam while waiting for a fish is now on by default for everyone. The separate AFK toggle has been removed.
- **Fallback dismiss loop added.** If the XP bar is still visible after 8 dismiss attempts, the bot keeps clicking every 2 seconds until it clears instead of giving up.
- **Video Setup Guide button added** to the FAQ header. Opens the YouTube guide directly.
- **FAQ and welcome page updated** to reflect Background Inputs changes.
- **Chibi hint added** below the character image pointing new users to the FAQ and video guide.

---

## Version 2.1.1

- **AFK Mode added.** After dismissing the caught fish display, the bot spams F every 700ms until the fishing stamina bar is detected. Useful if Fish Hooked Detect is unreliable for your setup.
- **AFK Mode warning.** Turning it on shows a reminder to disable Background Inputs and keep the game focused.
- **Log spam reduced.** AFK F presses update a single log line instead of flooding the log.

---

## Version 2.1

- **Fixed dismiss click landing on the fish card.** Click now targets the 90%/90% corner of the game window instead of center, so the card is never accidentally re-triggered.
- **Fixed dismiss click going to the wrong monitor.** Coordinates now use the full virtual desktop size so multi-monitor setups work correctly.
- **Removed ESC after dismiss.** The ESC press after clicking was exiting the fishing screen entirely and has been removed.
- **Dismiss click always uses foreground mode.** Saves and restores your cursor position when Background Inputs is on so your mouse does not jump.
- **Fixed cast retry loop being skipped every recast.** The confirmed cast flag was never being reset between casts, so every recast after the first was skipping the retry loop.
- **30-second cast retry timeout.** The retry loop now gives up after 30 seconds so the bot never hangs indefinitely.
- **Daily Reward Detect note added.** Small label under the button clarifies it is only set automatically by Auto-Scale.
- **Admin warning banner added.** A yellow banner shows when the bot is not running as Administrator.
- **Debug Log Export button added.** Saves the full log to a text file.
- **Chibi image enlarged** from 160px to 210px.

---

## Version 2.0

- **Full UI redesign.** Side menu with a hamburger toggle replaces the old button layout. Settings, FAQ, and Debug Log are now slide-in panels.
- **Dark and Light mode.** Toggle from the side menu. Preference is saved between sessions.
- **Ko-fi button added** to the bottom of the side menu.
- **First-run welcome page.** Shown automatically on first launch with setup tips and a link to the FAQ.
- **Pre-configured 1280x720 presets.** One-click setup for 1080p, 1440p, and 4K monitors. Fills in all detection coordinates and colors automatically.
- **Successful Cast Detect excluded from pre-config.** Resets to zero on apply so users always calibrate it manually to their own username.
- **Pre-config warning added.** Amber notice in the preset dialog reminds you to place Successful Cast Detect on the middle letter of your character name.
- **Fixed memory leaks** in the frame processing loop. Mat, Bitmap, and MemoryStream objects are now disposed properly every frame.
- **Fixed mouse freeze on bot stop.** DirectX resources are now disposed correctly on the background thread when the bot stops.

---

## Version 1.09.9

- **Pre-configured button is now a toggle.** A green indicator below it shows when it is active. State is saved across restarts.
- **Auto-center window pinning.** When pre-configured mode is on, the bot keeps the game window centered on your screen and re-checks every 2 seconds.
- **Fixed double F press cancelling cast.** Post-catch was pressing F twice in quick succession, cancelling the cast. Fixed.
- **Cast retry interval raised from 1s to 5s.** Rapid retries were cancelling in-progress casts.
- **Fixed stuck dismiss bug.** Unexpected errors inside the post-catch sequence now recover gracefully instead of leaving the bot stuck.
- **Stuck-state watchdogs.** Two recovery paths added: one for being stuck in the dismiss loop too long, one for state never resetting from ResetStart.

---

## Version 1.09.8

- **Fixed stuck dismiss bug.** Any error in the post-catch sequence now recovers and recasts instead of freezing permanently.
- **Fixed concurrent bitmap crash.** The ESC retry loop now takes a snapshot of the current frame before checking it.

---

## Version 1.09.7

- **Settle delay before cast.** Added 500ms after the ESC retry loop before pressing F so the game fully closes the dialog first.

---

## Version 1.09.6

- **Fixed Successful Cast Detect timing.** State is now set to NotFishing before pressing F so the confirmation check never misses the character talking.
- **EXP countdown timer.** Status shows a live countdown while waiting for the EXP bar.

---

## Version 1.09.5

- **Cast retry now loops.** Bot keeps pressing F every 3 seconds until Successful Cast Detect confirms the cast, instead of retrying just once.
- **Failed catch also checks cast.** Successful Cast Detect now runs after both caught and failed catches.
- **Fish Hooked Detect renamed.** Previously called Cast Detect.
- **Failed catch timeout changed to 6 seconds.**

---

## Version 1.09.4

- **Failed catch no longer presses ESC.** The game returns to default automatically after a failed catch. Pressing ESC was exiting fishing entirely.
- **Failed catch timeout increased to 7 seconds.**
- **Status message on failed catch** shows "Fish escaped, waiting..." so you know what happened.

---

## Version 1.09.3

- **Removed unnecessary Space press** after reeling.
- **EXP menu detection is now immediate.** Bot watches for the EXP menu as soon as stamina bars disappear instead of waiting 3 seconds.
- **ESC always fires** with up to 8 retries if the menu is still showing.
- **5 second fallback.** If the EXP bar is never detected, bot presses ESC and moves on.
- **After Catch default changed** from 500ms to 1000ms.

---

## Version 1.09.2

- **Fixed Background Inputs consistency.** A/D movement, ESC, and fish capture now all respect the Background Inputs setting correctly.
- **Capture error recovery.** Bad screen capture frames are skipped silently. Bot only stops if 10 consecutive errors occur.

---

## Version 1.09

- **ESC retry loop added.** After dismissing the fish dialog, the bot checks if the XP bar is still visible and keeps pressing ESC until it is gone (up to 8 retries).

---

## Version 1.08

- **Stop resets everything.** Stopping the bot clears the status, turns off indicators, and releases all held keys.
- **Fixed stuck keys.** A or D keys could stay pressed after stopping mid-fish. Fixed.

---

## Version 1.07

- **Successful Cast Detect added.** Bot checks if your character started talking after casting to confirm the cast worked.
- **Cast retry added.** If the cast did not register, the bot presses F again until it does.
- **Stop button actually stops.** Held keys are released and pending actions cancelled instantly.
- **Indicator lights moved** from button borders to pill-shaped bars under each button.
- **Background Inputs toggle added.**
- **Background Inputs warning popup added.**
- **Always On Top button added.**
- **Window made taller** so all buttons are fully visible.

---

## Version 1.06

- Focus settle time reduced from 80ms to 30ms.
- Cast retry interval reduced from 2000ms to 1000ms.
- 5-second post-catch cast retry added.

---

## Version 1.05

- Stronger focus transfer using BringWindowToTop and SwitchToThisWindow.
- Added fallback 2-second cast retry loop.

---

## Version 1.04

- Fixed background inputs not firing silently.
- Fixed bot getting stuck in Cast Detecting without pressing F.

---

## Version 1.03

- Added background inputs.
- Fixed bugs with Cast Detect.
