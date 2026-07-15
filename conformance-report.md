# Отчёт о соответствии канону

Дата измерения: **2026-07-15**
Стек: Avalonia 11 / .NET 9 / xUnit / headless renderer.
CI подключён: Release-сборка, тесты и format-check запускаются на Windows, Linux и macOS.

## Scorecard

`canons 6 · fully-guarded 6 · PARTIAL 0 · NONE 0 · CI-wired ✓ · biggest lever: keep the three adoption ratchets green`

## Реестр

| Канон | Дом | Guard | Результат |
| --- | --- | --- | --- |
| Semantic visual tokens | `Themes/Colors`, `Themes/Tokens`, `Themes/Controls` | `FeatureViews_UseSemanticVisualTokens` | done · 0 |
| Motion timing | `Themes/Tokens` | `FeatureViews_UseSharedMotionTokens` | done · 0 |
| Status button | `Themes/Controls` | `StatusButton_HasOneSharedStyleHome` | done · 0 |
| Modal surface/fields | `Themes/Controls`, `Shared/ModalWindow` | accessibility + headless matrix | done |
| Vector icons | `Themes/Icons` | accessibility + headless matrix | done |
| Theme coverage | `Themes/Colors` + theme service | `EveryTheme_ResolvesWorkspaceShellTokens` | done · 14 themes |

Все три machine-readable записи drift находятся в [`findings.json`](findings.json). Они сохранены как
измеряемые ratchet-записи, а не как открытый backlog: после выравнивания у каждой baseline равен нулю.

## Findings по severity и kind

- **Findings с `kind: bug` отсутствуют.** В этом UI-проходе не найдено регрессий корректности,
  безопасности или контрактов.
- **Три записи `kind: drift`**, все informational/low severity, полностью защищены и измерены как 0.
- Осознанные вариации разрешены: theme dictionary может задавать свою палитру; feature-view может
  собирать уникальный layout из тех же semantic tokens; status glyph и bookmark star — семантика
  содержимого, а не декоративная цветовая система.

## Рецепт проверки

```text
dotnet test Tittle.sln -c Release --no-restore --nologo
dotnet run --project tools/HeadlessRender/HeadlessRender.csproj -c Release --no-restore -- plans/avalonia-smoke/goal-round1
dotnet format Tittle.sln --verify-no-changes --no-restore
```

Локальная headless-матрица покрывает 4 leaf-экрана × 14 тем. Live smoke остаётся финальной проверкой
window chrome и modal focus: полный MainWindow намеренно исключён из leaf-renderer из-за native/animation
side effects.

Все три adoption-guard дополнительно проверены временной synthetic violating view: suite ожидаемо упал
3/3; после удаления фикстуры вернулся к 3/3 green.
