# Project Cook — โครงสร้างโค้ด

เอกสารนี้อธิบายว่าโค้ดแบ่งเป็นส่วนไหนบ้าง แต่ละส่วนทำอะไร และถ้าจะเพิ่มของใหม่ต้องทำที่ไหน กฎสั้นๆ ที่ต้องทำตามทุกครั้งอยู่ใน `.agents/AGENTS.md`

ภาพรวมของเกม: ลูปแบบ Tarkov × Monster Hunter คือเตรียมตัวที่บ้าน → ออกไปหาวัตถุดิบในแมพที่สร้างจากห้อง → กลับบ้านมาทำอาหาร เนื้อเรื่องเดินผ่านบทสนทนากับ NPC

---

## 1. ฉาก (Scene)

```
[MainMenu]  ฉากแรกของเกม ปุ่ม Play/Exit ผ่าน SceneFlow (Play = โหลด Core แบบ Single)
        │
[Core] โหลดค้างตลอดเกม
  Systems: GameModeManager, CursorManager, LocationLoader, QuestLog, SaveService, InputContextSwitcher,
           CutscenePlayer, CutsceneScheduler
  ผู้เล่น กล้อง UI หลัก (GameCanvas) บทสนทนา EventSystem
        │ LocationLoader สลับพื้นที่
[Home]      บ้าน ทำมือ save ได้ ออกไปแล้วถูกซ่อนไว้ (ไม่ปลด) ของในบ้านจึงอยู่ครบ
[TestArea]  พื้นที่ทดสอบสำหรับ dev ปิดได้ที่ GameData/Dev/DevSettings
[Raid ...]  (อนาคต) แมพที่สร้างจากห้อง ไม่ save
```

- หนึ่งพื้นที่ = หนึ่งฉาก + หนึ่ง `LocationSO` (`GameData/Locations/`) ในนั้นมีช่องติ๊ก **Keep Loaded** (ออกไปแล้วซ่อนไว้), **Dev Only** และ **Is Outside** (ข้างนอก: ไม่ save และของที่เกิดที่นี่ติดป้าย FoundOutside)
- ฉากพื้นที่จะเป็นฉากหลัก (Active Scene) เสมอ ของที่สร้างตอนเล่นจึงไปอยู่ในพื้นที่ และแสงกับท้องฟ้ามาจากพื้นที่
- object ในฉากหนึ่งอ้างถึง object ในอีกฉากไม่ได้ (Unity ไม่รองรับ) ให้หาผ่าน Singleton ใน Core แทน
- **กด Play ใน Editor** จากฉากพื้นที่ไหนก็ได้ `CoreSceneBootstrap` จะเปิด Core เพิ่มให้ก่อนเข้า Play แล้วปิดคืนเมื่อหยุด ไม่ต้องบันทึกฉากก่อนกด Play ยกเว้นกด Play จาก MainMenu ซึ่งเดินแบบเกมจริง (ปุ่ม Play โหลด Core เอง)
- ฉาก MainMenu ห้ามมี script เกมที่หา Singleton ของ Core เพราะตอนอยู่เมนู Core ยังไม่ถูกโหลด

## 2. Assembly (กล่องโค้ด)

แต่ละกล่องอ้างถึงได้เฉพาะกล่องที่อยู่ชั้นล่างกว่า ถ้าเผลออ้างกลับขึ้นไป Unity จะ compile ไม่ผ่าน

```
Cinematics ─────► NPC, Quests, Dialogue, Player, Core
UI ─────────────► Inventory, Items, Core
NPC ────────────► Quests, Cooking, Inventory, Dialogue, Items, Core
Quests ─────────► Cooking, Inventory, Dialogue, Items, Core
Cooking ────────► Stations, Player, Items, Core
Stations ───────► Player, Items, Core
Inventory ──────► Items, Core
Dialogue ───────► Core
Items, Player ──► Core
Core               (ไม่รู้จักระบบเกมใดเลย)
Editor             (เครื่องมือใน Editor เท่านั้น)
```

## 3. ระบบหลัก

### โหมดเกมและปุ่ม
- `GameModeManager` เก็บโหมดเป็นชั้นซ้อนกัน (Exploring / Station / Dialogue / Inventory / Loading / Cutscene) เช่น เปิดกระเป๋าทับสถานี ปิดกระเป๋าแล้วกลับไปที่สถานี
- `InputContextSwitcher` เปิดชุดปุ่ม (action map) ตามโหมดบนสุด และเป็นที่เดียวที่เปิด/ปิดปุ่มได้

| โหมด | ชุดปุ่ม |
|---|---|
| Exploring | Gameplay, View, World |
| Station | Station, World |
| Dialogue | Dialogue, View |
| Inventory | Menu |
| Loading | – |
| Cutscene | Cutscene, View |

  ชุด Dev เปิดตลอด
- script อ่านปุ่มผ่าน `InputActionReference` + extension ใน Core ปุ่มในชุดที่ปิดอยู่จะอ่านได้ว่า "ไม่ได้กด" เอง

### ไอเทม
- `ItemDefinitionSO` = ข้อมูลประจำชนิด ส่วน `ItemInstance` = **ตัวตนของไอเทมหนึ่งชิ้น** ที่ติดตัวไปทุกที่ (บนพื้น / ในมือ / ในที่เก็บ) เก็บ Flags (เช่น FoundOutside) และสถานะของ component ด้วยรหัสคงที่
- `Item` (บน prefab) เป็นร่างในโลกของตัวตน ทำหน้าที่จดสถานะ (`Capture`) และคืนสถานะ (`Restore`) ให้ component ที่ implement `ISavableState`
- ความสามารถของไอเทมมาจาก component: `StorableItem` (เก็บเข้ากระเป๋าได้), `EquippableItem` (ถือได้), `BagItem` (สวมได้)
- `PlayerItemTransfer` เป็นทางเดียวที่ของย้ายระหว่างโลกกับตัวผู้เล่น

### ที่เก็บ (Grid inventory)
- `ItemStorage` → `ItemGrid` → `InventoryItem` (ตำแหน่ง + ตัวตน) เป็น class ธรรมดาทั้งหมด ส่วน UI แค่ฟัง event `Changed`
- กฎพิเศษของที่เก็บแต่ละแบบ (เช่น ร้านค้า) ใส่ผ่าน `IStorageRules`

### การทำอาหาร
- `CookingProgress` (class ธรรมดา) เก็บเวลาทอดแต่ละด้าน แล้วตัดสินสถานะ ดิบ → กำลังสุก → สุก → ไหม้
- `IngredientInstance` หาว่าด้านไหนแตะกระทะ แล้วส่งความร้อนต่อให้ `CookingProgress`
- ภาชนะ (`FoodContainer` และลูก) ส่งความร้อนให้วัตถุดิบที่อยู่ข้างใน
- `DishEvaluator` ให้คะแนนจาก `IngredientSnapshot` (ข้อมูลสรุปของวัตถุดิบ) จึง test ได้โดยไม่ต้องมีวัตถุในฉาก

**โครง prefab ของวัตถุดิบ:** วัตถุดิบหนึ่งหน่วย (นับ / สุก / เข้าสูตร) = `IngredientInstance` หนึ่งตัวที่ root มีชิ้นฟิสิกส์ได้ 1..N ชิ้น ทุกระบบอ่านชิ้นจาก `IngredientInstance.Bodies` ห้ามหา `Rigidbody` เอง

```
<ingredient>-pf      IngredientInstance + IngredientVisuals + Item     ← ตัวตน
 ├─ piece_1          MeshRenderer + Collider + Rigidbody               ← ชิ้นฟิสิกส์
 └─ piece_N          ...
```

- วัตถุดิบชิ้นเดียว: root มี Rigidbody + Collider เองได้ (root = ชิ้นเดียว)
- วัตถุดิบหลายชิ้น (หั่น / เต๋า): **ห้าม** มี Rigidbody หรือ Collider ที่ root เพราะ Rigidbody ซ้อนกันจะสู้กันเอง (`IngredientInstance.Awake` จะ log error ให้)
- ชิ้นลูกห้ามมี `IngredientInstance` / `IngredientVisuals` ของตัวเอง ไม่งั้นจะถูกนับเป็นวัตถุดิบเพิ่มอีกหนึ่งหน่วย
- รูปแบบต่างกัน (ทั้งชิ้น / หั่นเต๋า / บด) = `IngredientDataSO` คนละตัว แต่ละตัวมี prefab ของตัวเอง ไม่ต้องเพิ่ม enum

### สถานี
- จุดที่ผู้เล่นเข้าไปใช้งานแล้วกล้องล็อก (เตา, PrepStation, เครื่องเล่นแผ่น) ใช้ `Station` หรือ subclass ของมัน (`MainStation` มีสถานีย่อยได้) ห้าม copy ตรรกะกล้อง / โหมด / ปุ่มออกไปเขียนซ้ำ
- ของที่เปิด/ปิดตามการเข้า-ออก (UI, controller, เสียง) ทำเป็น `IStationModule` แล้ววางเป็นลูกของสถานี `Station` จะเก็บเองโดยไม่ต้องรู้จักมันโดยตรง
- UI ของสถานีมี Canvas ของตัวเองอยู่ใต้สถานี (เช่น `Discplayer/DiscPlayerCanvas`) เพราะ object อ้างข้ามฉากไม่ได้

### ช่องข่าวกลาง
- `MessageBus<T>` ใช้ประกาศว่า "เกิดอะไรขึ้น" ข่าวที่มีตอนนี้: `ItemCollected`, `DialogueStarted`, `DialogueBeatStarted`, `DialogueEnded`, `WorldEventTriggered`, `QuestStarted`, `QuestCompleted` ส่วนการสั่งให้ทำอะไร ให้เรียกเมธอดตรงๆ
- `DialogueEnded` ถูกส่งก่อนออกจากโหมด Dialogue ระบบที่รอให้คุยจบจึงเปิดคำขอทันก่อนเกมกลับไปเดินสำรวจ

### เควส
- `QuestLog` (Core) เป็นเจ้าของเควสทั้งหมด: รับข่าวเพื่อเดินความคืบหน้า จ่ายรางวัล และเริ่มเควสที่ตั้ง Start Item / Start Event ไว้ให้เอง
- เนื้อหาเควสมาจาก `IQuestDefinition` ได้สองแบบ: `QuestSO` (เนื้อเรื่อง ทำครั้งเดียว) และ `IngredientOrder` (เควสสุ่มของ NPC ทำซ้ำได้)
- เป้าหมาย 4 แบบ: `DeliverDishObjectiveSO`, `CollectItemObjectiveSO`, `ReachLocationObjectiveSO`, `TalkToObjectiveSO` ความคืบหน้าเก็บใน `ActiveQuest` ไม่เก็บใน asset
- `NPCQuestGiver` เป็นแค่ทางเข้าในบทสนทนา ส่วน `QuestNote` คือโน้ตที่ผูกกับเควสด้วยรหัสของ `ActiveQuest`

### Cutscene
แยก 3 หน้าที่ไม่ปนกัน (วิธีทำฉากทีละขั้นอยู่ที่ `docs/CUTSCENE_GUIDE.md`)
- **เล่นเมื่อไหร่** — `CutsceneScheduler` (Core) ฟังข่าว `WorldEventTriggered`, `QuestStarted`, `QuestCompleted`, `DialogueEnded` เทียบกับตัวกระตุ้นของ `CutsceneSO` และเงื่อนไข "ต้องจบเควสเหล่านี้ก่อน" แล้วเปิด "คำขอ" ไว้ใน `CutsceneQueue` คำขออยู่จนกว่า cutscene จะเล่นจบจริงและลง save ด้วย (ตัวกระตุ้นเกิดครั้งเดียว ทิ้งคำขอแล้ว cutscene จะหายไปเลย) เล่นได้เมื่ออยู่โหมด Exploring และ Stage ของมันอยู่ในฉากที่เปิด
- **เล่นยังไง** — `CutscenePlayer` (Core) ประตูเดียวเข้าออก: โหมด Cutscene, ภาพเข้าออก (Blend / Fade / Cut), หยุดเวลาทั้งเกมเมื่อเป็นชนิด Full, ปุ่ม Enter ค้างเพื่อข้าม, และผลตอนจบ ดูจบกับกดข้ามผ่าน `Finish` ทางเดียวกัน ผลจึงเหมือนกันเสมอ
- **เล่นอะไร** — `CutsceneStage` + `PlayableDirector` ในฉากพื้นที่ ถือ Timeline กล้อง และนักแสดง แถวกล้องของ Timeline ถูกผูกกับ Brain ใน Core ตอนเริ่มเล่น
- ข้อมูลอยู่ใน `CutsceneSO` (รหัส, ตัวกระตุ้น + รหัส, เควสที่ต้องจบก่อน, เล่นครั้งเดียว, ชนิด, ภาพเข้าออก, หันกล้องได้ไหม, ข่าวตอนจบ) ข่าวตอนจบส่งเป็น `WorldEventTriggered` เควสจึงเริ่มต่อจาก cutscene ได้โดยไม่ต้องรู้จักระบบนี้
- บทพูดใน cutscene: แถว `DialogueLineTrack` + clip `DialogueLineClip` (ผู้พูด = `NPCProfileSO`, ข้อความ, รอกดไหม) `CutscenePlayer` ส่งบรรทัดให้ `DialogueRunner.PlayLine` และอ่านปุ่ม Cutscene/Advance เอง ถ้าบรรทัดยังไม่ถูกอ่านตอน clip จบ Timeline จะค้าง (ความเร็ว 0) จนกดไปต่อ clip ที่ต่อกันพอดีถูกต่อคิว ไม่แสดงทับกัน
- cutscene แบบสื่อ (`Content` = Video / Slides) ไม่ต้องมี Stage เล่นได้ทุกที่ แสดงผ่าน `MediaCutsceneView` (ใต้ GameCanvas ชั้นล่างสุด หน้าต่างคุยจึงอยู่บนภาพ) เข้าออกด้วยจอดำ หยุดเวลาและ `AudioListener.pause` เสมอ เสียงวิดีโอออกทาง AudioSource ที่ไม่สนการหยุดเสียง
- กล้องในบทสนทนาปกติ: `DialogueRunner` ประกาศ `DialogueBeatStarted` (Greeting / Response / Menu) แล้ว `DialogueShotDirector` (Core) เลือกมุมจาก `DialogueShotRig` ของผู้พูด (ลูกของ NPC) ด้วยการยก Priority (สายตาผู้เล่นเป็นมุมหลัก มุมอื่นใช้เฉพาะเมื่อ `DialogueOption.Shot` ของตัวเลือกที่กดขอ เมนูค้างมุมเดิม จบคุยกลับสายตาผู้เล่น) ระหว่างที่เป็นมุม NPC จะล็อกการหันกล้องของผู้เล่น `NPCFacing` หัน NPC หาผู้เล่นตอนเริ่มคุย มุมทั้งหมดจึงอยู่ฝั่งผู้เล่น (กฎ 180 องศา) ชื่อกล้อง `DialogueShot_CloseUp` / `DialogueShot_Medium` ถูกตั้งให้ตัดภาพในไฟล์ blends
- ภาพเลื่อนเข้าออกใช้เวลาและเส้นโค้งจาก `Main Camera Custom Blends` (กล้องผู้เล่น ↔ ANY) ปรับที่ไฟล์นั้นที่เดียว

### Save
- `SaveService` เขียนไฟล์ JSON ก้อนเดียว (มีเวอร์ชัน) ที่ `Application.persistentDataPath/save.json`
- save เฉพาะในพื้นที่ที่ไม่ใช่ข้างนอก: ตอนกลับถึงบ้าน, ทุก 30 วินาที, และตอนปิดเกม ถ้าตายหรือออกเกมระหว่างอยู่ข้างนอก จะกลับไปที่ save ล่าสุดในบ้าน
- ระบบที่มีข้อมูลถาวร implement `ISaveSection` แล้วลงทะเบียนตอน Start ข้อมูลจะถูกคืนให้ตอนลงทะเบียน (ส่วนของบ้านจึงคืนค่าเองตอนบ้านโหลดเสร็จ)
- ไอเทมอ้างถึงชนิดด้วยรหัสผ่าน `GameData/Resources/ItemDatabase` ส่วนวัตถุที่วางไว้ในฉากใช้ `PersistentId`
- ตอนพัฒนา save **ปิด** เป็นค่าเริ่มต้น กด **Backspace** เพื่อเริ่มบันทึก (มีป้าย SAVE ON ที่มุมจอ) เกมจริงที่ build ออกไป save เสมอ

---

## 4. สูตรเพิ่มของใหม่

| อยากเพิ่ม | ทำ |
|---|---|
| ไอเทมชนิดใหม่ | สร้าง `ItemDefinitionSO` (รหัสไม่ซ้ำ + Prefab ที่มี `Item`) → กด Refresh ที่ `ItemDatabase` |
| สถานะที่ติดไปกับไอเทม | implement `ISavableState` บน component ของ prefab ด้วย `StateKey` คงที่ |
| พื้นที่ใหม่ | ฉากใหม่ + `LocationSO` + ใส่ใน Build Settings + เพิ่มใน `LocationLoader.locations` + วาง `LocationSpawn` + (ถ้า save ได้) วาง `LocationItems` |
| ไอเทมที่วางไว้ในพื้นที่ที่ save ได้ | ใส่ `PersistentId` ที่ตัวในฉาก (ห้ามใส่ใน prefab) |
| เควสเนื้อเรื่อง | `QuestSO` + เป้าหมาย → ใส่ในรายการ `Quests` ของ QuestLog → ใส่ใน `storyQuests` ของ NPC (หรือตั้ง Start Item / Start Event) |
| เป้าหมายเควสแบบใหม่ | subclass ของ `QuestObjectiveSO` แล้ว override `On...` ของข่าวที่สนใจ (ถ้าต้องใช้ข่าวใหม่ ให้เพิ่ม struct ข่าวในกล่องของผู้ส่ง) |
| สถานีใหม่ | subclass หรือใช้ `Station` ตรงๆ ของที่เปิด/ปิดตามการเข้าออกทำเป็น `IStationModule` แล้ววางเป็นลูกของสถานี |
| ปุ่มใหม่ | ใส่ใน action map ของสถานการณ์ที่ใช้ แล้วอ้างผ่าน `InputActionReference` |
| ระบบที่ต้อง save | `ISaveSection` + `SaveService.Instance.Register(this)` ใน Start |
| cutscene ใหม่ | `CutsceneSO` (รหัสไม่ซ้ำ + Trigger + Trigger Id) → ใส่ในรายการ `Cutscenes` ของ CutsceneScheduler → ในฉากวาง `CutsceneStage` + Timeline (ดู `docs/CUTSCENE_GUIDE.md`) |

## 5. เครื่องมือตอนพัฒนา

- **Backspace**: เปิดหรือปิด save (จำค่าข้ามการกด Play)
- `GameData/Dev/DevSettings` → **Dev Locations Enabled**: เปิดหรือปิดประตูไปพื้นที่ทดสอบ
- `GameData/Dev/DevSettings` → **Skip Cutscenes**: ข้ามทุก cutscene ทันที (ผลตอนจบยังถูกใส่ครบ)
- `DynamicFontSaveCleaner`: ล้างตัวอักษรที่ฟอนต์ TMP แบบ Dynamic สร้างระหว่างใช้งานก่อนบันทึก ไฟล์ฟอนต์จึงไม่เปลี่ยนใน git
- test: Window → General → Test Runner → EditMode (ครอบคลุม grid, สถานะไอเทม, เควส, save, การทำอาหาร, คิว cutscene)

## 6. ที่ยังไม่ทำ (ตั้งใจเลื่อนไว้)

- ของซ้อนในของ (กล่องในกระเป๋า, อาหารบนจานที่ถืออยู่ตอน save)
- สถานะของเครื่องในบ้าน (ระดับไฟลูกบิด, ตำแหน่งกระทะ, เพลงที่เล่น) ยังไม่ถูก save
- ระบบตายแล้วเสียของ / หนีออก, ช่องอุปกรณ์และชุด build, อาวุธ, ตัวสร้างแมพจากห้อง, บทสนทนาแตกเส้นเรื่อง

## แหล่งอ้างอิงของแนวทาง

- Unity Open Project "Chop Chop": [Game architecture overview](https://github.com/UnityTechnologies/open-project-1/wiki/Game-architecture-overview) (โครงฉากแบบ persistent + พื้นที่, InputReader สลับชุดปุ่มตามบริบท, save เป็นก้อนเดียวอ้างด้วยรหัส)
- Unity Boss Room: [Architecture](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.4/manual/samples/bossroom/architecture.html) (แยก assembly ตามระบบของเกม, pub/sub แบบมีชนิด)
- Unreal Subsystems และ Lyra (ระบุอายุของ service ให้ชัด, ส่งข้อความระหว่างระบบ)
- Zero Sievert (save เฉพาะในฐาน), Escape from Tarkov ("Found in raid")
- Robert Nystrom, [Game Programming Patterns: Singleton](https://gameprogrammingpatterns.com/singleton.html)
- Cutscene: Chop Chop [Scripts/Cutscenes](https://github.com/UnityTechnologies/open-project-1/tree/main/UOP1_Project/Assets/Scripts/Cutscenes) (ผูก Timeline ตอนเล่น, ค้างไทม์ไลน์ด้วยความเร็ว 0), Pixel Crushers [Cutscene Sequences](https://www.pixelcrushers.com/dialogue_system/manual2x/html/cutscene_sequences.html) (คำสั่ง `required` = ผลที่ต้องเกิดแม้กดข้าม), [Cinemachine 3.1 + Timeline](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/concept-timeline.html), Witcher 3 GDC 2016 (cutscene ทำมือเฉพาะจุดสำคัญ ที่เหลือเป็นบทสนทนาแบบระบบ)
