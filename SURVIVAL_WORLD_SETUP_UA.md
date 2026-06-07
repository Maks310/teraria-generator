# Survival World Generator — інструкція для Unity

## Обраний підхід

Для невеликої інді-команди найпрактичніше рішення — один процедурний mesh ландшафту 2048x2048, один прозорий water mesh на глобальному рівні води та простий shader, який бере колір біому з vertex color. Це дешевше за сотні тайлів і не потребує дорогого texture splat pipeline на старті.

Генератор використовує seamless torus noise: координати `0` і `1` дають однакові значення шуму, а останній ряд/стовпець карти копіюється з першого. Тому лівий край збігається з правим, а нижній — з верхнім.

Один із біомів спеціально зроблений схожим на приклад із зображення: `Tundra` має темний фіолетово-синій ґрунт, холодний нічний настрій і добре підходить для майбутніх темних хвойних дерев, грибів, каменів та магічних рослин.

## Що створити в сцені

1. Створи порожній `GameObject` з назвою `World`.
2. Додай на нього компонент `WorldGenerator`.
3. Unity автоматично додасть `MeshFilter`, `MeshRenderer` і `MeshCollider`, бо генератор їх вимагає.
4. Створи material `TerrainMaterial` із shader `Custom/SurvivalTerrainBiome`.
5. Створи material `WaterMaterial` із shader `Custom/SeamlessSurvivalWater`.
6. Признач `TerrainMaterial` у поле `Terrain Material` компонента `WorldGenerator`.
7. Признач `WaterMaterial` у поле `Water Material` компонента `WorldGenerator`.
8. Натисни меню компонента `WorldGenerator` → `Generate World` або залиш `Generate On Start` увімкненим.

## Основні параметри в Inspector

- `World Size` — розмір світу. Для завдання залиш `2048`.
- `Mesh Resolution` — деталізація mesh. Практичний старт: `512`. Якщо лагає в Editor — `256`.
- `Height Scale` — висота гір і рельєфу.
- `Water Level` — рівень океану/морів; усе нижче цього рівня автоматично буде під водою.
- `Land Amount` — скільки суші проти океану.
- `Inland Sea Amount` — шанс внутрішніх морів/великих озер.
- `Continent Scale` — розмір материків.
- `Domain Warp Strength` — наскільки природно викривлені узбережжя.
- `Mountain Amount`, `Mountain Scale`, `Mountain Sharpness` — кількість і форма гір.
- `Desert Heat`, `Desert Dryness`, `Tundra Cold` — кліматичні межі трьох біомів.
- `Plains/Desert/Tundra Colors` — базові кольори біомів.
- `Auto Update In Editor` — перегенерація в Editor після зміни параметрів.

## Як працює вода

Вода — це один дочірній об’єкт `Water`, який генератор створює або перевикористовує. Це запобігає дублюванню води після повторної генерації. Вода лежить на висоті `Water Level * Height Scale + Water Surface Offset`, а низини ландшафту автоматично опиняються під нею.

Shader `Custom/SeamlessSurvivalWater` має хвилі, які рахуються від `World Size`, тому хвиля теж не має різкого шва на краях карти.

## Як додавати дерева, каміння та інші об’єкти

1. Створи порожній `GameObject` з назвою `WorldObjects`.
2. Додай компонент `WorldObjectSpawner`.
3. У поле `World Generator` перетягни об’єкт `World`.
4. У `Tree Prefabs` додай дерева, у `Rock Prefabs` — камені.
5. У `Biome Rules` налаштуй шанси для `Plains`, `Desert`, `Tundra`.
6. Для біому як на зображенні додай у правило `Tundra` extra prefabs: темні ялинки, фіолетові гриби, кристали, сухі колоди.
7. Натисни `WorldObjectSpawner` → `Spawn World Objects`.

Для production-версії в survival-грі краще пізніше замінити масове створення GameObject-ів на object pooling, GPU instancing або chunk streaming. Поточний spawner — безпечна база для прототипу.

## Як уникнути лагів і помилок

- Не став `Mesh Resolution` вище `512` без потреби. `1024` вже важкий для Editor.
- Якщо часто крутиш параметри, тимчасово вимкни `Update Collider` — MeshCollider дорогий.
- Якщо змінив багато параметрів підряд і Editor повільний, вимкни `Auto Update In Editor` і запускай `Generate World` вручну.
- Не створюй воду вручну багато разів. Генератор сам підтримує один дочірній об’єкт `Water`.
- Для великої кількості дерев збільш `Placement Step` у `WorldObjectSpawner` або зменш `Global Density`.
- Для гри з виживанням великий наступний крок — chunk streaming: тримати повну карту даних, але mesh/об’єкти створювати навколо гравця чанками.
