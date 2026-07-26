# Keiki Test Task

Невеликий Unity-проєкт із вибором рівня та механікою tracing для літер, цифр і фігур.

## Trace Route Editor

Кастомний editor tool використовується для редагування маршрутів tracing, які зберігаються як `TraceGeometryAsset`.

1. Відкрити `Window → Keiki → Trace Route Editor`.
2. У полі **Geometry** вибрати asset із `Assets/_Project/Data/Tracing`.
3. За потреби додати відповідний sprite у **Preview silhouette**.
4. Перейти у **Scene View** і перемістити огляд на світові координати **(0, 0, 0)** — робоча область редактора відображається навколо нульової точки сцени.
5. Вибрати активний stroke та редагувати його handles:
   - **Linear** — прямі сегменти;
   - **Bezier** — криві з окремими tangent handles.
6. За потреби використати **Reverse Stroke Direction**, орієнтуючись на стрілки напрямку.
7. Натиснути **Bake All**, а потім **Validate**. `Geometry is valid` означає, що маршрут готовий до використання.

Після переміщення handles активний stroke оновлюється автоматично, але перед завершенням редагування варто виконати `Bake All`, щоб перебудувати й зберегти всі strokes.

## Архітектура

Проєкт побудований навколо MVP із додатковим поділом на domain, services та infrastructure:

- **Model** — `Runtime/Domain`, конфігурація рівнів, repositories, stores та gameplay services.
- **View** — Unity-компоненти у `Runtime/UI`; відповідають за відображення та передають події користувача.
- **Presenter** — `MenuPresenter` і `GamePresenter`; координують View, стан застосунку та gameplay flow.

Залежності збираються через **Zenject**, асинхронні операції реалізовані через **UniTask**, а переходами `Boot → Menu → Game` керує state machine.

Основні папки:

- `Assets/_Project/Scripts/Core` — базові UI, state machine та scene management;
- `Assets/_Project/Scripts/Runtime` — domain, UI, services, infrastructure і composition root;
- `Assets/_Project/Scripts/Editor/Tracing` — Trace Route Editor та інструменти bake/validation;
- `Assets/_Project/Data/Tracing` — готові assets маршрутів;
- `Assets/_Project/Configs` — json конфігурація рівнів.
