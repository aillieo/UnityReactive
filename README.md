# Unity Reactive

[简体中文](README.zh-CN.md)

A lightweight reactive state library for Unity, featuring reactive properties, computed values, automatic dependency tracking, reactive collections, event streams, Unity lifecycle integration, uGUI bindings, and compile-time model generation.

## Requirements

- Unity `2022.3` or later
- `com.unity.ugui@1.0.0`

## Installation

In Unity Package Manager, select **Add package from git URL...** and enter:

```text
https://github.com/aillieo/UnityReactive.git#v0.2.0
```

## Quick Start

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

// Dispose the scope and its subscriptions when the GameObject is destroyed.
scope.AddTo(gameObject);
```

`Observe` runs immediately when created. Later changes are batched and flushed during Unity `LateUpdate`. Access reactive state and subscriptions from the Unity main thread.

## Reactive Models

The package includes a precompiled source generator. A model and all its containing types must be declared as `partial class`.

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

The generated model exposes `Health` and `Gold`, plus the read-only reactive adapters `HealthProperty` and `GoldProperty`.

## Common APIs

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

Additional APIs include `ReactiveDictionary`, `ReactiveSet`, `RxUnity.EveryUpdate`, `RxUnity.Interval`, and one-way uGUI bindings.

## Sample

Open this package in Package Manager and import **Reactive Dashboard** from **Samples**.

## License

[MIT License](LICENSE)
