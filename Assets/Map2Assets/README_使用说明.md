# 地下迷宫城 Unity 素材资源包

> 全部资源均为免费可商用（CC0 / MIT 协议），无需署名。
> 整理日期：2026-08-18

---

## 一、资源包总览

| 资源包 | 大小 | 模型数 | 协议 | 格式 | 风格 |
|--------|------|--------|------|------|------|
| KayKit Dungeon Remastered | 44MB | 203 | CC0 | FBX / GLTF / OBJ | 低多边形风格化 |
| Kenney Modular Dungeon Kit | 19MB | 40 | CC0 | FBX / GLB / OBJ | 低多边形卡通 |
| Kenney Mini Dungeon | 4.6MB | 30 | CC0 | FBX / GLB / OBJ | 低多边形迷你 |
| Cawina Dungeon Asset Pack | 24MB | 1个组合FBX + blend | MIT | FBX / BLEND | 写实地牢 |
| CC0 Modular Asset Pack | 76MB | 40+ | CC0 | FBX / GLB | 工业/写实模块化 |
| 生成的交互装置 | 64KB | 5 | 免费 | OBJ | 通用低多边形 |

---

## 二、你最需要的三类资源（灯 / 门 / 开关）

### 🔥 灯 / 火把（Lighting）

| 文件路径 | 说明 |
|----------|------|
| `KayKit-Dungeon-Remastered-1.0/addons/kaykit_dungeon_remastered/Assets/fbx/torch.fbx` | 基础火把（未点燃） |
| `KayKit-Dungeon-Remastered-1.0/addons/kaykit_dungeon_remastered/Assets/fbx/torch_lit.fbx` | 点燃的火把（带火焰造型） |
| `KayKit-Dungeon-Remastered-1.0/addons/kaykit_dungeon_remastered/Assets/fbx/torch_mounted.fbx` | 壁挂式火把 |

> **Unity 使用提示**：将 `torch_lit` 或 `torch_mounted` 拖入场景后，在火焰位置添加一个 Point Light，颜色设为橙黄色 (#FF6600)，Range 约 5-8，Intensity 约 2，再搭配一个火焰粒子系统即可。

---

### 🚪 门（Doors）

| 文件路径 | 说明 |
|----------|------|
| `kenney_modular-dungeon-kit/Models/FBX format/gate-door.fbx` | **可开合的木门**（推荐） |
| `kenney_modular-dungeon-kit/Models/FBX format/gate.fbx` | 栅栏门 |
| `kenney_modular-dungeon-kit/Models/FBX format/gate-metal-bars.fbx` | 金属栅栏门（监狱风） |
| `kenney_modular-dungeon-kit/Models/FBX format/gate-door-window.fbx` | 带小窗的门 |
| `kenney_mini-dungeon/Models/FBX format/gate.fbx` | 迷你风格栅栏门 |
| `cc0_asset_pack/exports/FBX_Nanite/SM_Door_Panel.fbx` | 写实门板（可做旋转门） |
| `cc0_asset_pack/exports/FBX_Nanite/SM_Doorframe.fbx` | 写实门框 |
| `cc0_asset_pack/exports/FBX_Nanite/SM_Floor_Hatch.fbx` | 地板活板门/舱口 |
| `KayKit-Dungeon-Remastered-1.0/.../fbx/wall_doorway.fbx` | 石质门洞（门框） |
| `KayKit-Dungeon-Remastered-1.0/.../fbx/wall_doorway_sides.fbx` | 带侧柱的门洞 |

> **Unity 开门动画提示**：将门模型的 Pivot 设为门轴一侧，用 `Transform.Rotate()` 或 Animator 做开合动画。`gate-door.fbx` 最适合做可交互的木门。

---

### 🎛️ 开关 / 拉杆 / 按钮（Interactables）

这些是专门为你生成的地下迷宫交互装置，OBJ 格式可直接导入 Unity：

| 文件路径 | 说明 | 面数 |
|----------|------|------|
| `generated_interactables/wall_lever.obj` | **墙壁拉杆**（石底座+木手柄） | 72 |
| `generated_interactables/floor_button.obj` | **地面压力按钮**（石底座+金属按钮） | 132 |
| `generated_interactables/valve_wheel.obj` | **阀门转轮**（金属底座+转轮） | 306 |
| `generated_interactables/wall_switch_panel.obj` | **墙壁开关面板**（带指示灯） | 66 |
| `generated_interactables/chain_pull.obj` | **拉绳开关**（天花板安装） | 114 |

> **生成脚本**：`generated_interactables/generate_models.py`，可自行修改参数重新生成。
>
> **Unity 交互提示**：
> - 拉杆：手柄部分设为子物体，绕 X 轴旋转 45° 模拟拉下
> - 地面按钮：按钮部分沿 Y 轴下移 0.05 模拟按下
> - 阀门：转轮部分绕 Y 轴旋转 90°
> - 所有模型都带 UV 坐标，可直接贴材质

---

## 三、其他丰富的环境资产

### KayKit Dungeon Remastered（203个模型，最全面）
- **建筑结构**：各种墙、地板、楼梯、拱门、柱子、天花板
- **道具**：宝箱（多种）、酒桶、桌椅、板条箱、陷阱、篝火、铁砧
- **装饰**：旗帜（多种颜色/图案）、盾牌、骨架、蜘蛛网
- **完整文件列表**：`KayKit-Dungeon-Remastered-1.0/addons/kaykit_dungeon_remastered/Assets/fbx/`

### Kenney Modular Dungeon Kit（40个模型，模块化搭建）
- **走廊系统**：corridor（直/弯/十字/T字/宽版）
- **房间系统**：room（小/大/宽，各有变体）
- **模板件**：wall、floor、corner、stairs 等基础模块
- 适合用网格拼接快速搭建迷宫布局

### Kenney Mini Dungeon（30个模型，含角色）
- 除了建筑和道具，还包含 **human（人类）** 和 **orc（兽人）** 角色模型
- 武器：剑、矛、盾牌
- 物品：钥匙、药水、金币、罐子

### Cawina Dungeon Asset Pack（MIT协议）
- `fbx/Dungeon_Assets_CarolinaSousa.fbx`：一个包含所有地牢资产的组合 FBX
- `Dungeon_Assets_CarolinaSousa.blend`：Blender 源文件，可在 Blender 中拆分导出
- 写实风格，纹理在 `Asset_Images/` 目录

### CC0 Modular Asset Pack（工业/写实风格）
- 模块化建筑件：墙、地板、天花板、屋顶、楼梯、栏杆
- 道具：油桶、木箱、托盘、管道、通风口
- 提供 LOD 和 Nanite 两种 FBX 版本
- 纹理在 `textures/` 目录

---

## 四、Unity 导入指南

1. **导入 FBX**：直接将 `.fbx` 文件拖入 Unity Project 窗口的 Assets 文件夹
2. **导入 OBJ**：同样直接拖入，Unity 会自动识别
3. **材质设置**：
   - KayKit 和 Kenney 的模型使用共享纹理图集，导入时在 Import Settings → Materials 中提取材质
   - 生成的交互装置（OBJ）需要自行创建材质并赋值
4. **碰撞体**：导入后在 Import Settings → Model 中勾选 "Generate Colliders"，或手动添加 Mesh Collider
5. **灯光**：火把模型本身不含光源，需手动添加 Point Light 子物体

---

## 五、推荐使用组合

**快速搭建地下迷宫场景**：
- 主体结构：Kenney Modular Dungeon Kit（走廊+房间模块化拼接）
- 细节装饰：KayKit Dungeon Remastered（火把、宝箱、酒桶、旗帜）
- 交互机关：generated_interactables（拉杆、按钮、阀门）
- 门：Kenney 的 gate-door.fbx（木门）+ gate-metal-bars.fbx（监狱门）

---

## 六、来源与许可证

| 资源 | 作者/来源 | 许可证 |
|------|-----------|--------|
| KayKit Dungeon Remastered | Kay Lousberg (kaylousberg.itch.io) | CC0 1.0 |
| Kenney Modular Dungeon Kit | Kenney (kenney.nl) | CC0 1.0 |
| Kenney Mini Dungeon | Kenney (kenney.nl) | CC0 1.0 |
| Cawina Dungeon Asset Pack | Cawina-exe (github.com/Cawina-exe) | MIT |
| CC0 Modular Asset Pack | JaronKBragg7337 (github.com) | CC0 1.0 |
| 生成的交互装置 | 本工具生成 | 免费使用 |

所有资源均可用于个人和商业项目，CC0 资源无需署名，MIT 资源需保留版权声明。
