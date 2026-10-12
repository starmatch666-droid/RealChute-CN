// RealChute_CN — RealChute 简体中文运行时汉化插件
// 作者：为 STARM 定制
//
// RealChute.dll 完全没有使用 KSP 的 #LOC 本地化系统，界面文字分两类：
//   1) [KSPEvent]/[KSPField] 的 guiName —— 存在程序集元数据里，运行时才被 KSP 读取；
//   2) 窗口/对话框里的字符串字面量（ldstr）。
// 因此本插件用两种手段分别处理：
//   A. Harmony Postfix 挂在 RealChute 各 PartModule 的 OnStart 上，重写 Events/Fields 的 guiName；
//   B. Harmony Transpiler 把目标方法里命中的 ldstr 常量整段替换成中文。
//
// 只替换字典里明确列出的字符串，其余一律不动，故不会影响任何标识符/查找键。

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace RealChuteCN
{
    internal static class Patcher
    {
        internal const string HarmonyId = "cn.starm.realchutecn";

        private static bool patched;
        private static bool reported;

        // ------------------------------------------------------------------
        // A. 特性里的 guiName（右键菜单按钮、字段标签）
        // ------------------------------------------------------------------
        private static readonly Dictionary<string, string> GuiNames = new Dictionary<string, string>
        {
            { "Arm parachute", "预位降落伞" },
            { "Arm parachuteS", "预位降落伞" },
            { "Disarm parachute", "解除预位" },
            { "Disarm parachuteS", "解除预位" },
            { "Deploy chute", "展开降落伞" },
            { "Deploy Chute", "展开降落伞" },
            { "Deploy ChuteS", "展开降落伞" },
            { "Cut main chute", "切断主伞" },
            { "Cut main chuteS", "切断主伞" },
            { "Repack chute", "重新打包" },
            { "Repack chuteS", "重新打包" },
            { "Show Parachute Editor", "打开编辑器" },
            { "Hide Parachute Editor", "关闭编辑器" },
            { "Toggle info", "开关信息" },
            { "Is Safe to Deploy?", "可安全展开？" },
            { "Spare chutes.", "备用伞" },
        };

        // ------------------------------------------------------------------
        // B. 代码里的字符串字面量（窗口标签、提示、对话框）
        // ------------------------------------------------------------------
        private static readonly Dictionary<string, string> Literals = new Dictionary<string, string>
        {
            // —— 伞具编辑器窗口 ——
            { "Altitude predeployment", "预展开海拔" },
            { "Area cost:  ", "面积成本：" },
            { "Area density:  ", "面积密度：" },
            { "Autocut altitude", "自动切断高度" },
            { "Autocut altitude (m):", "自动切断高度（米）：" },
            { "Autocut altitude: ", "自动切断高度：" },
            { "Autocut altitude: {0}m", "自动切断高度：{0}米" },
            { "Autocut altitudes: ", "自动切断高度：" },
            { "Autocut speed", "自动切断速度" },
            { "Autocut speed (m/s):", "自动切断速度（米/秒）：" },
            { "Autocut speed: ", "自动切断速度：" },
            { "Autocut speed: {0}m/s", "自动切断速度：{0}米/秒" },
            { "Calculations mode:", "计算模式：" },
            { "Case cost: ", "壳体成本：" },
            { "Case mass: ", "壳体质量：" },
            { "Case mass: {0}", "壳体质量：{0}" },
            { "Case texture:", "壳体纹理：" },
            { "Change material", "更换材料" },
            { "Choose material", "选择材料" },
            { "Chute #", "伞 #" },
            { "Chute max temperature: ", "伞面最高温度：" },
            { "Chute max temperatures: ", "伞面最高温度：" },
            { "Chute model: ", "伞型：" },
            { "Chute texture:", "伞面纹理：" },
            { "Chutes:", "伞具：" },
            { "Copy to others chutes", "复制到其他伞" },
            { "Copy to symmetry counterparts", "复制到对称部件" },
            { "Craft mass", "飞行器质量" },
            { "Current chute temperature: ", "当前伞面温度：" },
            { "Current material: ", "当前材料：" },
            { "Current vertical velocity: {0:0.0}/s", "当前垂直速度：{0:0.0} 米/秒" },
            { "Currently using ", "当前使用 " },
            { "Cycle part size", "切换部件尺寸" },
            { "Deploy on ground contact:", "触地时展开：" },
            { "Deployed diameter", "完全展开直径" },
            { "Deployed diameter (m):", "完全展开直径（米）：" },
            { "Deployed diameter: ", "完全展开直径：" },
            { "Deployed diameter: {0}m", "完全展开直径：{0}米" },
            { "Deployed diameters: ", "完全展开直径：" },
            { "Deployment altitude", "展开高度" },
            { "Deployment altitude: {0}m", "展开高度：{0}米" },
            { "Deployment altitudes: ", "展开高度：" },
            { "Deployment awaiting negative vertical velocity", "等待转为下降后展开" },
            { "Deployment in ", "展开倒计时 " },
            { "Deployment in {0:0.0}s", "展开倒计时 {0:0.0} 秒" },
            { "Deployment safety: dangerous", "展开安全性：危险" },
            { "Deployment safety: risky", "展开安全性：有风险" },
            { "Deployment safety: safe", "展开安全性：安全" },
            { "Deployment speed", "展开速度" },
            { "Deployment speed (s):", "展开速度（秒）：" },
            { "Deployment speed: ", "展开速度：" },
            { "Deployment speed: {0}s", "展开速度：{0} 秒" },
            { "Deployment speeds: ", "展开速度：" },
            { "Deployment timer", "展开计时器" },
            { "Deployment timer:", "展开计时器：" },
            { "Deployment timer: ", "展开计时器：" },
            { "Deployment timer: {0}s", "展开计时器：{0} 秒" },
            { "Deploys on ground contact: true", "触地展开：是" },
            { "Description: ", "说明：" },
            { "Description:  ", "说明：" },
            { "Drag coefficient: ", "阻力系数：" },
            { "Drag coefficient:  ", "阻力系数：" },
            { "Drag coefficient: {0:0.00}", "阻力系数：{0:0.00}" },
            { "Drag coefficients: ", "阻力系数：" },
            { "dry mass", "干质量" },
            { "wet mass", "湿质量" },
            { "Emissivity constant: ", "发射率常数：" },
            { "General:", "通用：" },
            { "Input craft mass", "输入飞行器质量" },
            { "Landing alt (m):", "着陆高度（米）：" },
            { "Landing altitude", "着陆高度" },
            { "Landing speed", "着陆速度" },
            { "Mains planned deployment alt", "主伞计划展开高度" },
            { "Mass to use (t):", "使用质量（吨）：" },
            { "Material max temperature: {0:0.#}", "材料最高温度：{0:0.#}" },
            { "Material: ", "材料：" },
            { "Max temperature: ", "最高温度：" },
            { "Minimum deployment altitude: {0}m", "最低展开高度：{0}米" },
            { "Minimum deployment clauses: ", "最低展开条件：" },
            { "Minimum deployment pressure: {0}atm", "最低展开气压：{0} 标准大气压" },
            { "Must go down to deploy:", "需下降才能展开：" },
            { "Must go downwards to deploy", "需处于下降状态才能展开" },
            { "Must go downwards to deploy: true", "需处于下降状态才能展开：是" },
            { "Next size", "下一个尺寸" },
            { "Previous size", "上一个尺寸" },
            { "Parachute count", "降落伞数量" },
            { "Parachute material", "降落伞材料" },
            { "Parachute material: {0}", "降落伞材料：{0}" },
            { "Parachute materials: ", "降落伞材料：" },
            { "Parachutes used (parachutes):", "已使用降落伞（个）：" },
            { "Part mass: ", "部件质量：" },
            { "Part name: ", "部件名称：" },
            { "Planned landing speed (m/s):", "计划着陆速度（米/秒）：" },
            { "Pre deployment speed (s):", "预展开速度（秒）：" },
            { "Predeployed diameter", "预展开直径" },
            { "Predeployed diameter (m):", "预展开直径（米）：" },
            { "Predeployed diameter: ", "预展开直径：" },
            { "Predeployed diameter: {0}m", "预展开直径：{0}米" },
            { "Predeployed diameters: ", "预展开直径：" },
            { "Predeployment altitude", "预展开高度" },
            { "Predeployment altitude (m):", "预展开高度（米）：" },
            { "Predeployment altitude: {0}m", "预展开高度：{0}米" },
            { "Predeployment pressure", "预展开气压" },
            { "Predeployment pressure (atm):", "预展开气压（标准大气压）：" },
            { "Predeployment pressure: {0}atm", "预展开气压：{0} 标准大气压" },
            { "Predeployment speed", "预展开速度" },
            { "Predeployment speed: ", "预展开速度：" },
            { "Predeployment speed: {0}s", "预展开速度：{0} 秒" },
            { "Predeployment speeds: ", "预展开速度：" },
            { "Predeployment:", "预展开：" },
            { "Pressure predeployment", "气压触发预展开" },
            { "Resulting area: ", "最终面积：" },
            { "Resulting deployed area: --- m", "最终完全展开面积：--- 平方米" },
            { "Resulting predeployed area: --- m", "最终预展开面积：--- 平方米" },
            { "Selected part: ", "已选部件：" },
            { "Spare chutes", "备用伞" },
            { "Spare chutes:", "备用伞：" },
            { "Spare chutes: ", "备用伞：" },
            { "Spare chutes: {0}", "备用伞：{0}" },
            { "Spare chutes: inf", "备用伞：无限" },
            { "Specific heat: ", "比热容：" },
            { "Switch to ", "切换到 " },
            { "Symmetry counterparts: ", "对称部件：" },
            { "Target altitude (m):", "目标高度（米）：" },
            { "Target planet:", "目标天体：" },
            { "Total part cost: ", "部件总成本：" },
            { "Total part mass: ", "部件总质量：" },
            { "Use current craft mass", "使用当前飞行器质量" },
            { "Wanted deceleration", "期望减速度" },
            { "Wanted deceleration (m/s", "期望减速度（米/秒" },
            { "Wanted speed at target alt (m/s):", "目标高度处期望速度（米/秒）：" },
            { "Wanted touchdown speed (m/s):", "期望触地速度（米/秒）：" },
            { "We don't know much about this, it might as well be made of fishnets", "我们对这种材料了解不多，说不定就是渔网做的" },
            { "Manual", "手动" },
            { "Automatic", "自动" },
            { "Cancel", "取消" },
            { "Close", "关闭" },
            { "Comments", "备注" },
            { "Error", "错误" },
            { "Error!", "错误！" },
            { "Warning", "警告" },
            { "Success", "成功" },

            // —— 预设与设置 ——
            { "Save as preset", "保存为预设" },
            { "Save as preset...", "保存为预设……" },
            { "Select a preset", "选择一个预设" },
            { "Select preset", "选择预设" },
            { "Delete preset", "删除预设" },
            { "Preset description", "预设说明" },
            { "Preset name:", "预设名称：" },
            { "Preset name cannot be empty!", "预设名称不能为空！" },
            { "Preset saved", "预设已保存" },
            { "No saved presets", "没有已保存的预设" },
            { "Are you sure you wish to delete this preset?", "确定要删除这个预设吗？" },
            { "Warning: there is already a preset saved under this name. Are you sure you wish to proceed?", "警告：该名称下已有保存的预设。确定要继续吗？" },
            { "Apply settings", "应用设置" },
            { "Apply to all symmetry counterparts", "应用到所有对称部件" },
            { "Automatically arm when staging", "分级时自动预位" },
            { "Automatically deploys on ground contact", "触地时自动展开" },
            { "Engineer minimum level to repack:", "重新打包所需最低工程师等级：" },
            { "Only engineers can repack in career", "生涯模式下只有工程师可以重新打包" },
            { "Only an engineer can repack a parachute", "只有工程师才能重新打包降落伞" },
            { "Only a level {0} and higher engineer can repack a parachute", "{0} 级或更高的工程师才能重新打包降落伞" },
            { "Activate April Fools' joke (DANGER!!)", "启用愚人节彩蛋（危险！！）" },
            { "Activate NyanMode", "启用 Nyan 模式" },
            { "RealChute Settings", "RealChute 设置" },
            { "RealChute Settings ", "RealChute 设置 " },
            { "RealChute Info Window ", "RealChute 信息窗口 " },
            { "RealChute Parachute Editor ", "RealChute 降落伞编辑器 " },

            // —— 信息窗口 / 提示 ——
            { "Reason: in space.", "原因：处于太空中。" },
            { "Reason: parachute is in fairings", "原因：降落伞在整流罩内" },
            { "Reason: stopped on the ground.", "原因：已停在地面上。" },
            { "Reason: too high.", "原因：高度过高。" },
            { "Parachute deployment failed.", "降落伞展开失败。" },
            { "Some parameters could not be applied", "部分参数未能应用" },
            { "The application of the parameters succeeded!", "参数应用成功！" },
            { "This RealChute part can be tweaked from the Action Groups window.", "该 RealChute 部件可在动作组窗口中调整。" },
            { "Warning: The mass of the craft was too high and the parachutes have been set at their limit. Please review the stats to make sure no problem may occur.", "警告：飞行器质量过大，降落伞已被设为极限值。请检查参数，确认不会出问题。" },
            { "'s parachute has been destroyed due to aero forces and heat.</color>", " 的降落伞因气动力与高温而损毁。</color>" },
            { "Main chute:", "主伞：" },

            // —— 版本检查 / 兼容性提示 ——
            { "Incompatible Mods Detected", "检测到不兼容的模组" },
            { "Additionally, some", "此外，部分" },
            { " installed mods may be incompatible with this version of Kerbal Space Program. Features may be broken or disabled. Please check for updates to the listed mods.", " 个已安装模组可能与当前版本的坎巴拉太空计划不兼容。相关功能可能异常或被禁用，请检查这些模组的更新。" },
            { "These mods are incompatible with KSP {0}.{1}.{2}:", "以下模组与 KSP {0}.{1}.{2} 不兼容：" },
            { "These mods are incompatible with Unity ", "以下模组与 Unity 不兼容 " },
            { "Assembly Version", "程序集版本" },
            { "Realistic Parachutes for Kerbal Space Program", "为坎巴拉太空计划打造的拟真降落伞系统" },

            // —— 筛选分类（RealChute 自带的 FilterExtensions 分类） ——
            { "Filter by Function", "按功能筛选" },
            { "Filter by Module", "按模块筛选" },
        };

        internal static void TryPatch()
        {
            if (patched)
            {
                return;
            }

            Assembly rc = FindRealChute();
            if (rc == null)
            {
                return;
            }

            patched = true;

            Harmony harmony;
            try
            {
                harmony = new Harmony(HarmonyId);
            }
            catch (Exception e)
            {
                Debug.LogError("[RealChuteCN] 无法建立 Harmony 实例：" + e);
                return;
            }

            int methods = PatchLiterals(harmony, rc);
            int modules = PatchGuiNames(harmony, rc);
            Debug.Log("[RealChuteCN] 汉化完成：字面量方法 " + methods + " 个，界面模块 " + modules + " 个。");
        }

        private static Assembly FindRealChute()
        {
            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                try
                {
                    if (asms[i].GetName().Name == "RealChute")
                    {
                        return asms[i];
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        // ------------------------------------------------------------------
        // 字面量：只给“确实含有目标字符串”的方法挂 Transpiler
        // ------------------------------------------------------------------
        private static int PatchLiterals(Harmony harmony, Assembly rc)
        {
            MethodInfo transpilerMethod = typeof(Patcher).GetMethod("Transpile", BindingFlags.Static | BindingFlags.NonPublic);
            HarmonyMethod transpiler = new HarmonyMethod(transpilerMethod);
            int count = 0;

            Type[] types;
            try
            {
                types = rc.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            for (int i = 0; i < types.Length; i++)
            {
                Type t = types[i];
                if (t == null)
                {
                    continue;
                }

                MethodBase[] ms = AllMethods(t);
                for (int j = 0; j < ms.Length; j++)
                {
                    MethodBase m = ms[j];
                    if (m == null || m.IsAbstract || m.ContainsGenericParameters)
                    {
                        continue;
                    }

                    if (!HasTargetLiteral(m))
                    {
                        continue;
                    }

                    try
                    {
                        harmony.Patch(m, null, null, transpiler, null);
                        count++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[RealChuteCN] 跳过方法 " + t.Name + "." + m.Name + "：" + e.Message);
                    }
                }
            }

            return count;
        }

        private static MethodBase[] AllMethods(Type t)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            List<MethodBase> list = new List<MethodBase>();
            try
            {
                MethodInfo[] methods = t.GetMethods(flags);
                for (int i = 0; i < methods.Length; i++)
                {
                    list.Add(methods[i]);
                }

                ConstructorInfo[] ctors = t.GetConstructors(flags);
                for (int i = 0; i < ctors.Length; i++)
                {
                    list.Add(ctors[i]);
                }
            }
            catch
            {
            }

            return list.ToArray();
        }

        // 扫描 IL 里的 ldstr 操作数，判断该方法是否含字典中的字符串
        private static bool HasTargetLiteral(MethodBase m)
        {
            try
            {
                MethodBody body = m.GetMethodBody();
                if (body == null)
                {
                    return false;
                }

                byte[] il = body.GetILAsByteArray();
                if (il == null)
                {
                    return false;
                }

                Module module = m.Module;
                for (int i = 0; i <= il.Length - 5; i++)
                {
                    if (il[i] != 0x72)
                    {
                        continue;
                    }

                    int token = BitConverter.ToInt32(il, i + 1);
                    try
                    {
                        string s = module.ResolveString(token);
                        if (s != null && Literals.ContainsKey(s))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                    }

                    i += 4;
                }
            }
            catch
            {
            }

            return false;
        }

        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction ci in instructions)
            {
                if (ci.opcode == OpCodes.Ldstr && ci.operand is string)
                {
                    string cn;
                    if (Literals.TryGetValue((string)ci.operand, out cn))
                    {
                        ci.operand = cn;
                    }
                }

                yield return ci;
            }
        }

        // ------------------------------------------------------------------
        // guiName：挂在 RealChute 各 PartModule 的 OnStart 之后
        // ------------------------------------------------------------------
        private static int PatchGuiNames(Harmony harmony, Assembly rc)
        {
            MethodInfo postfixMethod = typeof(Patcher).GetMethod("GuiNamePostfix", BindingFlags.Static | BindingFlags.NonPublic);
            HarmonyMethod postfix = new HarmonyMethod(postfixMethod);
            Type partModule = typeof(PartModule);
            int count = 0;

            Type[] types;
            try
            {
                types = rc.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            string[] hooks = { "OnStart", "Awake", "Start" };
            for (int i = 0; i < types.Length; i++)
            {
                Type t = types[i];
                if (t == null || !partModule.IsAssignableFrom(t))
                {
                    continue;
                }

                for (int h = 0; h < hooks.Length; h++)
                {
                    MethodInfo target = t.GetMethod(hooks[h], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
                    if (target != null && !target.IsAbstract)
                    {
                        try
                        {
                            harmony.Patch(target, null, postfix, null, null);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("[RealChuteCN] 跳过 " + t.Name + "." + hooks[h] + "：" + e.Message);
                        }
                    }
                }

                MethodInfo onStart = t.GetMethod("OnStart", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (onStart != null && !onStart.IsAbstract)
                {
                    try
                    {
                        harmony.Patch(onStart, null, postfix, null, null);
                        count++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[RealChuteCN] 跳过 " + t.Name + ".OnStart：" + e.Message);
                    }
                }
            }

            return count;
        }

        private static void GuiNamePostfix(PartModule __instance)
        {
            try
            {
                if (__instance == null)
                {
                    return;
                }

                if (__instance.Events != null)
                {
                    System.Collections.IEnumerable events = __instance.Events;
                    foreach (object o in events)
                    {
                        BaseEvent ev = o as BaseEvent;
                        if (ev != null)
                        {
                            ev.guiName = TranslateGuiName(ev.guiName);
                        }
                    }
                }

                if (__instance.Fields != null)
                {
                    System.Collections.IEnumerable fields = __instance.Fields;
                    foreach (object o in fields)
                    {
                        BaseField f = o as BaseField;
                        if (f != null)
                        {
                            f.guiName = TranslateGuiName(f.guiName);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!reported)
                {
                    reported = true;
                    Debug.LogWarning("[RealChuteCN] guiName 重写失败：" + e.Message);
                }
            }
        }

        private static string TranslateGuiName(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            string cn;
            if (GuiNames.TryGetValue(s, out cn))
            {
                return cn;
            }

            // 复数后缀 "S"（多伞时 RealChute 会拼一个 S）
            if (s.Length > 2 && s.EndsWith("S", StringComparison.Ordinal))
            {
                string trimmed = s.Substring(0, s.Length - 1);
                if (GuiNames.TryGetValue(trimmed, out cn))
                {
                    return cn;
                }
            }

            return s;
        }
    }

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class RealChuteCNBoostrap : MonoBehaviour
    {
        private void Awake()
        {
            Patcher.TryPatch();
        }
    }

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RealChuteCNRetry : MonoBehaviour
    {
        private void Awake()
        {
            Patcher.TryPatch();
        }
    }
}
