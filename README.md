# Zoo World

**Language / Язык:** [English](#english) | [Русский](#русский)

---

## English

A Unity prototype for the "Zoo World" test task: a top-down zoo where
animals spawn on a timer, move through real PhysX, and predators eat
prey. The main design goal is an animal architecture that scales to
roughly a thousand species, where each new species is data — a
definition asset, a movement profile, an interaction profile and a
prefab — with no branches inside shared systems.

- Project: `UnityProject/` (Unity `6000.6.2f1`, the version is fixed in
  `ProjectSettings/ProjectVersion.txt`)
- Architecture and acceptance contract: [ARCHITECTURE.md](ARCHITECTURE.md)
- Task analysis (Russian): [TEST_TASK_ANALYSIS.md](TEST_TASK_ANALYSIS.md)

Run: open `UnityProject` in Unity 6000.6.x and press Play — the startup
scene is `Assets/Scenes/ZooWorld.unity`.

Tests: NUnit via Unity Test Framework. With the Editor running:

```bash
unity command test run --mode editor --format json --project-path UnityProject
unity command test run --mode playmode --async_tests true --format json --project-path UnityProject
```

Add a species: create a movement-profile asset (or reuse an existing
one), an interaction profile, a prefab and an `AnimalDefinition`; add
the definition to `AnimalCatalog`. No code changes are needed — see the
demo species `hare`/`turtle` under
`Assets/Tests/PlayMode/Fixtures/`.

---

## Русский

Прототип для тестового задания «Zoo World»: вид сверху на зоопарк, где
животные появляются по таймеру, двигаются через настоящую физику PhysX,
а хищники едят добычу. Главная цель — архитектура животных, которая
масштабируется примерно до тысячи видов. Новый вид — это данные: ассет
определения, профиль движения, профиль взаимодействия и префаб.
В общих системах нет веток под конкретные виды.

- Проект: `UnityProject/` (Unity `6000.6.2f1`, версия зафиксирована в
  `ProjectSettings/ProjectVersion.txt`)
- Архитектура и приёмочный контракт: [ARCHITECTURE.ru.md](ARCHITECTURE.ru.md)
- Разбор задания: [TEST_TASK_ANALYSIS.md](TEST_TASK_ANALYSIS.md)

Запуск: открыть `UnityProject` в Unity 6000.6.x и нажать Play —
стартовая сцена `Assets/Scenes/ZooWorld.unity`.

Тесты: NUnit через Unity Test Framework. При открытом редакторе:

```bash
unity command test run --mode editor --format json --project-path UnityProject
unity command test run --mode playmode --async_tests true --format json --project-path UnityProject
```

Добавить вид: создать ассет профиля движения (или взять существующий),
профиль взаимодействия, префаб и `AnimalDefinition`; добавить
определение в `AnimalCatalog`. Код менять не нужно — примеры видов
`hare` и `turtle` лежат в `Assets/Tests/PlayMode/Fixtures/`.
