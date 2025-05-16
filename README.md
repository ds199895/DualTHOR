# DualTHOR
We have built a lightweight simulation environment based on [AI2-THOR](https://ai2thor.allenai.org/) that runs on the web for training and evaluation of embodied agents and multimodal models. In addition to AI2-THOR, we have added the following features:
- Dual-arm action execution: Now supports parallel, asynchronous task execution with both arms.
- Task rollback: Supports rolling back to any time step during task execution, improving data synthesis efficiency.
- Action probability system: This version adds a probability system that can customize success rates for individual actions according to config.json.
- More realistic action execution: Added IK library to implement actions, replacing direct attachment transfer, allowing for more detailed simulation of action execution time.
- More realistic state changes: Added more detailed tuning for states such as water filling and cooking.


## Installation
### 1. Clone the Project

### 2. Setup Configuration
---

#### 1. Python Environment
Create a virtual environment with Python version `3.10.15`.
```
conda create -n playground python=3.10.15
conda activate playground
```
---

#### 2. Install Required Libraries

Basic dependency installation

```bash
conda create -n playground python=3.10.15
conda activate playground
cd python
pip install -r requirements.txt
```

---
IK(inverse kinematics)
```bash
conda install pinocchio -c conda-forge
pip install meshcat
pip install casadi
```

---
Unitree DDS (unitree_dds_wrapper)

```bash
# Install the Python version of the unitree_dds_wrapper.
git clone https://github.com/unitreerobotics/unitree_dds_wrapper.git
cd unitree_dds_wrapper/python
pip install -e .
```



#### 3. Start Agent Server

```bash
python main.py
```
![alt text](image/img_v3_launcher.gif)

## Operation Methods

### 1. Python Side:
Python is mainly used to control the Agent in the virtual environment to perform navigation, interaction, control and other tasks, and to obtain perception data from the environment. The main scripts are:
   1. main.py: Main program entry, including launcher, tcp_server, server_ik; simply start this file directly.
   2. controller.py: Controller program, responsible for interacting with the Unity environment, starting the server, calling action methods, sending and providing feedback.
   3. config.json: Configuration file, currently only contains success rate; more parameters can be added as needed.
   4. actions: Action scripts containing all action methods.
   Two control modes: user command line input control and controller.step calling action control.
      - Move includes `MoveAhead`, `MoveRight`, `MoveBack`, `MoveLeft`, command input can be uppercase or lowercase. Followed by parameters, the first parameter is Magnitude, default is 1. For example, `MoveAhead Magnitude=1` means move forward 1 unit by default, `MoveAhead Magnitude=2` means move forward 2 units.
      ![alt text](image/img_v3_move.gif)
      - Rotate includes `RotateRight`, `RotateLeft`, command input can be uppercase or lowercase. Followed by parameters, the first parameter is rotation direction, default is 90°; the second parameter is success rate, default is 1. For example, `RotateRight Magnitude=1` means rotate right 90° by default, `RotateRight Magnitude=2` means rotate right 180°.
      ![alt text](image/img_v3_rotate.gif)
      - Pick and Place are for grabbing and placing objects (need to teleport to the object first), the first parameter is arm selection (Left\Right) (currently defaulting to both arms, pending modification), the second parameter is the ObjectId of the interactive item. Details of ObjectId are introduced in the table. For example, `tp objectID=Kitchen_Cup_01` `pick arm=left objectID=Kitchen_Cup_01` means teleport to the Cup first, then grab the Cup with the left arm.
      ![alt text](image/img_v3_pick.gif)
      - Toggle and Open followed by ObjectId (need to teleport to the object first), currently defaults to right hand operation, pending modification. For example, `tp Kitchen_Faucet_01` `toggle Kitchen_Faucet_01` means teleport to the faucet and turn on the faucet.
      ![alt text](image/img_v3_toggle.gif)
      - TP means teleport, with only one parameter, ObjectId, which teleports to the vicinity of that item.
      - Undo and Redo are for state history management, Redo can roll back to the previous state information, while Undo is to cancel.
      - LoadState is for specified rollback, followed by the specified state id. For example, `loadstate stateID=1` means roll back to the first state.
      ![alt text](image/img_v3_loadstate.gif)
      - Resetjoint resets the mechanical arm joint angle, restoring to the initial joint angle, followed by parameter (arm=Left\Right). For example, `resetjoint arm=left`.
      ![alt text](image/img_v3_resetjoint.gif)

#### Feedback System: After the action is completed, all state information will be automatically returned as feedback, including the robot and items.

### 2. Unity Side:
  - Need to confirm IP and PORT in advance to ensure normal connection, currently defaulting to localhost and 5678.
  - Press Z to enter robot control mode, WSAD to control robot movement, mouse to control rotation, center the screen on interactive items to interact. For example, left click to Pick, left click again to Place; aim at the faucet to turn the water on and off, aim at the refrigerator to open and close the door, etc. See [Interaction Table](#interaction-table) for details.
  - Press P key to save all camera images, including first and third person camera screenshots saved to the local directory.
  - Press the number 0 key to enter free camera mode, separating from the whole.


## Visual System
1. The scene has multiple cameras, including first-person and third-person front, back, left, and right views, to provide 360° panoramic view with no blind spots for image information acquisition, supporting free rotation in all directions, ensuring the robot can monitor the surrounding environment in real time, and the number and direction of views can be customized according to requirements. (Showing all views would be too performance-intensive, so how to display views is still to be discussed)
![alt text](image/camera.png)
2. Provides depth map acquisition within the camera's field of view.
3. Features a mini-map that constantly relates the robot's position in the scene, helping players quickly understand the overall environmental layout.
4. Saves image data from each view as PNG files with UUIDs in the local directory, and can later convert image data to binary data for transmission.


## Item Interaction System
### 1. Item Classification
Objects are divided into three categories: Static/Moveable/Can pickup.
1. Static: Objects that cannot be moved in the scene. Such as switches, faucets.
2. Moveable: Objects that can be moved in the scene but cannot be picked up. Such as coffee machines.
3. Can Pickup: Objects that can be picked up in the scene. Such as potatoes, mugs.
### 2. Interactive States
A total of eight interaction states are set:
1. **Break**: Items can be broken. For example, a cup falling from a height and hitting the ground with enough force will break.
2. **Can PickUp**: Items can be picked up. Only items categorized as Can Pickup can be picked up.
3. **Contains**: Items can serve as containers for other objects. You can get the ID of child items contained in a parent container, or get the ID of the parent container containing child items.
4. **Cook**: Items can be cooked. For example, when a potato in a pot is heated to a certain temperature with the gas turned on, the potato will be cooked.
5. **Fill**: Items can be filled with liquid. For example, a cup can be filled with water or coffee. When the cup is tilted 90°, the filled liquid will disappear.
6. **Open**: Items can be opened. Such as opening a refrigerator door or a drawer.
7. **Slice**: Items can be sliced. Such as slicing a potato into many pieces. Sliced items with the Cook property can also be cooked.
8. **ToggleOnOff**: Items can be toggled. Such as faucet switches, coffee machine switches.
9. **UsedUp**: Items can be used up. Such as toilet paper can be depleted.
### 3. Interaction Table
This table lists all currently interactive item types, item ID naming (Room_ItemType_ID), location, interactive states, and notes.
| Item Type   |Item ID Format| Room | Interactive States | Notes |
|:-: |:-:|:-:|:-:|:-:|
| Cabinet   |Kitchen_Cabinet_ID| Kitchen   | Contains,Open  | |
| CoffeeMachine   |Kitchen_CoffeeMachine_ID|  Kitchen  | Contains,ToggleOnOff   |    |
| Drawer   |Kitchen_Drawer_ID| Kitchen   | Contains,Open   |    |
| Faucet   |Kitchen_Faucet_ID| Kitchen   | ToggleOnOff   |    |
| Fridge   |Kitchen_Fridge_ID| Kitchen   | Contains,Open  |    |
| Mug   |Kitchen_Mug_ID| Kitchen   | Break,Can pickup,Fill   |    |
| Pan   |Kitchen_Pan_ID| Kitchen   | Can pickup,Contains   |   |
| PaperTowerRoll   |Kitchen_PaperTowerRoll_ID| Kitchen   | Can pickup,UsedUp   |   |
| Potato   |Kitchen_Potato_ID| Kitchen   | Can pickup,Cook,Slice   | Potatoes can be cooked, or sliced and then cooked  |
| StoveKnob   |Kitchen_StoveKnob_ID| Kitchen   | ToggleOnOff   |  Gas stove switch  |
| Cabinet   |Kitchen_Cabinet_ID| Kitchen   | Contains,Open  | |
| CoffeeMachine   |Kitchen_CoffeeMachine_ID|  Kitchen  | Contains,ToggleOnOff   |    |
| Drawer   |Kitchen_Drawer_ID| Kitchen   | Contains,Open   |    |
| Faucet   |Kitchen_Faucet_ID| Kitchen   | ToggleOnOff   |    |
| Fridge   |Kitchen_Fridge_ID| Kitchen   | Contains,Open  |    |
| Mug   |Kitchen_Mug_ID| Kitchen   | Break,Can pickup,Fill   |    |
| Pan   |Kitchen_Pan_ID| Kitchen   | Can pickup,Contains   |   |
| PaperTowerRoll   |Kitchen_PaperTowerRoll_ID| Kitchen   | Can pickup,UsedUp   |   |
| Potato   |Kitchen_Potato_ID| Kitchen   | Can pickup,Cook,Slice   | Potatoes can be cooked, or sliced and then cooked  |
| StoveKnob   |Kitchen_StoveKnob_ID| Kitchen   | ToggleOnOff   |  Gas stove switch  |


