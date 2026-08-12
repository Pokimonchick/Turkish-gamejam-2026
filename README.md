# In Search Of

**In Search Of** is a 2D adventure puzzle game set in a world inspired by Turkic mythology and culture.

The player follows **Aisulu** through a world presented as a living illustrated book. Progress is based on interacting with the environment and **folding parts of the game field like paper**, changing the layout of the level to create paths, overcome obstacles, and continue the story.

The project was originally created during **Turkish Game Jam 2026** and is currently presented as an MVP.

## 🎮 Play the Game

Playable WebGL build:

https://panini-entertainment.itch.io/in-search-of

## 🧩 Core Gameplay

The main puzzle mechanic allows the player to fold sections of the level, changing the position and accessibility of different parts of the map.

The mechanic is used to:

* create new paths
* connect previously separated areas
* overcome environmental obstacles
* solve spatial puzzles
* progress through the story

The goal is to combine puzzle mechanics with narrative and visual storytelling inspired by Turkic mythology.

## 💻 Source Code

The project is developed in **Unity using C#**.

The main gameplay scripts are located in:

```text
Assets/Scripts/
```

The source code for the core folding puzzle mechanic can be found here:

```text
Assets/Scripts/BookPuzzle/OrigamiFold/
```

This directory contains the scripts responsible for the folding system, puzzle state, player interaction and movement related to the mechanic.

Some of the main components include:

```text
OrigamiFoldBoard.cs
OrigamiFoldDragController.cs
OrigamiFoldMoveAction.cs
OrigamiFoldPlayerMover.cs
OrigamiFoldPuzzleState.cs
```

## 🛠 Technologies

* Unity
* C#
* WebGL
* Git / GitHub

## 🚀 Running the Project

1. Clone the repository:

```bash
git clone https://github.com/Pokimonchick/Turkish-gamejam-2026.git
```

2. Open the project through **Unity Hub**

3. Allow Unity to import the project assets and packages

4. Open the main game scene

5. Enter **Play Mode**

## 📁 Project Structure

```text
Assets/
├── Scripts/            # C# gameplay code
│   └── BookPuzzle/
│       └── OrigamiFold/ # Core folding mechanic
├── Scenes/             # Game scenes
├── Art/                # Visual assets
└── ...

Packages/               # Unity package configuration
ProjectSettings/        # Unity project settings
```

## 👥 Team

The project was created by a multidisciplinary team during Turkish Game Jam 2026. Responsibilities overlapped during development, with team members contributing across game design, programming, art, narrative and audio.

Core areas of work included:

* Game Design
* Programming
* 2D Art
* Narrative Design
* Sound Design

## 🌱 Project Direction

The MVP demonstrates the core folding mechanic, visual direction and narrative concept.

Further development may include:

* additional levels and puzzles
* expanded story and character interactions
* illustrated cutscenes
* improved visual and gameplay polish
* additional stories using the same core mechanics
* deeper exploration of Turkic mythology and culture

## 📌 Project Status

**MVP / Prototype**

The repository contains the working source code used to build the current playable version of the project.
