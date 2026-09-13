<div align="center">

# 🏏 VR Cricket Bat Game

**An immersive VR cricket batting experience built in Unity.**
Realistic ball physics · Stadium environment · Hand-tracked bat grip · Meta Quest + SteamVR support

[![Unity](https://img.shields.io/badge/Unity-2022.3 LTS-000000?style=flat-square&logo=unity&logoColor=white)](https://unity.com)
[![C#](https://img.shields.io/badge/C%23-11-239120?style=flat-square&logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Meta Quest](https://img.shields.io/badge/Meta-Quest%202%2F3-1C1E20?style=flat-square)](https://www.meta.com/quest/)
[![SteamVR](https://img.shields.io/badge/SteamVR-PC%20VR-1B2838?style=flat-square&logo=steam&logoColor=white)](https://store.steampowered.com/app/250820/SteamVR/)
[![License](https://img.shields.io/badge/license-MIT-a855f7?style=flat-square)](LICENSE)

</div>

---

## The Problem

Cricket has always been one of the hardest sports to bring into VR convincingly. The physics of bat-on-ball contact, the timing window for a cover drive, the footwork — standard VR sports games sacrifice all of it for broad accessibility. This project does the opposite: it prioritises the *feel* of batting.

VR Cricket Bat Game is a physics-first experience. The bat is a real rigid body in the scene. The ball has seam movement, varying pitch length, and realistic bounce. When you connect, you feel it through your controller's haptics. Miss the line and you edge to slip. It rewards the mechanics that make cricket interesting.

---

## ✨ Features

- 🏏 **Physics-accurate bat** — full rigid-body bat with proper collision meshes; hitting off the sweet spot matters
- 🎾 **Ball delivery system** — configurable pace (80–95mph), swing (in/out-swing), seam angle, and pitch length
- 🏟️ **Stadium environment** — fully modelled cricket ground with crowd ambient audio, grass texture variation, and sky cycling
- ✋ **Hand-tracking bat grip** — grip the bat handle naturally using Quest hand tracking or controller grip input; two-handed grip changes bat weight feel
- 🎯 **Pitch difficulty** — Easy (slower, full-pitched), Medium (good length, mild swing), Hard (short-of-length, late movement)
- 🏆 **Leaderboard** — local high scores by runs and strike rate; online leaderboard via PlayFab
- 📊 **Shot analytics** — tracks where the ball went, shot type (drive, cut, pull), and timing window hit
- 🔊 **Spatial audio** — crowd reacts to boundaries and wickets; wood-on-leather impact has positional audio
- 🥅 **Multiple dismissal modes** — LBW detection, edged catches, stump hits, run-out chase mode
- 🌅 **Day/night cycle** — optional dynamic lighting cycling from morning to floodlit evening

---

## 🏗️ Architecture

```
┌──────────────────────────────────────────────────────┐
│                  Unity Scene (Game World)             │
│                                                       │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────┐  │
│  │ BallDelivery │  │ BatPhysics   │  │ Stadium    │  │
│  │ System       │  │ Controller   │  │ Manager    │  │
│  └──────┬───────┘  └──────┬───────┘  └────────────┘  │
│         │ Rigidbody        │ Input                    │
│  ┌──────▼───────────────────▼──────────────────────┐  │
│  │            Physics Engine (Unity)                │  │
│  │   Collision  ·  Haptic Feedback  ·  Trajectory   │  │
│  └────────────────────────┬────────────────────────┘  │
│                           │                           │
│  ┌────────────────────────▼────────────────────────┐  │
│  │            Game State Manager                    │  │
│  │  Score · Wickets · Over counter · Analytics      │  │
│  └────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────┘
         │ XR Input                      │ PlayFab SDK
┌────────▼──────────┐         ┌──────────▼────────────┐
│  Meta Quest SDK   │         │  PlayFab Leaderboard   │
│  SteamVR SDK      │         │  (Online scores)       │
└───────────────────┘         └───────────────────────┘
```

---

## 🚀 Quick Start

### Prerequisites

- Unity 2022.3 LTS
- Meta XR SDK (for Quest) or SteamVR Plugin (for PC VR)
- Android Build Support module (for Quest standalone build)

### 1 — Clone and open in Unity

```bash
git clone https://github.com/harryatwork/vr-cricket-bat-game
```

Open Unity Hub → Add Project → select the cloned folder.

### 2 — Import SDKs

- **Meta Quest:** Import `Meta XR All-in-One SDK` from the Unity Asset Store
- **PC VR / SteamVR:** Import `SteamVR Plugin` from the Asset Store

### 3 — Configure target platform

For **Meta Quest (standalone)**:
- File → Build Settings → Android → Switch Platform
- Player Settings → XR Plug-in Management → Enable Oculus

For **PC VR (SteamVR)**:
- File → Build Settings → PC, Mac & Linux → Switch Platform
- Player Settings → XR Plug-in Management → Enable OpenVR

### 4 — Build

```
File → Build Settings → Build
```

Deploy the APK to your Quest via `adb install VRCricketBatGame.apk` or run directly from the Unity Editor with your VR headset connected.

---

## 🎮 Controls

| Action | Quest (controller) | Quest (hand tracking) | PC VR |
|---|---|---|---|
| Grip bat | Hold Grip button | Natural grip on handle | Hold Grip button |
| Stance left/right | Left joystick | Step with feet | Left thumbstick |
| Pause | Menu button | Thumbs-up gesture | System button |
| Request delivery | A button | Index pinch | A button |

---

## ⚙️ Configuration

In `Assets/Config/GameConfig.asset`:

| Setting | Default | Description |
|---|---|---|
| `BallSpeed` | `85 mph` | Average ball speed |
| `SwingAmount` | `0.3` | Lateral movement (0–1) |
| `PitchLength` | `Good` | Delivery pitch point |
| `EnableHandTracking` | `true` | Use Quest hand tracking |
| `HapticsIntensity` | `0.8` | Controller rumble on impact |
| `EnableLeaderboard` | `true` | Push scores to PlayFab |
| `CrowdDensity` | `Medium` | Stadium crowd rendering detail |

---

## 📁 Project Structure

```
vr-cricket-bat-game/
├── Assets/
│   ├── Scripts/
│   │   ├── Ball/
│   │   │   ├── BallDelivery.cs      # Delivery physics + trajectory
│   │   │   └── BallSwing.cs         # Swing and seam movement
│   │   ├── Bat/
│   │   │   ├── BatPhysics.cs        # Rigidbody bat controller
│   │   │   └── GripHandler.cs       # Hand tracking grip input
│   │   ├── Game/
│   │   │   ├── GameManager.cs       # Score, wickets, over count
│   │   │   ├── ShotAnalytics.cs     # Shot classification + replay
│   │   │   └── LeaderboardManager.cs
│   │   ├── Stadium/
│   │   │   ├── CrowdController.cs   # Crowd reaction system
│   │   │   └── DayNightCycle.cs
│   │   └── UI/
│   │       ├── ScoreboardUI.cs
│   │       └── PauseMenuUI.cs
│   ├── Prefabs/
│   ├── Scenes/
│   │   ├── MainMenu.unity
│   │   └── CricketGround.unity
│   ├── Audio/
│   └── Config/
│       └── GameConfig.asset
└── Packages/
    └── manifest.json
```

---

<details>
<summary><strong>Common issues and fixes</strong></summary>

| Error | Fix |
|---|---|
| Black screen on Quest | Enable Oculus XR plugin in XR Plug-in Management and rebuild |
| Ball clipping through bat | Lower `Physics.defaultContactOffset` in Project Settings → Physics |
| Hand tracking not responding | Enable Hand Tracking Support in Quest developer settings |
| Build fails: missing SDK | Import Meta XR All-in-One SDK from Asset Store |
| PlayFab scores not uploading | Set `PlayFabTitleID` in `Assets/Config/GameConfig.asset` |
| Low frame rate on Quest | Reduce `CrowdDensity` to Low and disable real-time shadows |

</details>

---

<div align="center">

Built by [Harish K](https://github.com/harryatwork) · Connect, drive, six. 🏏

</div>