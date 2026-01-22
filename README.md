🎮 Grid-Based Puzzle Prototype (Unity)

This project is a mechanics-focused Unity prototype built to explore clean, scalable, and reusable gameplay systems using only primitive meshes and native Unity components.
No external assets, no store-bought models — the entire focus is on core interaction, logic, and system design.

The project is intentionally kept visually minimal to highlight gameplay architecture over aesthetics.

✨ Core Features
🔲 Grid-Based Placement System

Fully custom grid snapping system with configurable cell size and origin

Objects snap to the center of grid cells, ensuring clean alignment

Runtime validation to prevent overlapping blocks

Supports future expansion for pathfinding, constraints, or AI logic

🖱️ Smooth 3D Drag & Drop

Mouse-based 3D dragging using a ground plane

Smooth interpolation for natural movement

Intelligent fallback:

If the target cell is occupied → block returns to its original position

If the cell is free → block snaps perfectly into place

🎯 Color-Matched Goal Mechanics

Blocks and goals share a type-based color system

Only correctly matched blocks are accepted by goals

Wrong matches are safely ignored (ready for future feedback systems like shake, push-back, or VFX)

💥 Block Consumption Feedback

Lightweight “pop” animation when a block is consumed

Clean destruction flow without physics glitches

Drag interaction is automatically disabled on consumed blocks

🧠 Level Progress Tracking

Centralized LevelManager tracks total blocks in the level

Automatically detects level completion

Designed to be easily extended with:

UI screens

Score systems

Timers or move counters

🛠️ Developer Tools & Debugging
🧩 Grid Visualization

Two visualization methods are included:

Editor Gizmos

Lightweight grid preview directly in the Scene view

Useful for fast layout iteration

Runtime LineRenderer Grid

Visible during gameplay

Customizable color, thickness, and visibility

Optimized to rebuild only when necessary

🧱 System Architecture

The project follows a modular and decoupled design:

Each responsibility is isolated (Grid, Drag, Goal, Level logic)

No hard dependencies between systems

Easy to:

Add new block types

Introduce new goal behaviors

Replace visuals without touching logic

This structure makes the prototype ideal as a foundation for puzzle, strategy, or casual games.

🚀 Why This Project Exists

This prototype was created as a learning and portfolio project, with the goal of:

Strengthening gameplay programming fundamentals

Designing clean, readable, and extensible systems

Proving that strong mechanics don’t require fancy assets

All visuals are placeholders by design.

🔮 Planned Improvements

UI for level completion

Visual & audio feedback for incorrect matches

Move limits / scoring systems

Level data loading

Touch input support (mobile-ready architecture)

🧠 Tech Stack

Unity

C#

Built-in physics & rendering

No external plugins or assets
