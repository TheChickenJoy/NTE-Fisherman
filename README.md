<p align="center">
  <img width="883" height="795" alt="image" src="https://github.com/user-attachments/assets/15e0f9e4-0231-4073-baf4-66959a37e6f0" />
</p>


# NTE-Fisherman
A fishing bot using OpenCVSharp and SharpDX, originally a Tower of Fantasy bot made by mdnpascual and repurposed for Neverness to Everness.

**NTE Fisherman** is an automated fishing bot for NTE that handles casting, reeling, and recasting so you don't have to watch the screen and click manually. It requires some setup, but after the first time it is done you rarely need to touch it again.

> Follow the video/picture step by step, you won't have issues. [Setup Guide](https://www.youtube.com/watch?v=BlUfjPJfAh4)

---

### Requirements
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
  
---

## Detection Points

| Point | What to sample |
|---|---|
| Fish Stamina | The colored bar above the fish during the minigame |
| Player Stamina | Your own stamina bar during the minigame |
| Middle Bar | The green zone on the fishing bar |
| Cursor | The white cursor on the fishing bar |
| Cast Button | Center of the blue cast/hook button (when a fish bites) |
| XP Bar | The pink/red experience bar shown after catching a fish |
| Daily Reward | A pixel on the Daily Reward popup button |

Click the picker button, then click the exact pixel on your screen. The bot stores the coordinate and color.

---

## How It Works

1. **Casting.** Bot detects the blue cast button and presses the cast key (`F` by default).
2. **Fishing minigame.** Holds `A` or `D` to keep the cursor inside the green zone.
3. **End of minigame.** When stamina bars disappear, enters the capture phase.
4. **XP bar wait.** Waits up to 15 seconds for the XP bar to appear. This handles long rare-fish catch animations where the cast button area becomes visible before the XP bar shows up. The bot never treats cast-button visibility alone as a genuine escape during this phase. Post-catch genuine-escape detection uses cast-ready stability (5s with no XP bar).
5. **Dismiss.** Clicks to dismiss the catch dialog, retrying up to 8 times if the bar stays visible.
6. **Recast.** Casts again and repeats.

- There's an optional bait counter, which stops the bot after using all the bait available. (Enter # of Bait and Bot Ends after # of Bait)

---

## Recommended: Play with Full Focus

Use the bot while the game is your active window. This is the most stable and reliable mode. The bot will handle all inputs cleanly and fishing will work as expected.

## Background Inputs

Background Inputs now works properly and lets the bot send inputs while you're focused on another window. That said, it can still occasionally bug out, so it's best used when you're keeping an eye on the game on the side (e.g. scrolling Discord or watching a video). If you plan to go fully AFK and step away from your PC, keep the game focused for the most reliable experience.

---

## Starting Out at Level 1 Fishing

If you're just starting, watch the bot for at least the first hour. You'll need to manually sell your fish and use the money to buy more bait to keep the cycle going. The overnight/AFK farm only becomes reliable once you have around **1,000+ bait** stockpiled. Anything less and you'll run out mid-session and the bot will sit idle doing nothing. Note that the fishing in NTE does not auto switch bait once you run out, so you'll have to do it manually.

- 99 Stacks of Basic Bait = 495 Shell
- Avg starting fish is 10 to 20 Shell
- You need about **5,445 Shell total** for 1k bait (more the better)

---

## Fishing Recommendation Spot

<p align="center">
  <img width="2311" height="1439" alt="image" src="https://github.com/user-attachments/assets/cd92204d-ac52-43d7-b729-28f56e7c9dd3" />
</p>

---

## Troubleshooting

**Bot never detects a fish bite.** Check that Fish Stamina and Player Stamina points turn green during the minigame.

**Bot recasts without catching.** The XP Bar point may be wrong. Sample the pink bar that appears after a catch.

**Bot dismisses too early.** Increase Post-Catch Delay or After-Click Delay in Settings.

**Keys not working.** Verify the game process name. Try enabling Background Input.

**Daily Reward not dismissed.** Reconfigure the Daily Reward point with a uniquely-colored pixel from the popup button.

---

## Settings Reference

| Setting | Default | Description |
|---|---|---|
| Game Process Name | `HTGame` | Executable name of the game (no .exe) |
| Stamina Color Threshold | 40 | Pixel color match tolerance |
| Middlebar Color Threshold | 10 | Tighter tolerance for green zone detection |
| Lag Compensation Delay | 5000 ms | Ignores stamina reads after casting |
| Post-Catch Delay | 1000 ms | Wait after minigame ends before dismiss sequence |
| After-Click Delay | 1000 ms | Cooldown after dismiss (genuine escape path) |
| Background Input | Off | Send input without focusing the game window |
| Discord Hook URL (WIP) | (none) | Webhook URL for out-of-bait notifications |
| Discord User ID (WIP) | (none) | Your Discord user ID for @mentions |

## Customizing Delays

Only delays I would recommend touching are:

```
Dismiss Delay (default: 4000)  ->  2000
After Catch   (default: 1000)  ->  1000
After Esc     (default: 1000)  ->  same
```
