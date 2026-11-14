# Наклоны Q/E в TPS и FPS

## Требования и решение

Наклон удержанием Q влево и E вправо; Q+E и отпускание возвращают корпус к нейтрали. Работает без оружия, с оружием, при ходьбе, приседе и ADS в обоих видах. Предел из оригинального `QE_TORSO_ROLL`: 38.1°, FPS camera multiplier: 0.2 (максимум 7.62°). TPS сохраняет ровный горизонт. Спринт, воздух и переходы позы плавно гасят наклон. Точные ограничения ввода и интерполяция оригинального PlayerAvatar не восстановлены; удержание и скорость сглаживания — решения ZoneReborn.

Наклон распределяется по существующей цепочке позвоночника до оружейного IK и skinning. Капсула, положение ног и выбор locomotion clips не меняются. FPS-камера следует получившемуся Spine3; её roll участвует в view basis, небе, billboard-растительности и преобразовании мыши. Ограничение наклона возле препятствий использует sweep нейтральной/наклонённой скелетной точки глаз через существующую scene collision, до финальной оценки оружия и IK.

## План

- [x] Добавить ввод Q/E и общий угол наклона в Locomotion; обработать нейтраль, sprint/jump, потерю фокуса и F8.
- [x] Подключить процедурный наклон позвоночника и ограничение по сцене до IK, сохранив одну финальную skinning-оценку.
- [x] Передать FPS-roll в camera/view, sky/billboard и mouse; согласовать ADS-offset со скелетной камерой.
- [x] Добавить подсказку управления, проверить исходники и матрицы, выполнить независимое статическое ревью и `git diff --check`.

Проверка только статическая по AGENTS.md. Не создавать и не запускать тесты, тестовые приложения и сборки. Пользовательские настройки FOV не изменяются этой механикой, включая параллельную правку значений камеры во время реализации.

## Подтверждение в оригинале

- Packed `sdk/scripts/client/data/default_key_bindings.xml`: `TorsoRollLeft` → Q, `TorsoRollRight` → E.
- `player_speed_settings.pyc_dis`, `QE_TORSO_ROLL`: `MAX_TORSO_ROLL = GRAD(38.1)`, `FPS_CAMERA_MULT = 0.2`.
- `Avatar.pyc_dis`, `onTorsoRoll`: сохраняет угол, вызывает `updateTorsoRoll`.
- `AvatarTrackers.pyc_dis`, `updateTorsoRoll`: устанавливает `torso_nodeinfo.addRoll`, уведомляет камеру и систему поворотов.
- `view_control.pyc_dis`, `_nearPlaneCheck` и `pConvertMouseMoveDcursor`: наклон задаёт camera upright direction и участвует в преобразовании движения мыши.
- `turn_avatar.pyc_dis`/`new_turn.pyc_dis`: угол передаётся нативному ActionMatcher/системе поворотов.

Нативный TorsoTracker и недекомпилированная логика PlayerAvatar не считаются полностью восстановленными. Распределение угла по Spine/Spine1/Spine2 с весами 0.20/0.35/0.45, сглаживание 12 с⁻¹ и sweep-проверка — реализация в существующей архитектуре ZoneReborn.

## Изменённые файлы

| Файл | Назначение |
| --- | --- |
| src/World/Characters/Locomotion/AnimState.cs | Lean во входе; исходные предел угла и FPS multiplier |
| src/World/Characters/Locomotion/CharacterLocomotion.Lean.cs | Сглаживание, разрешённые состояния, eye sweeps и ограничение угла у scene collision |
| src/World/Characters/Locomotion/CharacterLocomotion.cs | Обновление угла перед итоговой позой, публикация угла и сброс при ReleaseInput |
| src/World/Characters/CharacterPose.Aim.cs | Процедурный roll позвоночника, без изменения положения капсулы и ног |
| src/World/Characters/CharacterPose.FirstPerson.cs | Единая rolled look matrix для eye offset, weapon tracking и ADS reach |
| src/World/Characters/CharacterAsset.cs | Режим предварительной оценки скелета с оружейными слоями, до IK/skinning |
| src/World/Characters/CharacterPose.Weapon.cs | Выделен общий ApplyWeaponLayers для preview и окончательной позы |
| src/World/Characters/Locomotion/CharacterAnimator.cs | Передача режима skin без продвижения времени клипа |
| src/World/Characters/ThirdPersonCamera.cs | FPS-roll, преобразование мыши, rolled collision basis; TPS horizon остаётся ровным |
| src/Editor/Viewport/FreeCamera.cs | Опциональный Roll в CameraPose и общий right/up basis |
| src/Editor/Viewport/Dx11Renderer.cs | View-матрица использует camera right/up с учётом roll |
| src/Editor/Viewport/Environment/SkyRenderer.cs | Небо использует тот же rolled basis |
| src/Editor/Viewport/Vegetation/TreeRenderer.cs | Billboard-геометрия использует тот же rolled basis |
| src/Editor/Viewport/DxViewportHost.cs | Q/E отслеживаются только в управлении персонажем |
| src/Editor/Viewport/DxViewportHost.Play.cs | Передача Lean в Locomotion, угла в orbit и Roll в play camera; сброс при освобождении ввода |
| src/Editor/Viewport/DxViewportHost.Inspection.cs | Свободный осмотр F8 начинается с ровным горизонтом |
| src/Editor/MainWindow.axaml | Подсказка Q/E; существующие C TPS/FPS и V присед подписаны согласно обработчику |
| src/Editor/Play/PlayCommands.cs | Подсказки управления в строке Play |
| docs/lean.md | Решение, источники, план и границы проверки |

## Статическая проверка

Q задаёт положительный угол: при row-vector RotationZ верх корпуса уходит к отрицательному X (влево). E задаёт отрицательный угол. Оба удержания дают ноль. При повороте персонажа ось наклона выражена через LookYaw в model space, а world yaw применяется обычным renderer.

Сумма весов позвоночника равна 1. Roll применяется к готовой матрице узла вокруг его позиции, затем наследуется потомками; процедурная поправка не записывается в базовые `_locals` и не накапливается по кадрам. Оружейные target и IK используют уже наклонённый скелет; прежняя синхронизация одежды сохраняется.

Предварительные оценки для sweep используют тот же clip/frame/blend, feet/torso yaw, хваты и track-12 overlay, что и окончательная поза. Это важно: исходные equip/holster actions действительно содержат NodePriority=12 на Spine/Spine1/Spine2. После независимого review выделен общий ApplyWeaponLayers, чтобы проверки H+Q/E не использовали другую позицию глаз. Preview не запускает tracking/IK/skinning и не продвигает время; CaptureBlend сохраняет прежние базовые локальные матрицы. Окончательный skin остаётся один на обычный кадр обновления.

View, небо и растительность получают одну ориентацию `Rz(roll) * Rx(-pitch) * Ry(yaw)`. При нулевом roll получаются прежние right/up и yaw/pitch мыши. TPS-roll равен нулю; при переходе C он смешивается с FPS-blend. При F8/потере фокуса сбрасываются клавиши, угол корпуса и camera roll. Старые вызовы CameraPose без Roll получают значение 0.

Выполнены чтение изменённых исходников, проверка всех вызовов новых параметров, XML-разбор MainWindow.axaml, проверка UTF-8/отступов и `git diff --check`. Независимое статическое ревью после исправления preview не обнаружило Critical/Important ошибок. Сборки, тесты и приложения не запускались; компиляция и визуальный результат не подтверждены.

## Проверить в Play

- Q/E и Q+E в TPS/FPS, отпускание, быстрая смена стороны и поворот мышью при наклоне.
- Без оружия, с оружием, ADS, H во время наклона, разные рукава/перчатки.
- Стоя/в приседе, W/A/S/D и диагонали, переход в спринт, прыжок и приземление.
- C при удержании Q/E, F8 туда/обратно, потеря фокуса с зажатой клавишей.
- Стены с обеих сторон и углы; согласованное положение мушки/целика, неба и billboard-листвы.

Sweep ограничивает смещение скелетных eye points с запасом 0.15 м через существующие scene collider. Это не отдельная физическая модель всей наклонённой одежды, головы или ствола; коллизия всей геометрии и препятствия без импортированного collider этим способом не гарантируются.
