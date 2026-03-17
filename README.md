# Slide & Strike - Development Readme
**Slide & Strike** is a fast-paced 3D arcade game developed in Unity. Players launch a penguin across icy landscapes, manipulating gravity and using power-ups to smash into "Petits Filous" tubes for the ultimate strike.

---

## 🐧 Project Overview
* **Genre:** Physics-based Arcade / Bowling
* **Platform:** PC (Windows)
* **Engine:** Unity 2022.3+
* **Core Loop:** Aim -> Launch -> Dodge/Collect -> Strike

---

## 🚀 Key Features
* **Impulse Launch:** Master the initial power to set your trajectory.
* **Gravity Shift:** Navigate inverted tracks by hitting specific gravity tiles.
* **Drift System:** Tighten turns and build boost levels (inspired by classic kart racers).
* **Petits Filous Strikes:** Replace boring pins with explosive yogurt tubes and iconic sound effects.
* **Dynamic Power-ups:**
    * **Chili:** Ignite the penguin to destroy obstacles and ignore snow friction.
    * **Giga-Growth:** Scale up to smash through heavy barriers.
    * **Magnet:** Pull nearby collectibles automatically.
    * **Clones:** Spawn replicas to maximize your strike zone.

---

## 🛠️ Installation & Setup
1. **Clone the repository:** `git clone https://github.com/ClementDaguenet/slide-and-strike.git`
2. **Open in Unity:** Use Unity Hub to add the project folder.
3. **LFS Check:** Ensure Git Large File Storage is installed for 3D assets.  
   `git lfs pull`

---

## 📂 Project Structure
* `Assets/Scenes/`: Project levels and individual sandbox scenes.
* `Assets/Prefabs/`: Essential game objects (Penguin, Tubes, Obstacles).
* `Assets/Scripts/`: C# logic (PlayerController, GravityManager, Inventory).
* `Assets/Settings/`: Physics materials and Input Actions.

---

## 🤝 Contribution Guidelines
* **Feature Branches:** Create a new branch for every major feature.
* **Prefab Workflow:** Never modify the `Main_Level` scene directly. Work on Prefabs or in your own sandbox scene.
* **Commit Messages:** Use clear, imperative titles (e.g., `Add: Penguin drift logic`).
* **Sync:** Pull from `main` at the start of every session.

---

## 🎮 Default Controls
| Action | Key |
| :--- | :--- |
| **Launch / Impulse** | Space / Mouse Click |
| **Steer Left/Right** | Q / D or Left/Right Arrows |
| **Drift** | Shift (Hold) |
| **Dive / Boost** | Control |
| **Reset Position** | R |

---

## 🛑 Roadmap & TODO
- [ ] Implement `Rigidbody` physics balancing (Mass vs. Friction).
- [ ] Develop the `GravityTile` trigger script.
- [ ] Integrate "Petits Filous" commercial jingle on Strike.
- [ ] Create Olympic-style trick animations for air-time.
