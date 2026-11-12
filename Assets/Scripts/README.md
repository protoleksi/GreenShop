# GreenShop — скрипты по задачам

Все скрипты лежат в `Assets/Scripts` и используют namespace `GreenShop`.

> Пошаговая инструкция по подключению всех компонентов: [`SETUP_GUIDE_RU.txt`](SETUP_GUIDE_RU.txt).

| Задача из ТЗ | Скрипты |
|---|---|
| 1. Базовый кликер | `Clicker/Clickable`, `Clicker/FloatingText`, `Clicker/FloatingTextSpawner`, `Core/GameManager`, `Economy/IncomeTicker`, `UI/CurrencyHud` |
| 2. Навигация между зонами/сценами | `Navigation/ZoneCameraController` (pan в одной сцене), `Navigation/SceneLoader` + `Navigation/ScreenFade` (fade между сценами) |
| 3. ScriptableObject растений | `Plants/PlantData`, `Plants/PlantDatabase`, `Editor/PlantAssetGenerator` |
| 4. Система сохранений | `Core/SaveData`, `Core/SaveSystem`, `Core/SaveManager` |

---

## 1. Кликер

### Объект-кликер
1. Создай объект со спрайтом растения/магазина.
2. Добавь `Collider2D` (например, `BoxCollider2D`).
3. Добавь компонент `Clickable`.
4. На `Main Camera` добавь `Physics2DRaycaster`.
5. В сцене должен быть `EventSystem` (`GameObject > UI > Event System`). При включённом Input System он создаётся с `InputSystemUIInputModule`.

`Clickable` реализует `IPointerClickHandler`, поэтому клики работают и с новым Input System.
За клик: `+1` монета, всплывающий текст `+1`, лёгкая анимация масштаба и опциональный звук.

Метод `Clickable.Click()` можно повесить и на обычную UI-кнопку через `Button.onClick`.

### Всплывающий текст
1. Создай `Canvas` (Screen Space — Overlay) и на него/в него добавь `FloatingTextSpawner`.
2. Сделай префаб: `UI > Text - TextMeshPro`, добавь `CanvasGroup` и `FloatingText`.
3. В `FloatingText` назначь `Label` (TMP-текст) и `CanvasGroup`.
4. Положи префаб в поле `Prefab` у `FloatingTextSpawner`.

Если Unity предложит импортировать TMP Essentials — согласись.

### Монеты, доход и таймер
1. Создай пустой объект `GameRoot`.
2. Добавь на него `GameManager`, `IncomeTicker` и `SaveManager`.
   - `Starting Income Per Second` = `1` — это демо-доход, чтобы таймер сразу работал.
   - `GameManager.AddCoins(...)`, `AddIncome(...)`, `TrySpendCoins(...)` — публичное API для магазина.
3. HUD: на Canvas положи TMP-тексты для монет и дохода.
   - Для полоски таймера: `UI > Image`, `Image Type = Filled`, `Fill Method = Radial 360` или `Horizontal`.
   - Добавь `CurrencyHud` и назначь ссылки; поле `Income Ticker` можно оставить пустым — найдётся автоматически.
4. `IncomeTicker` раз в секунду начисляет `IncomePerSecond` и отдаёт прогресс для `Image.fillAmount`.

---

## 2. Навигация

### Вариант А: 3 камеры/зоны в одной сцене (pan)
1. Создай 3 пустых объекта `Zone_Farm`, `Zone_Shop`, `Zone_Garden` в местах, куда должна приехать камера (позиция и поворот камеры).
2. На объект с камерой (или отдельный `ZoneController`) добавь `ZoneCameraController`.
3. Заполни массив `Zones`: имя зоны + `View Point` (пустой объект).
4. Кнопки зон в UI: в `Button.onClick` перетащи `ZoneCameraController` и выбери `GoToZone(int)` с индексом `0/1/2`, либо `GoToNextZone()` / `GoToPreviousZone()`.
5. Скорость и кривая плавности настраиваются в `Move Duration` и `Ease Curve`.

Посещённые зоны автоматически попадают в `GameManager.UnlockedZones` (можно отключить `Unlock Visited Zone`).

### Вариант Б: 3 сцены (fade)
1. Создай сцены (например, `Farm`, `Shop`, `Garden`) и добавь их в `File > Build Settings > Scenes In Build`.
2. В первой сцене создай отдельный объект `SceneLoader` и добавь компонент `SceneLoader`.
3. Поле `Fade` можно оставить пустым — затемнение создастся автоматически (чёрный Canvas поверх всего).
4. Кнопки: `Button.onClick` → `SceneLoader.LoadScene(string)` с именем сцены или `LoadScene(int)` с индексом.
5. `SceneLoader` переживает смену сцен (`DontDestroyOnLoad`). Не добавляй его в каждую сцену — достаточно одной.

`ScreenFade` также можно использовать отдельно: `FadeOut()`, `FadeIn()`, `FadeTo(alpha, duration)`.

---

## 3. Растения (ScriptableObject)

`PlantData` содержит поля из ТЗ:

- `Plant Name` — название;
- `Grow Time` — время роста в секундах;
- `Price` — цена;
- `Role` — роль (`Культура`, `Цветок`, `Дерево`, `Особое`);
- `Season` — сезон (`Весна`, `Лето`, `Осень`, `Зима`, `Все сезоны`);
- плюс `Icon`, `Grown Sprite`, `Description` для UI.

### Автозаполнение 9 растений
Меню: **Tools > GreenShop > Растения > Создать 9 растений (заглушки)**.

Скрипт создаёт `Assets/Data/Plants/Plant_01..09.asset` и общую базу `Assets/Data/PlantDatabase.asset`.

> В генераторе стоят заглушки (Редис, Морковь, Клубника, Огурец, Томат, Подсолнух, Тыква, Яблоня, Лаванда). Замени значения на реальные из таблицы ГД в инспекторе. Уже существующие ассеты повторный запуск не перезаписывает.

Ручное создание: `Assets > Create > GreenShop > Растение (PlantData)`.

`PlantDatabase.GetById(id)` и `GetByIndex(index)` — для магазина/грядок. `id` растения — имя его ассета.

---

## 4. Сохранения

- `SaveData` — версия формата, монеты, доход, клики, время игры, список растений, открытые зоны. При добавлении полей увеличь `SaveData.CurrentVersion`.
- `SaveSystem` — JSON в `Application.persistentDataPath/save.json`, резервная копия `save.json.bak` и дублирование в `PlayerPrefs` (ключ `greenshop.save.v1`). При повреждении основного файла загрузка идёт из бэкапа/PlayerPrefs.
- `SaveManager` — автосохранение каждые 30 секунд, при сворачивании и выходе. Метод `SaveNow()` можно повесить на кнопку.

Полезные пункты меню:

- **Tools > GreenShop > Сохранение > Показать путь сохранения**
- **Tools > GreenShop > Сохранение > Открыть папку сохранений**
- **Tools > GreenShop > Сохранение > Удалить сохранение**

Для теста: запусти игру, накликай монеты, выйди и зайди снова — прогресс восстановится. Удалить прогресс — пункт меню выше.

---

## Порядок объектов в сцене (пример)

```
GameRoot                  GameManager + IncomeTicker + SaveManager
Main Camera               Camera + Physics2DRaycaster
EventSystem               EventSystem (+ InputSystemUIInputModule)
Canvas                    Canvas + FloatingTextSpawner + CurrencyHud
  CoinsLabel              TMP_Text
  IncomeLabel             TMP_Text
  IncomeFill              Image (Filled)
Grid                      спрайт + BoxCollider2D + Clickable
Zone_Farm / Zone_Shop / Zone_Garden   пустые точки для ZoneCameraController
SceneLoader               SceneLoader (только в первой сцене)
```

Все скрипты не используют старый `Input` напрямую, поэтому проект корректно работает с активным Input System.
