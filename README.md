# RealChute 简体中文运行时汉化插件

给 [RealChute](https://spacedock.info/mod/1364)（作者 stupid_chris）补上简体中文界面。
对应 RealChute v1.4.9.5 / KSP 1.12.5。

## 为什么需要插件

RealChute 的 cfg 部分（预设名、材料说明、VAB 分类）可以直接用 ModuleManager 汉化，
但**界面文字改不到**：

RealChute.dll **完全没有使用 KSP 的 `#LOC_` 本地化系统**（DLL 内 0 处 `#LOC`），它的界面文字分两类：

| 类型 | 存放位置 | 例子 |
|---|---|---|
| `[KSPEvent]` / `[KSPField]` 的 `guiName` | 程序集**元数据特性 blob**（不是 `ldstr`） | `Arm parachute`、`Deploy chute` |
| 窗口 / 对话框字符串字面量 | IL 里的 `ldstr` | `Deployment altitude`、`Wanted deceleration` |

两类都在 DLL 里，cfg 覆盖不了，所以只能用插件在运行时改。

## 实现方式

| 手段 | 目标 | 覆盖 |
|---|---|---|
| Harmony **Postfix** | `RealChuteModule.OnStart`、`ProceduralChute.OnStart` → 遍历 `Events`/`Fields` 重写 `guiName` | 16 条按钮/字段名 |
| Harmony **Transpiler** | 全程序集方法，把命中的 `ldstr` 操作数整段替换为中文 | 188 条字典项 |

Transpiler **只替换字典里明确列出的字符串**，其余一律不动，因此不会影响任何标识符、配置键或查找键
（如 `Main chute`、`caseName`、`sizeID`）。替换是「同一条指令换操作数」，不改变 IL 长度与跳转偏移。

## 离线验证结果

| 检查项 | 结果 |
|---|---|
| 编译 | 通过（csc 4.0，`/target:library /optimize+`） |
| 字典键是否真实存在于目标 DLL | Literals 188/188、GuiNames 16/16 全部命中（逐一做过字节级比对） |
| `string.Format` 占位符一致性 | 21 条含 `{0}` 的条目两侧占位符数量全部一致 |
| 模拟 IL 扫描 | 命中 35 个方法 / 170 条字符串 |
| 命中的关键类 | `EditorGUI`、`TemplateGUI`、`SettingsWindow`、`Parachute`、`ProceduralChute`、`RealChuteModule`、`RCToolbarManager`、`CompatibilityChecker` |
| 两个 PartModule 的 OnStart | 均存在且已挂钩 |

**注意**：以上都是离线验证，**没有进游戏实测过**。

## 依赖

- RealChute（本体）
- [Harmony](https://github.com/KSPModdingLibs/HarmonyKSP)（`000_Harmony`，2.x）

## 安装

把 `GameData\RealChute_CN` 放进 KSP 的 `GameData` 目录。

## 进游戏后怎么确认生效

`KSP.log` 里会出现这一行：

```
[RealChuteCN] 汉化完成：字面量方法 35 个，界面模块 2 个。
```

然后看三处：

1. VAB 里右键 RealChute 零件 → 按钮应为「预位降落伞 / 解除预位 / 展开降落伞 / 切断主伞 / 重新打包 / 打开编辑器」
2. 打开降落伞编辑器 → 字段名应为「展开高度 / 完全展开直径 / 预展开气压 / 期望减速度 …」
3. 设置窗口（工具栏图标）→ 应为「分级时自动预位 / 触地时自动展开 / 重新打包所需最低工程师等级」

没生效就把 `KSP.log` 里含 `RealChuteCN` 的行发出来。

## 重新编译

双击 `src\RealChute_CN\build.cmd`。它用系统自带的 csc 编译，并自动复制到 `GameData\RealChute_CN\`。

要加词条，编辑 `RealChute_CN.cs` 里的 `GuiNames` / `Literals` 两个字典即可
（键必须是目标 DLL 里**原样**的英文串）。

## 卸载

删掉 `GameData\RealChute_CN` 整个目录即可。插件不写任何存档数据。

## 授权

MIT。本仓库不包含 RealChute 本体的任何文件。

---

# RealChute CN

A runtime Chinese localization plugin for [RealChute](https://spacedock.info/mod/1364) by stupid_chris.
RealChute does not use KSP's `#LOC_` localization system at all, so its UI strings (KSPEvent/KSPField
`guiName` attributes and `ldstr` literals) cannot be reached with a config patch. This plugin rewrites
them at runtime with Harmony: a Postfix on the two PartModules' `OnStart` for the attribute-based
names, and a Transpiler that swaps a fixed dictionary of string literals.

Requires RealChute and Harmony. Licensed MIT. Contains no files from the original mod.
