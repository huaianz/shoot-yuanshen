# 原神风格 UI 素材资源包

> ⚠️ **重要版权声明**：本资源包中的大部分 UI 图片提取自《原神》游戏文件，版权归 **miHoYo / HoYoverse** 所有。仓库虽标注 GPL-3.0 协议，但这是代码/整理协议，**不代表游戏素材本身可自由商用**。建议仅用于**个人学习、非商业项目、同人创作**。商业使用请联系米哈游获得官方授权。

---

## 一、资源包总览

| 目录 | 大小 | 内容 | 来源 |
|------|------|------|------|
| `00_精选核心UI/` | 175MB | 2177个精选UI，按11个类别整理 | PathOfGenshin/resources |
| `01_原神图标SVG/` | 200KB | 31个SVG矢量图标（元素/属性/圣遗物） | Sacr3d/genshin-icons |
| `02_Unity抽卡项目/` | 17MB | Unity 6 URP 抽卡系统克隆，含角色头像/立绘 | arturguitelar/banner-genshin-clone |
| `03_完整UI素材库/` | ~1.5GB | 8530个原神UI PNG（完整未筛选） | PathOfGenshin/resources |

---

## 二、00_精选核心UI（推荐优先使用）

按类别整理的最常用 UI 元素，可直接导入 Unity 使用。

### 01_按钮图标（143个）
通用功能按钮图标，包括：
- 关闭、返回、箭头、背包、聊天、设置、相机、地图
- 活动入口、战斗通行证、图鉴、角色数据、语音
- 气候图标（寒冷、炎热、雾、沙暴等）
- 关键文件：`UI_BtnIcon_Bag.png`、`UI_BtnIcon_Close.png`、`UI_BtnIcon_Arrow_L.png`

### 02_背包标签（10个）
背包界面的分类标签图标：
- `UI_BagTabIcon_Avatar.png` — 角色
- `UI_BagTabIcon_Weapon.png` — 武器
- `UI_BagTabIcon_Equip.png` — 圣遗物
- `UI_BagTagIcon_Food.png` — 食物
- `UI_BagTabIcon_Material.png` — 材料
- `UI_BagTabIcon_Consume.png` — 消耗品
- `UI_BagTabIcon_Quest.png` — 任务道具
- `UI_BagTabIcon_Packet.png` — 贵重道具
- `UI_BagTabIcon_Other.png` — 其他
- `UI_BagTabIcon_Homeworld.png` — 尘歌壶

### 03_框架边框（636个）
各种UI边框、框架、外框素材，用于搭建界面。
- `UI_Frame_04.png`、`UI_Frame_06.png`、`UI_Frame_08.png` 等通用框架
- `UI_Frm_*` 系列活动特定框架

### 04_背景面板（280个）
界面背景、面板底图素材。

### 05_角色头像（254个）
原神全角色头像图标，可用于角色选择、队伍界面等。

### 06_装备武器图标（312个）
武器、圣遗物、道具图标。

### 07_Buff状态图标（71个）
战斗中的Buff/Debuff状态图标，包括：
- 攻击提升、暴击率、暴击伤害、元素伤害加成
- 冷却缩减、能量回复、治疗加成、护盾强效

### 08_抽卡UI（393个）
祈愿/抽卡界面相关UI素材。

### 09_对话框（20个）
任务对话、奖励对话框背景和框架。

### 10_设置齿轮（44个）
设置界面相关的齿轮、背景、图标。

### 11_血条体力条（14个）
战斗HUD血条素材：
- `UI_HPBar_0.png` — 普通敌人血条
- `UI_HPBar_0_Elite.png` — 精英敌人血条
- `UI_HPBar_0_Outline.png` — 血条外框
- `UI_HPBar_1.png`、`UI_HPBar_2.png` — 不同等级血条
- `UI_Frm_BossHPBar_Grid_Bg.png` — Boss血条背景
- `UI_Frm_BossHPBar_Grid_Fill.png` — Boss血条填充

---

## 三、01_原神图标SVG（矢量图标）

31个粉丝重绘的SVG矢量图标，可无损缩放：

| 类别 | 文件 |
|------|------|
| 元素（7种） | elementAmeno.svg(风), elementCryo.svg(冰), elementDendro.svg(草), elementElectro.svg(雷), elementGeo.svg(岩), elementHydro.svg(水), elementPyro.svg(火) |
| 基础属性 | baseATK.svg, baseDEF.svg, baseMaxHP.svg, baseElementalMastery.svg, baseMaxStamina.svg |
| 进阶属性 | advancedCritRate.svg, advancedCDReduction.svg, advancedEnergyRecharge.svg, advancedHealingBonus.svg, advancedShieldStrength.svg |
| 伤害加成 | dmgPhysicalDmgBonus.svg |
| 圣遗物（5件） | artifactFlower.svg(生之花), artifactPlume.svg(死之羽), artifactSands.svg(时之沙), artifactGoblet.svg(空之杯), artifactCirclet.svg(理之冠) |

> SVG格式需要在Unity中安装SVG Importer插件，或提前转成PNG使用。

---

## 四、02_Unity抽卡项目

完整的Unity 6 URP项目，克隆了原神的抽卡系统：
- 角色头像：`Assets/_Game/Art/Sprites/Characters/Icons/`（25+角色）
- 角色立绘：`Assets/_Game/Art/Sprites/Characters/Splash/`
- 抽卡界面参考图：`Assets/_Game/Art/Refs/`

可直接用Unity打开学习，或提取其中的图片素材。

---

## 五、03_完整UI素材库

8530个原神UI PNG文件，未筛选的完整资源库，路径：`resources/gi/Sprite/`
包含所有活动UI、七圣召唤UI、过场动画UI等。
文件命名规则：`UI_<类别>_<具体名称>.png`

---

## 六、Unity 使用指南

### 导入图片
1. 将需要的PNG文件拖入Unity Project窗口的Assets文件夹
2. 选中图片，在Inspector中设置：
   - **Texture Type**：Sprite (2D and UI)
   - **Sprite Mode**：Single（单图）或 Multiple（图集）
   - **Pixels Per Unit**：根据需要调整（UI通常用100）
   - **Mesh Type**：Full Rect
   - 勾选 **Generate Mip Maps** 取消（UI不需要Mipmap）

### 搭建血条
1. 导入 `UI_HPBar_0_Outline.png`（外框）和 `UI_HPBar_0.png`（填充）
2. 在Canvas下创建Image作为背景
3. 创建子Image作为填充，设置Image Type为Filled，Fill Method为Horizontal
4. 通过脚本控制fillAmount显示血量

### 搭建背包界面
1. 使用 `UI_Frame_*` 作为外框
2. 使用 `UI_Bg_*` 作为背景面板
3. 使用 `UI_BagTabIcon_*` 作为分类标签
4. 使用 `UI_BtnIcon_*` 作为功能按钮

### SVG图标转PNG
如果需要使用SVG图标，可在命令行用ImageMagick转换：
```bash
convert -background none -resize 256x256 input.svg output.png
```

---

## 七、版权与使用建议

| 资源 | 版权状态 | 个人非商用 | 商业用途 |
|------|----------|-----------|---------|
| 游戏提取UI (PathOfGenshin) | 米哈游版权 | ✅ 可学习参考 | ❌ 需官方授权 |
| SVG重绘图标 (Sacr3d) | 粉丝重绘，无明确协议 | ✅ 建议署名 | ⚠️ 需确认 |
| Unity抽卡项目 | 代码MIT，图片米哈游版权 | ✅ 可学习 | ❌ 图片需授权 |

**安全替代方案**：
- 如需商业项目，建议使用原创或明确CC0协议的奇幻风格UI素材
- itch.io上有大量免费奇幻RPG UI Kit（搜索"fantasy UI kit free"）
- 可参考原神的设计风格（金色边框、深蓝渐变、圆润按钮），自己制作原创UI

---

## 八、来源链接

- PathOfGenshin/resources: https://github.com/PathOfGenshin/resources
- Sacr3d/genshin-icons: https://github.com/Sacr3d/genshin-icons
- arturguitelar/banner-genshin-clone: https://github.com/arturguitelar/banner-genshin-clone
