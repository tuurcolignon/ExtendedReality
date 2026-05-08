# AR Indoor Navigation System

## Overview
A scalable, markerless Augmented Reality indoor navigation system. The application recognizes physical environments and uses dynamic A* pathfinding to project a continuous, real-time route onto the floor, guiding users to selected destinations within a building.

## Key Features
 * **Real-Time Floor Routing:** Translates the user's spatial location into an accurate floor-level path, instantly calculating the shortest route to their chosen destination.

 * **Seamless Multi-Room Navigation:** Supports continuous guidance across large-scale environments by seamlessly stitching multiple overlapping scanned areas into a single unified tracking map.

 * **Context-Aware Guidance:** Monitors the AR tracking state in real-time. If the user points the camera away or tracking is lost, the path is instantly hidden to prevent the user from following a drifting or incorrect route.

 * **Dynamic Destination Selection:** Allows users to easily switch between points of interest (e.g., Vending Machine, Exit) via an intuitive, mobile-friendly interface.

 * **Optimized Mobile Performance:** Intelligently throttles pathfinding calculations to ensure a smooth, real-time visual experience without excessively draining the device's CPU or battery.

## Tech Stack
* **Engine:** Unity 2022 LTS
* **AR SDK:** Vuforia Engine (Area Targets)
* **AI:** Unity AI Navigation package (`NavMeshSurface`, `NavMeshAgent`)
* **Platform:** Android / iOS