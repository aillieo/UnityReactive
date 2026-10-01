# Unity Reactive

[English](README.md)

一个面向 Unity 的轻量级响应式状态库，提供响应式属性、派生状态、自动依赖追踪、响应式集合、事件流、Unity 生命周期、uGUI 绑定和编译期 Model 生成。

## 环境要求

- Unity `2022.3` 或更高版本
- `com.unity.ugui@1.0.0`

## 安装

在 Unity Package Manager 中选择 **Add package from git URL...**，输入：

```text
https://github.com/aillieo/UnityReactive.git#v0.2.0
```

## 快速开始

```csharp
using AillieoUtils.Reactive;
using AillieoUtils.Reactive.Unity;
using UnityEngine;

var scope = new ReactiveScope();
var gold = Rx.Property(100);
var price = Rx.Property(30);
var canBuy = scope.Add(Rx.Computed(() => gold.Value >= price.Value));

scope.Observe(() =>
    Debug.Log($"Gold={gold.Value}, CanBuy={canBuy.Value}"));

Rx.Batch(() =>
{
    gold.Value -= 30;
    price.Value = 50;
});

// GameObject 销毁时释放 scope 及其持有的订阅。
scope.AddTo(gameObject);
```

`Observe` 创建时会立即执行一次；后续变化默认在 Unity `LateUpdate` 批量刷新。所有状态访问和订阅操作应在 Unity 主线程完成。

## Reactive Model

包内包含预编译的 Source Generator。Model 及其所有外层类型必须声明为 `partial class`。

```csharp
using AillieoUtils.Reactive;

[ReactiveModel]
public sealed partial class PlayerModel
{
    private int health = 100;
    private int gold;

    partial void OnHealthChanged(int oldValue, int newValue)
    {
        UnityEngine.Debug.Log($"HP: {oldValue} -> {newValue}");
    }
}
```

生成代码会提供 `Health`、`Gold` 属性，以及对应的只读响应式适配器 `HealthProperty`、`GoldProperty`。

## 常用 API

```csharp
var inventory = Rx.Collection<string>();
scope.Observe(() => Debug.Log(inventory.Count));
inventory.Add("Potion");

var damage = Rx.Event<int>();
damage.Subscribe(value => Debug.Log(value)).AddTo(scope);
damage.Emit(10);

scope.BindText(label, textProperty);
scope.BindInteractable(button, canSubmitProperty);
scope.BindToggleTwoWay(toggle, enabledProperty);
scope.BindSliderTwoWay(slider, volumeProperty);

RxUnity.Timer(System.TimeSpan.FromSeconds(1))
    .Subscribe(_ => HideHint())
    .AddTo(scope);
```

此外还提供 `ReactiveDictionary`、`ReactiveSet`、`RxUnity.EveryUpdate`、`RxUnity.Interval` 和其他 uGUI 单向绑定。

## 示例

在 Package Manager 中打开本包，从 **Samples** 导入 **Reactive Dashboard**。

## 许可证

[MIT License](LICENSE)
