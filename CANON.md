# Канон интерфейса Tittle

Этот файл — тонкая карта канона, сгенерированная из `findings.json`. Исполняемые adoption-guards
остаются источником истины: тесты должны падать при появлении нового визуального расхождения.

## Визуальный слой

| Область | Каноническое место | Adoption-guard |
| --- | --- | --- |
| Semantic colours, surfaces, borders, and shadows | `src/Tittle/Themes/Colors/*.axaml`, `src/Tittle/Themes/Tokens.axaml`, `src/Tittle/Themes/Controls.axaml` | `FeatureViews_UseSemanticVisualTokens` |
| Motion timing | `src/Tittle/Themes/Tokens.axaml` (`DurFast`, `DurBase`) | `FeatureViews_UseSharedMotionTokens` |
| Compact status-bar buttons | `src/Tittle/Themes/Controls.axaml` (`Button.statusbtn`) | `StatusButton_HasOneSharedStyleHome` |
| Modal surface and fields | `src/Tittle/Themes/Controls.axaml` (`Border.modalcard`, `modal-field`, `modal-dismiss`) + `src/Tittle/Shared/ModalWindow.axaml.cs` | UI/accessibility suite and headless render matrix |
| Vector affordance icons | `src/Tittle/Themes/Icons.axaml` | accessibility suite + headless render matrix |

## Логика и контракты

Команды, контракты view-model, хранение и границы платформы остаются у существующих интерфейсов и
сервисов, описанных в `ARCHITECTURE.md`; этот визуальный проход не добавляет альтернативных путей
диспетчеризации или состояния.

## Архитектура

Приложение хранит layout конкретных функций в feature-view, а общие визуальные правила — в `Themes/`.
CI запускает Release-сборку, полный набор xUnit-тестов и format-check на Windows, Linux и macOS
(`.github/workflows/ci.yml`). Avalonia headless-матрица — дополнительный локальный визуальный gate.

## Безопасность и i18n

Этот проход не меняет allowlist URL, границы clipboard, работу с файлами и localization-контракты.
Ссылки поддержки по-прежнему проходят через существующий безопасный launcher.

## Ratchet status

- `visual-semantic-tokens`: baseline **0** violations.
- `shared-motion-tokens`: baseline **0** literal transition durations outside the token dictionary.
- `status-button-single-home`: baseline **0** feature-local `statusbtn` style definitions.

P2 — `N/A`: после предыдущей консолидации конкурирующих реализаций не осталось. P3 завершён:
недостающие контракты palette/modal/numeric/dismiss параметризованы в общем доме стилей.
P4 завершён: три adoption-guard с высоким радиусом зелёные и подключены через xUnit/CI.
P5 завершён для этого прохода: все проверенные расхождения приведены к канону без изменения
пользовательского поведения.
