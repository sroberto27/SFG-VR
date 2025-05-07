
# SFG-VR: Distributed VR Rendering System

**SFG-VR** is a distributed VR rendering prototype designed to split the rendering workload across multiple machines. It captures the VR user's point of view from a Meta Quest (master) and transmits camera position and orientation to two slave PCs. These PCs render separate halves of the scene, stream the results back, and the master stitches and displays the final view in VR.

This project is an **extension and adaptation of the SFG architecture** described in the paper:

**[ATC'24] High-density Mobile Cloud Gaming on Edge SoC Clusters**  
Read their original paper for more technical insights: https://www.usenix.org/conference/atc24/presentation/zhang-li-gaming  
Their repository: https://github.com/lizhang20/SFG

---

## 🧩 System Architecture

- **Master (Meta Quest)**:
  - Sends position and orientation of the VR headset.
  - Receives images from two slave PCs via TCP.
  - Reconstructs a full panoramic or stereo image using the received left/right view textures.
  
- **Left and Right Slaves (Windows PCs)**:
  - Receive camera parameters and rendering scissor regions via UDP.
  - Render respective scene segments.
  - Stream textures back to the master.

---

## 🗂️ Core Components

### 🎮 `MasterVRSender.cs`
- Sends the current VR headset's position, rotation, and the scissor region for left/right partitioning via UDP.
- Uses Unity’s `XRSettings.gameViewRenderMode = OcclusionMesh` for optimized stereo rendering.

### 🖼 `MasterVRReceiver.cs`
- Listens for TCP image streams from slave devices.
- Identifies sender IP (left/right) and stores each image in a texture.
- Reconstructs a single stitched texture using `SetPixels()` and displays it on a Unity `Renderer`.

### 🖥 `SlaveVRReceiver.cs`
- Receives camera transform and viewport scissor rectangle via UDP.
- Applies it to a local camera.
- Captures the appropriate region of a `RenderTexture`, encodes, and sends it via TCP to the master.

### ⚙️ `NetworkConfig.cs`
- Holds runtime-configurable IP addresses for master, left slave, and right slave.
- Marked with `DontDestroyOnLoad()` so it's persistent across scenes.
- Used by all connection-related scripts.

### 🖱 `ConnectButtonHandler.cs`
- UI-linked script for live updating the IPs of all network devices.
- Reloads the active Unity scene with the new network configuration.

---

## 🧪 Requirements

- Unity 2022.3+ with XR Plugin Management and OpenXR
- Meta Quest (tested with Quest Pro)
- Two networked Windows PCs with GPU
- All devices must be on the same LAN

---

## 🛠 Setup Instructions

1. Clone the repo:
   ```bash
   git clone https://github.com/sroberto27/SFG-VR.git
   ```
2. Open the project in Unity.
3. Build the following scenes separately:
   - **Master Scene** for Meta Quest.
   - **LeftSlave Scene** for PC #1.
   - **RightSlave Scene** for PC #2.
4. Use the in-scene UI to enter IP addresses for the Master, Left Slave, and Right Slave. Press **Connect**.
5. Launch executables in this order:
   - Start **Master** first (on the Quest).
   - Then run **Left** and **Right** slave builds on their respective PCs.

---

## 📸 Features

- Real-time image stitching from two remote slave renderers.
- Automatic reconnection and live reconfiguration of network settings via UI.
- Half-resolution scissor rendering on each slave to reduce load.
- Occlusion mesh enabled for efficient stereo display on Quest.

---

## ⚠️ Current Limitations

📉 **Reduced Resolution for Testing**  
Image resolution is currently capped at **512×512** for bandwidth and debugging purposes.

🎮 **Limited VR Interaction**  
Only basic headset **movement tracking** is functional. XR interactions (e.g., grabbing, UI interaction) are **not yet supported** and are planned for future work.

🧠 **Command vs. VR Game Architecture Conflict**  
The system currently focuses on camera-based streaming, which complicates interaction integration typical in XR games.

🎨 **Visual Quality**  
Rendering may result in artifacts or **distortion**, especially near the seams of the stitched image.

🤢 **Motion Sickness Risk**  
Frame rate variation and non-native rendering can cause **discomfort or virtual sickness** for sensitive users.

🚀 **Performance Optimization Needed**  
Streaming, compression, and reconstruction are still **unoptimized** and may not run efficiently on low-end hardware.

---

## 📁 Directory Structure

```
/Assets/
├── RobertoScripts/
│   ├── MasterVRSender.cs
│   ├── MasterVRReceiver.cs
│   ├── SlaveVRReceiver.cs
│   ├── NetworkConfig.cs
│   └── ConnectButtonHandler.cs
├── Scenes/
│   ├── MasterScene.unity
│   ├── LeftSlaveScene.unity
│   └── RightSlaveScene.unity
```

---

## 🧠 Author

**Roberto Salazar**  
GitHub: [@sroberto27](https://github.com/sroberto27)

---

## 🙌 Contributions

Pull requests are welcome. Please open issues for bug reports, suggestions, or improvements.
