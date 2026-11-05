# Pocket Putt Arena

A stylized 3D mini-golf game built with **Unity 6.3 LTS** and published as a browser-playable WebGL game.

**Play the game:** https://pocket-putt-arena.vercel.app  
**Source code:** https://github.com/Parinith86604/Pocket-Putt-Arena

## Overview

Pocket Putt Arena features ten themed mini-golf courses, course difficulty tiers, stroke and par information, course selection, and hole-completion options. The interface adapts to different screen sizes: desktop layouts keep the course list visible, while narrow and portrait layouts use a compact **COURSES** menu to keep the gameplay and completion controls accessible.

## Courses

| # | Course | Difficulty |
|---:|---|---|
| 01 | Evergreen Valley | Medium |
| 02 | Copper Canyon | Medium |
| 03 | Frostlight Ridge | Medium |
| 04 | Jade Temple Garden | Difficult |
| 05 | Azure Coast | Difficult |
| 06 | Volcanic Ember Run | Difficult |
| 07 | Moonlit Crater | More Difficult |
| 08 | Redwood Rapids | More Difficult |
| 09 | Sky Harbor | More Difficult |
| 10 | Crystal Caverns | More Difficult |

## Play online

Open the live WebGL game in a modern desktop or mobile browser:

**https://pocket-putt-arena.vercel.app**

The website hosts the compiled WebGL build. The Unity source repository is maintained separately; the generated WebGL build output is not committed to this repository.

## Open the Unity project

### Requirements

- Unity **6000.3.27f2** (Unity 6.3 LTS)
- Unity Hub
- Git, if cloning the repository

### Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/Parinith86604/Pocket-Putt-Arena.git
   ```
2. In Unity Hub, choose **Add** or **Open** and select the cloned project folder.
3. Open the project with Unity **6000.3.27f2**.
4. Allow Unity to finish importing assets.
5. Open the main scene:
   `Assets/Scenes/GolfCourse01.unity`
6. Press **Play** in the Unity Editor to run the game.

## Build a WebGL version

1. Open the project in Unity **6000.3.27f2**.
2. Open **File → Build Profiles**.
3. Select the **Web** platform and make sure `Assets/Scenes/GolfCourse01.unity` is included in the scene list.
4. For a release build, leave **Development Build** disabled.
5. Choose the required Web code optimization setting, then click **Build**.
6. Select an output folder outside the Unity project's source folders.
7. Test the generated `index.html` build locally or through a web host.

### Deployment

The live site is deployed to the existing Vercel project **pocket-putt-arena**. To publish a new version, build WebGL in Unity first, then deploy the **contents of the generated WebGL output folder** to that existing Vercel project. Do not deploy the Unity source repository as though it were a ready-made website.

## Repository structure

- `Assets/` — scenes, scripts, materials, models, textures, and other game assets
- `Packages/` — Unity package manifest and package configuration
- `ProjectSettings/` — Unity project and editor settings
- `README.md` — project overview and setup instructions

## Current project status

- Unity source project is maintained in this repository.
- Ten-course selector and responsive course-menu behavior are included in the source.
- A WebGL build is published at the live game link above.
- The desktop and mobile layouts have been tested in the current published version.

---

Built with Unity.
